@tool
class_name ArcweaveMediaDownloader extends Node
## Downloads the images and audio of a project initialized from the WebAPI and links them to its ArcweaveAsset.

## Emitted before each download and while waiting to retry a rate limited download
signal progress_changed(info: String, value: float)
signal _fetch_finished(response: Array)

const IMAGE_EXTENSIONS = ["png", "jpg", "jpeg", "webp", "svg", "bmp", "tga"]
const AUDIO_EXTENSIONS = ["mp3", "ogg", "wav"]
const MAX_ATTEMPTS = 5
const MAX_RETRY_SECONDS = 60
const REQUEST_TIMEOUT_SECONDS = 120
const POLL_SECONDS = 0.1
const IMPORT_TIMEOUT_SECONDS = 120

## True once canceled. The remaining downloads are skipped.
var canceled : bool = false
var arcweave_asset : ArcweaveAsset
var _api_request : APIRequest
var _http_request : HTTPRequest

func _init(object : ArcweaveAsset):
	arcweave_asset = object
	_api_request = APIRequest.new(object)
	add_child(_api_request)

## Cancels the download. Media that was not downloaded keeps its previous copy.
func cancel():
	canceled = true
	if _http_request != null:
		_http_request.cancel_request()
		_fetch_finished.emit([HTTPRequest.RESULT_REQUEST_FAILED, 0, PackedStringArray(), PackedByteArray()])

## Downloads the media the project uses and links it to the ArcweaveAsset. The node must be inside the tree.
## Never fails: media that cannot be downloaded or saved keeps its previous copy and a warning is printed.
func download(project_settings : Dictionary):
	var asset = arcweave_asset
	if not _is_saved(asset):
		push_warning("[Arcweave] Skipping media download because the ArcweaveAsset is not saved as a file.")
		return
	asset.is_downloading_media = true

	var media = _collect_media(project_settings)
	var downloads = {}
	var ids = media.keys()
	var api_url = _api_request.get_api_url()
	var headers = _api_request.get_headers()
	var project_hash = asset.project_hash
	for i in ids.size():
		if canceled:
			break
		var id : String = ids[i]
		var asset_name : String = media[id].name
		var extension = asset_name.get_extension().to_lower()
		var supported_extensions = IMAGE_EXTENSIONS if media[id].type == "image" else AUDIO_EXTENSIONS
		if not extension in supported_extensions:
			push_warning("[Arcweave] Skipping '%s' because Godot cannot import '.%s' files as %s." % [asset_name, extension, media[id].type])
			continue
		# The id names the downloaded file, so anything that is not a plain file name is never used in a path
		if not id.is_valid_filename() or id.replace(".", "").strip_edges().is_empty():
			push_warning("[Arcweave] Skipping '%s' because its asset id '%s' is not a valid file name." % [asset_name, id])
			continue

		var url = api_url + project_hash.uri_encode() + "/asset/" + id.uri_encode() + "/file"
		var info = "Downloading media %d/%d: %s" % [i + 1, ids.size(), asset_name]
		var bytes = await _fetch(url, headers, asset_name, info, float(i) / ids.size())
		if bytes != null:
			downloads[id] = bytes
	if canceled:
		push_warning("[Arcweave] Media download canceled. Media that was not downloaded keeps its previous copy.")

	# Nothing awaits until the files are written and deleted, so the paths resolved there are still current.
	if not _is_saved(asset):
		push_warning("[Arcweave] Skipping media update because the ArcweaveAsset was deleted during the download.")
		asset.is_downloading_media = false
		return
	var changed = _save_media(asset, media, downloads)
	await _link_resources(asset, changed)
	asset.is_downloading_media = false

