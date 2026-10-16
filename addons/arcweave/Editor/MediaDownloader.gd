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

	# Nothing below awaits, so the paths resolved here are still current when the files are written and deleted.
	if not _is_saved(asset):
		push_warning("[Arcweave] Skipping media update because the ArcweaveAsset was deleted during the download.")
		asset.is_downloading_media = false
		return
	_save_media(asset, media, downloads)
	asset.is_downloading_media = false

func _save_media(asset : ArcweaveAsset, media : Dictionary, downloads : Dictionary):
	var asset_path = asset.resource_path
	# A duplicated ArcweaveAsset starts with its own media instead of managing the original's
	var owner_id = _owner_id(asset_path)
	if asset.media_owner != owner_id:
		asset.media_owner = owner_id
		asset.media_folder = ""
		asset.downloaded_media = {}
		asset.stale_media = PackedStringArray()

	# Files downloaded by previous initializations. Only these files are ever replaced or deleted.
	var owned_paths = []
	var previous_paths = {}
	for id in asset.downloaded_media:
		var path : String = asset.downloaded_media[id]
		if FileAccess.file_exists(path):
			previous_paths[id] = path
			if not path in owned_paths:
				owned_paths.append(path)
	for path in asset.stale_media:
		if FileAccess.file_exists(path) and not path in owned_paths:
			owned_paths.append(path)

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
	for id in downloads:
		# Overwrite the previous file in place when possible, so references to it are kept.
		# Otherwise use a unique path, so files the downloader does not own are never overwritten.
		var extension = media[id].name.get_extension().to_lower()
		var previous_path : String = previous_paths.get(id, "")
		var path = previous_path
		if previous_path.is_empty() or previous_path.get_extension().to_lower() != extension:
			path = _unique_path(folder.path_join(id), extension)
		var bytes : PackedByteArray = downloads[id]
		if not FileAccess.file_exists(path) or FileAccess.get_file_as_bytes(path) != bytes:
			var error = _write_file(path, bytes)
			if error != OK:
				# Eg. a read-only file checked in to version control
				push_warning("[Arcweave] Could not save '%s' to %s: %s" % [media[id].name, path, error_string(error)])
				if not previous_path.is_empty():
					paths[id] = previous_path
				continue
			changed = true
		paths[id] = path
		saved += 1

	var linked_paths = paths.values()
	var stale_media = PackedStringArray()
	for path in owned_paths:
		if path in linked_paths:
			continue
		var error = _delete_file(path)
		if error != OK:
			# Still owned, so a later initialization tries again
			push_warning("[Arcweave] Could not delete unused media file %s: %s" % [path, error_string(error)])
			stale_media.append(path)
		else:
			changed = true

	asset.downloaded_media = paths
	asset.stale_media = stale_media
	asset.emit_changed()
	if changed:
		_scan_filesystem()
	print("[Arcweave] Downloaded %d of %d media files for %s" % [saved, media.size(), asset_path.get_file()])

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

## Writes the file through a temporary file, so a failed write never leaves it truncated.
## Temporary files start with a dot, so the editor never imports them.
static func _write_file(path : String, bytes : PackedByteArray) -> Error:
	var temporary_path = path.get_base_dir().path_join(".arcweave-%d-%d.tmp" % [Time.get_ticks_usec(), randi()])
	var file = FileAccess.open(temporary_path, FileAccess.WRITE)
	if file == null:
		return FileAccess.get_open_error()
	file.store_buffer(bytes)
	var error = file.get_error()
	file.close()
	if error == OK:
		error = DirAccess.rename_absolute(temporary_path, path)
	if FileAccess.file_exists(temporary_path):
		DirAccess.remove_absolute(temporary_path)
	return error

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

## Updates the editor's FileSystem once, after all the files are written and deleted, so they are imported in one go
static func _scan_filesystem():
	if Engine.is_editor_hint() and Engine.has_singleton("EditorInterface"):
		Engine.get_singleton("EditorInterface").get_resource_filesystem().scan()
