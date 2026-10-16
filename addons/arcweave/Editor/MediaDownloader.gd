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
const MAX_IMPORT_SCANS = 3
const SCAN_START_SECONDS = 0.5
const INVALID_CHARACTERS = "<>:\"/\\|?*"
const RESERVED_NAMES = ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
	"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"]

## True once canceled. The remaining downloads are skipped.
var canceled : bool = false
var arcweave_asset : ArcweaveAsset
var _api_request : APIRequest
var _http_request : HTTPRequest
# The Arcweave folder of each asset and folder id
var _parents : Dictionary = {}

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
## Media the project no longer uses is only deleted by delete_unused_media(), once the ArcweaveAsset is saved.
func download(project_settings : Dictionary):
	var asset = arcweave_asset
	if not _is_saved(asset):
		push_warning("[Arcweave] Skipping media download because the ArcweaveAsset is not saved as a file.")
		return
	# The resource must be saved with the new media, so nothing is downloaded if it can't be
	var file = FileAccess.open(asset.resource_path, FileAccess.READ_WRITE)
	if file == null:
		push_warning("[Arcweave] Skipping media download because %s cannot be written: %s" % [asset.resource_path, error_string(FileAccess.get_open_error())])
		return
	file.close()
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

		var url = api_url + project_hash.uri_encode() + "/asset/" + id.uri_encode() + "/file"
		var info = "Downloading media %d/%d: %s" % [i + 1, ids.size(), asset_name]
		var bytes = await _fetch(url, headers, asset_name, info, float(i) / ids.size())
		if bytes != null:
			downloads[id] = bytes
	if canceled:
		push_warning("[Arcweave] Media download canceled. Media that was not downloaded keeps its previous copy.")

	# Nothing awaits until the files are written, so the paths resolved there are still current.
	if not _is_saved(asset):
		push_warning("[Arcweave] Skipping media update because the ArcweaveAsset was deleted during the download.")
		asset.is_downloading_media = false
		return
	var saved = _save_media(asset, media, downloads)
	await _link_resources(asset, saved.written, saved.replaced, saved.moved_from)
	asset.is_downloading_media = false

## Deletes the media the project no longer uses and returns true if any was deleted. Call it once the ArcweaveAsset is
## saved, so a resource that failed to save never references deleted media.
func delete_unused_media() -> bool:
	var asset = arcweave_asset
	var linked_paths = asset.downloaded_media.values()
	var owned = asset.owned_media.duplicate()
	var deleted = PackedStringArray()
	for path in owned.keys():
		# Only files that still have the contents the downloader wrote are deleted
		if path in linked_paths or not _is_owned(path, owned[path]):
			continue
		var error = _delete_file(path)
		if error != OK:
			# Still owned, so a later initialization tries again
			push_warning("[Arcweave] Could not delete unused media file %s: %s" % [path, error_string(error)])
			continue
		owned.erase(path)
		deleted.append(path)
	if deleted.is_empty():
		return false
	asset.owned_media = owned
	asset.emit_changed()
	var removed_folders = _remove_empty_folders(Array(deleted), asset.media_folder)
	if Engine.is_editor_hint() and Engine.has_singleton("EditorInterface"):
		var filesystem = Engine.get_singleton("EditorInterface").get_resource_filesystem()
		# A scan forgets the deleted files and folders. It is skipped while the editor imports, which would crash it,
		# and the editor's next scan updates the FileSystem dock instead.
		if not _is_busy(filesystem):
			filesystem.scan_sources()
	return true