## Writes the downloaded media, deletes the media the project no longer uses and returns true if any file changed
func _save_media(asset : ArcweaveAsset, media : Dictionary, downloads : Dictionary) -> bool:
	var asset_path = asset.resource_path
	# A duplicated ArcweaveAsset starts with its own media instead of managing the original's
	var owner_id = _owner_id(asset_path)
	if asset.media_owner != owner_id:
		asset.media_owner = owner_id
		asset.media_folder = ""
		asset.downloaded_media = {}
		asset.media_resources = {}
		asset.owned_media = {}

	# Files written by previous initializations whose contents still match. Only these files are ever replaced or
	# deleted, so a file the user edited or put in place of a downloaded one is never touched.
	var owned = {}
	var previous_paths = {}
	for id in asset.downloaded_media:
		var saved_path : String = asset.downloaded_media[id]
		var sha256 : String = asset.owned_media.get(saved_path, "")
		# The imported resource follows the file when it is moved
		var path = saved_path
		var resource = asset.media_resources.get(id)
		if resource is Resource and not resource.resource_path.is_empty():
			path = resource.resource_path
		if _is_owned(path, sha256):
			previous_paths[id] = path
			owned[path] = sha256
	for path in asset.owned_media:
		if not owned.has(path) and not path in asset.downloaded_media.values() and _is_owned(path, asset.owned_media[path]):
			owned[path] = asset.owned_media[path]

	# A previous file is kept when its replacement cannot be downloaded or saved, so a failed download does not remove media.
	var paths = {}
	for id in media:
		if not downloads.has(id) and previous_paths.has(id):
			paths[id] = previous_paths[id]

	var folder = asset.media_folder
	if downloads.size() > 0 and (folder.is_empty() or not DirAccess.dir_exists_absolute(folder)):
		folder = _unique_path(asset_path.get_base_dir().path_join(asset_path.get_file().get_basename() + " Media"), "")
		var error = DirAccess.make_dir_recursive_absolute(folder)
		if error != OK:
			push_warning("[Arcweave] Could not create the media folder %s: %s" % [folder, error_string(error)])
			downloads = {}
		else:
			asset.media_folder = folder

	var saved = 0
	var changed = false
	var writer = load("res://addons/arcweave/Editor/MediaFileWriter.cs").new()
	for id in downloads:
		# Overwrite the previous file in place when possible, so references to it are kept.
		# Otherwise use a unique path, so files the downloader does not own are never overwritten.
		var extension = media[id].name.get_extension().to_lower()
		var previous_path : String = previous_paths.get(id, "")
		var path = previous_path
		if previous_path.is_empty() or previous_path.get_extension().to_lower() != extension:
			path = _unique_path(folder.path_join(id), extension)
		var bytes : PackedByteArray = downloads[id]
		var sha256 = _sha256(bytes)
		if owned.get(path) != sha256:
			var error : String = writer.Write(path, bytes)
			if not error.is_empty():
				# Eg. a read-only file checked in to version control
				push_warning("[Arcweave] Could not save '%s' to %s: %s" % [media[id].name, path, error])
				if not previous_path.is_empty():
					paths[id] = previous_path
				continue
			owned[path] = sha256
			changed = true
		paths[id] = path
		saved += 1

	var linked_paths = paths.values()
	for path in owned.keys():
		if path in linked_paths:
			continue
		var error = _delete_file(path)
		if error != OK:
			# Still owned, so a later initialization tries again
			push_warning("[Arcweave] Could not delete unused media file %s: %s" % [path, error_string(error)])
		else:
			owned.erase(path)
			changed = true

	asset.downloaded_media = paths
	asset.owned_media = owned
	asset.emit_changed()
	print("[Arcweave] Downloaded %d of %d media files for %s" % [saved, media.size(), asset_path.get_file()])
	return changed