## Writes the downloaded media and returns { written, replaced }: the paths written, and those of them that replaced
## a previous file. The media the project no longer uses stays owned, so delete_unused_media() can delete it.
func _save_media(asset : ArcweaveAsset, media : Dictionary, downloads : Dictionary) -> Dictionary:
	var asset_path = asset.resource_path
	# A duplicated ArcweaveAsset starts with its own media instead of managing the original's
	var owner_id = _owner_id(asset_path)
	# A duplicate has another uid and another path. A resource moved or renamed in the editor keeps its uid, and one whose
	# uid was regenerated, or that had none yet, keeps its path.
	if asset.media_owner != owner_id and asset.media_owner_path != asset_path and asset.media_owner != asset_path:
		asset.media_folder = ""
		asset.downloaded_media = {}
		asset.media_resources = {}
		asset.owned_media = {}
	asset.media_owner = owner_id
	asset.media_owner_path = asset_path

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

	# A previous file stays linked until its replacement is saved, so media that cannot be downloaded or saved is kept.
	var paths = {}
	for id in media:
		if previous_paths.has(id):
			paths[id] = previous_paths[id]

	var folder = asset.media_folder
	var folder_failed = false
	var saved = 0
	var written = PackedStringArray()
	var replaced = PackedStringArray()
	var moved_from = PackedStringArray()
	var writer = load("res://addons/arcweave/Editor/MediaFileWriter.cs").new()
	for id in downloads:
		var relative_path = _relative_path(media[id])
		var previous_path : String = previous_paths.get(id, "")
		var path = previous_path
		var same_extension = not previous_path.is_empty() \
			and previous_path.get_extension().to_lower() == relative_path.get_extension().to_lower()
		# A file the user moved out of the media folder stays where they put it
		if not same_extension or previous_path.begins_with(folder + "/"):
			# The folder is only created when a file needs a new path
			if folder.is_empty() or not DirAccess.dir_exists_absolute(folder):
				if folder_failed:
					continue
				folder = _unique_path(asset_path.get_base_dir().path_join(asset_path.get_file().get_basename() + " Media"), "")
				var folder_error = DirAccess.make_dir_recursive_absolute(folder)
				if folder_error != OK:
					push_warning("[Arcweave] Could not create the media folder %s: %s" % [folder, error_string(folder_error)])
					folder_failed = true
					continue
				asset.media_folder = folder
			# The file mirrors the asset's Arcweave folders and name. A unique path is used when it is taken, so files the
			# downloader does not own, or that belong to other assets, are never overwritten.
			var desired = folder.path_join(relative_path)
			if not same_extension:
				path = _unique_path(desired.get_basename(), desired.get_extension())
			elif not _is_name_of(previous_path, desired):
				# Renamed or moved in Arcweave, or named by its id by an earlier version. Moving the file keeps its uid, so
				# the scenes that reference it keep working.
				var target = _unique_path(desired.get_basename(), desired.get_extension())
				var move_error = _move_file(previous_path, target)
				if move_error == OK:
					owned[target] = owned[previous_path]
					owned.erase(previous_path)
					moved_from.append(previous_path)
					written.append(target)
					path = target
				else:
					push_warning("[Arcweave] Could not move %s to %s: %s" % [previous_path, target, error_string(move_error)])
		var bytes : PackedByteArray = downloads[id]
		var sha256 = _sha256(bytes)
		if owned.get(path) != sha256:
			var replacing = FileAccess.file_exists(path)
			# The Arcweave folders of the asset
			DirAccess.make_dir_recursive_absolute(path.get_base_dir())
			var error : String = writer.Write(path, bytes)
			if not error.is_empty():
				# Eg. a read-only file checked in to version control
				push_warning("[Arcweave] Could not save '%s' to %s: %s" % [media[id].name, path, error])
				_remove_empty_folders([path], folder)
				continue
			owned[path] = sha256
			if not path in written:
				written.append(path)
			if replacing:
				replaced.append(path)
		paths[id] = path
		saved += 1
	_remove_empty_folders(Array(moved_from), folder)

	asset.downloaded_media = paths
	asset.owned_media = owned
	asset.emit_changed()
	print("[Arcweave] Downloaded %d of %d media files for %s" % [saved, media.size(), asset_path.get_file()])
	return { "written": written, "replaced": replaced, "moved_from": moved_from }

## Imports the written files in one go and links the imported resources to the ArcweaveAsset.
## Outside the editor the files can't be imported, so the resources are left as they are.
func _link_resources(asset : ArcweaveAsset, written_paths : PackedStringArray, replaced_paths : PackedStringArray, moved_from : PackedStringArray):
	if not (Engine.is_editor_hint() and Engine.has_singleton("EditorInterface")):
		return
	var filesystem = Engine.get_singleton("EditorInterface").get_resource_filesystem()
	if not written_paths.is_empty():
		progress_changed.emit("Importing media...", 1.0)
		# Checks that the FileSystem imported the files instead of waiting for a scan signal, since a scan that was
		# already running may finish without seeing them. Files that are still not imported after a few scans, eg. in
		# a hidden or .gdignore folder, can't be imported, so the wait ends instead of rescanning.
		var deadline = Time.get_ticks_msec() + IMPORT_TIMEOUT_SECONDS * 1000
		var scans = 0
		while not canceled and Time.get_ticks_msec() < deadline:
			# Never scans while the editor imports: in the editor window, importing keeps processing frames, and scanning
			# then would end the import's progress task while it still runs, which crashes the editor
			if not _is_busy(filesystem):
				var pending = Array(written_paths).filter(func(path): return not _is_imported(filesystem, path))
				if pending.is_empty():
					break
				if scans == MAX_IMPORT_SCANS:
					push_warning("[Arcweave] The editor did not import %s. Media in hidden or ignored folders can't be imported." % ", ".join(pending))
					break
				filesystem.scan_sources()
				scans += 1
				# Gives the scan time to start, so a scan request that was ignored is retried
				await get_tree().create_timer(SCAN_START_SECONDS).timeout
			await get_tree().create_timer(POLL_SECONDS).timeout
		# Files replaced in place are reimported explicitly, since a scan misses files replaced within the same second,
		# which keep their modified time. They are tracked when written, because the FileSystem can't be queried while
		# it scans.
		var reimport_paths = Array(replaced_paths).filter(func(path): return _is_imported(filesystem, path))
		if not reimport_paths.is_empty() and not _is_busy(filesystem):
			filesystem.reimport_files(PackedStringArray(reimport_paths))

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