## Updates the editor's FileSystem once, after all the files are written and deleted, so they are imported in one go.
## Then links the imported resources to the ArcweaveAsset. Outside the editor the files can't be imported, so the
## resources are left as they are.
func _link_resources(asset : ArcweaveAsset, changed : bool):
	if not (Engine.is_editor_hint() and Engine.has_singleton("EditorInterface")):
		return
	var filesystem = Engine.get_singleton("EditorInterface").get_resource_filesystem()
	if changed:
		progress_changed.emit("Importing media...", 1.0)
		var scanned = [false]
		var on_filesystem_changed = func(): scanned[0] = true
		filesystem.filesystem_changed.connect(on_filesystem_changed)
		filesystem.scan()
		var deadline = Time.get_ticks_msec() + IMPORT_TIMEOUT_SECONDS * 1000
		while (not scanned[0] or filesystem.is_scanning()) and Time.get_ticks_msec() < deadline:
			await get_tree().process_frame
		filesystem.filesystem_changed.disconnect(on_filesystem_changed)

	var resources = {}
	for id in asset.downloaded_media:
		var path : String = asset.downloaded_media[id]
		var resource = asset.media_resources.get(id)
		if resource is Resource and resource.resource_path == path:
			resources[id] = resource
		elif ResourceLoader.exists(path):
			resource = ResourceLoader.load(path)
			if resource != null:
				resources[id] = resource
	asset.media_resources = resources
	asset.emit_changed()

## Collects the images and audio the project uses, as Arcweave asset id -> { name, type }.
## These are the element and component covers, the element audio and the assets of asset list attributes.
func _collect_media(project_settings : Dictionary) -> Dictionary:
	var media = {}
	var assets = _dictionary(project_settings.get("assets"))
	for element in _dictionary(project_settings.get("elements")).values():
		_add_cover(media, assets, element)
		var audio = _dictionary(_dictionary(element).get("assets")).get("audio")
		if audio is Array:
			for audio_asset in audio:
				_add_media(media, assets, _dictionary(audio_asset).get("asset"), "audio")
	for component in _dictionary(project_settings.get("components")).values():
		_add_cover(media, assets, component)
	for attribute in _dictionary(project_settings.get("attributes")).values():
		var value = _dictionary(_dictionary(attribute).get("value"))
		if value.get("type") == "asset-list" and value.get("data") is Array:
			for id in value.data:
				_add_media(media, assets, id, "")
	return media

func _add_cover(media : Dictionary, assets : Dictionary, node):
	var cover = _dictionary(_dictionary(_dictionary(node).get("assets")).get("cover"))
	# Icon covers are built in Arcweave icons, not uploaded assets
	if cover.get("type") == "icon":
		return
	_add_media(media, assets, cover.get("id"), "image")

func _add_media(media : Dictionary, assets : Dictionary, id, type : String):
	if not (id is String) or media.has(id):
		return
	var asset = _dictionary(assets.get(id))
	var asset_name = asset.get("name")
	if not (asset_name is String) or asset_name.is_empty():
		return
	if type.is_empty():
		match asset.get("type"):
			"image", "template-image":
				type = "image"
			"audio", "template-audio":
				type = "audio"
			_:
				return
	media[id] = { "name": asset_name, "type": type }

## Downloads a file, waiting and retrying while rate limited. Returns null if the download fails or is canceled.
func _fetch(url : String, headers : PackedStringArray, asset_name : String, info : String, value : float):
	for attempt in range(1, MAX_ATTEMPTS + 1):
		if canceled:
			return null
		progress_changed.emit(info, value)
		var response = await _request(url, headers)
		if canceled:
			return null
		var result : int = response[0]
		var response_code : int = response[1]
		if result == HTTPRequest.RESULT_SUCCESS and response_code == 200:
			return response[3]

		if response_code == 429 and attempt < MAX_ATTEMPTS:
			var retry_at = Time.get_ticks_msec() + int(_retry_delay_seconds(_header(response[2], "Retry-After")) * 1000)
			while Time.get_ticks_msec() < retry_at:
				if canceled:
					return null
				var seconds = ceili((retry_at - Time.get_ticks_msec()) / 1000.0)
				progress_changed.emit("%s (rate limited, retrying in %ds)" % [info, seconds], value)
				await get_tree().create_timer(POLL_SECONDS).timeout
			if canceled:
				return null
			continue

		push_warning("[Arcweave] Could not download '%s' (code = %d, result = %d)" % [asset_name, response_code, result])
		return null
	return null

## Sends a GET request and returns [result, response_code, headers, body]
func _request(url : String, headers : PackedStringArray) -> Array:
	var http_request = HTTPRequest.new()
	http_request.timeout = REQUEST_TIMEOUT_SECONDS
	add_child(http_request)
	_http_request = http_request
	http_request.request_completed.connect(func(result, response_code, response_headers, body):
		if _http_request == http_request:
			_fetch_finished.emit([result, response_code, response_headers, body]))
	var response = [HTTPRequest.RESULT_CANT_CONNECT, 0, PackedStringArray(), PackedByteArray()]
	if http_request.request(url, headers) == OK:
		response = await _fetch_finished
	_http_request = null
	http_request.queue_free()
	return response

## Returns how long to wait before retrying a rate limited request, from its Retry-After header in seconds or as a date,
## capped so a download never waits for long.
static func _retry_delay_seconds(retry_after : String) -> float:
	var seconds = float(MAX_RETRY_SECONDS)
	if retry_after.is_valid_int():
		seconds = retry_after.to_int()
	elif not retry_after.is_empty():
		var date = _http_date_to_unix(retry_after)
		if date >= 0:
			seconds = date - Time.get_unix_time_from_system()
	return clampf(seconds, 1, MAX_RETRY_SECONDS)

## Parses an HTTP date such as "Wed, 21 Oct 2015 07:28:00 GMT". Returns -1 if it is not one.
static func _http_date_to_unix(date : String) -> float:
	const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
	var parts = date.strip_edges().split(" ", false)
	if parts.size() < 5 or not parts[1].is_valid_int() or not parts[3].is_valid_int() or not parts[2] in MONTHS:
		return -1
	var time = parts[4].split(":")
	if time.size() != 3:
		return -1
	var datetime = "%04d-%02d-%02dT%s" % [parts[3].to_int(), MONTHS.find(parts[2]) + 1, parts[1].to_int(), parts[4]]
	return Time.get_unix_time_from_datetime_string(datetime)

static func _header(headers : PackedStringArray, header_name : String) -> String:
	for header in headers:
		var separator = header.find(":")
		if separator > 0 and header.substr(0, separator).strip_edges().to_lower() == header_name.to_lower():
			return header.substr(separator + 1).strip_edges()
	return ""

## Returns true if the file exists and still has the contents the downloader wrote
static func _is_owned(path : String, sha256 : String) -> bool:
	return not sha256.is_empty() and FileAccess.file_exists(path) and FileAccess.get_sha256(path) == sha256

static func _sha256(bytes : PackedByteArray) -> String:
	var context = HashingContext.new()
	context.start(HashingContext.HASH_SHA256)
	context.update(bytes)
	return context.finish().hex_encode()

## Deletes a downloaded file and the files the editor created for it
static func _delete_file(path : String) -> Error:
	var error = DirAccess.remove_absolute(path)
	if error == OK:
		for extra_path in [path + ".import", path + ".uid"]:
			if FileAccess.file_exists(extra_path):
				DirAccess.remove_absolute(extra_path)
	return error

## Returns base + "." + extension, or "base 2.extension", "base 3.extension"... if it already exists
static func _unique_path(base : String, extension : String) -> String:
	var suffix = "" if extension.is_empty() else "." + extension
	var path = base + suffix
	var number = 2
	while FileAccess.file_exists(path) or DirAccess.dir_exists_absolute(path):
		path = "%s %d%s" % [base, number, suffix]
		number += 1
	return path

## Identifies the resource file across moves and renames by its uid when it has one, otherwise by its path
static func _owner_id(asset_path : String) -> String:
	var uid = ResourceLoader.get_resource_uid(asset_path)
	if uid != ResourceUID.INVALID_ID:
		return ResourceUID.id_to_text(uid)
	return asset_path

## Media is saved next to the resource, so a resource that is not saved as a file can't download it
static func _is_saved(asset : ArcweaveAsset) -> bool:
	return asset.resource_path.begins_with("res://") and not asset.resource_path.contains("::")

static func _dictionary(value) -> Dictionary:
	return value if value is Dictionary else {}