## Collects the images and audio the project uses, as Arcweave asset id -> { name, type, folders }, where folders are
## the names of the Arcweave folders the asset is in, from the outermost one.
## These are the element and component covers, the element audio and the assets of asset list attributes.
func _collect_media(project_settings : Dictionary) -> Dictionary:
	var media = {}
	var assets = _dictionary(project_settings.get("assets"))
	_parents = {}
	for folder_id in assets:
		var children = _dictionary(assets[folder_id]).get("children")
		if children is Array:
			for child in children:
				_parents[child] = folder_id
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
	media[id] = { "name": asset_name, "type": type, "folders": _folders(assets, id) }

## Returns the names of the Arcweave folders the asset is in, from the outermost one, without the root folder
func _folders(assets : Dictionary, id : String) -> PackedStringArray:
	var folders = PackedStringArray()
	var folder_id = _parents.get(id)
	# Bounded, so a malformed tree with a cycle can't loop forever
	while folder_id != null and folders.size() < 64:
		var folder = _dictionary(assets.get(folder_id))
		if folder.get("root") == true:
			break
		folders.insert(0, str(folder.get("name", "")))
		folder_id = _parents.get(folder_id)
	return folders

## Returns the path of the media relative to the media folder, eg. "Images/door.jpeg", mirroring the Arcweave folders
static func _relative_path(entry : Dictionary) -> String:
	var segments = Array(entry.folders).map(func(folder): return _sanitize(folder))
	segments.append(_sanitize(entry.name))
	return "/".join(segments)

## Makes a folder or file name safe on every platform, so a name can never leave the media folder
static func _sanitize(segment : String) -> String:
	var sanitized = ""
	for character in segment:
		sanitized += "_" if character in INVALID_CHARACTERS or character.unicode_at(0) < 32 else character
	# Windows drops trailing dots and spaces
	sanitized = sanitized.rstrip(". ")
	if sanitized.is_empty():
		return "_"
	# The editor never imports files or folders whose name starts with a dot
	if sanitized.begins_with("."):
		sanitized = "_" + sanitized
	# Windows reserves these names, with or without an extension
	var stem = sanitized.get_slice(".", 0)
	if stem.to_upper() in RESERVED_NAMES:
		sanitized = stem + "_" + sanitized.substr(stem.length())
	return sanitized

## Returns true if path is desired, or a unique variant of it such as "door 2.jpeg" for "door.jpeg"
static func _is_name_of(path : String, desired : String) -> bool:
	if path.get_base_dir() != desired.get_base_dir() or path.get_extension().to_lower() != desired.get_extension().to_lower():
		return false
	var stem = desired.get_file().get_basename().to_lower()
	var file_stem = path.get_file().get_basename().to_lower()
	if file_stem == stem:
		return true
	return file_stem.begins_with(stem + " ") and file_stem.substr(stem.length() + 1).is_valid_int()

## Moves a downloaded file with the files the editor created for it. Moving the .import file along keeps the file's
## uid, so the scenes that reference it keep working.
static func _move_file(from : String, to : String) -> Error:
	var error = DirAccess.make_dir_recursive_absolute(to.get_base_dir())
	if error == OK:
		error = DirAccess.rename_absolute(from, to)
	if error != OK:
		return error
	for extra in [".import", ".uid"]:
		if FileAccess.file_exists(from + extra):
			error = DirAccess.rename_absolute(from + extra, to + extra)
			if error != OK:
				# Moved back, so the file never loses its uid
				DirAccess.rename_absolute(to, from)
				return error
	return OK

## Deletes the empty folders the paths were in, up to the media folder, which is kept
static func _remove_empty_folders(paths : Array, media_folder : String) -> bool:
	var removed = false
	for path in paths:
		var folder : String = path.get_base_dir()
		while folder.begins_with(media_folder + "/") and _is_empty_folder(folder):
			if DirAccess.remove_absolute(folder) != OK:
				break
			removed = true
			folder = folder.get_base_dir()
	return removed

static func _is_empty_folder(folder : String) -> bool:
	var directory = DirAccess.open(folder)
	if directory == null:
		return false
	directory.include_hidden = true
	return directory.get_files().is_empty() and directory.get_directories().is_empty()

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

## Returns true while the editor's FileSystem scans or imports
static func _is_busy(filesystem) -> bool:
	return filesystem.is_scanning() or (filesystem.has_method("is_importing") and filesystem.is_importing())

static func _is_indexed(filesystem, path : String) -> bool:
	var directory = filesystem.get_filesystem_path(path.get_base_dir())
	return directory != null and directory.find_file_index(path.get_file()) >= 0

## Returns true if the editor's FileSystem knows the file and has imported it, even if the import failed
static func _is_imported(filesystem, path : String) -> bool:
	return _is_indexed(filesystem, path) and FileAccess.file_exists(path + ".import")

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

