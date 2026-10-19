@tool
class_name ArcweaveMediaDownloader extends Node
## Downloads the images and audio of a project initialized from the WebAPI and links them to its ArcweaveAsset.

## Emitted as each download progresses and while waiting to retry a failed download
signal progress_changed(info: String, value: float)

const IMAGE_EXTENSIONS = ["png", "jpg", "jpeg", "webp", "svg", "bmp", "tga"]
const AUDIO_EXTENSIONS = ["mp3", "ogg", "wav"]
const MAX_ATTEMPTS = 5
const MAX_RETRY_SECONDS = 60
const MAX_BACKOFF_SECONDS = 8
const MAX_REDIRECTS = 5
# A connection that drops, eg. over a flaky IPv6 route, is retried after a short wait, since a new one usually connects
const FAST_CONNECT_TIMEOUT_SECONDS = 10
const FAST_CONNECT_ATTEMPTS = 2
# Connecting may try each address of the server in turn, eg. an IPv6 address that never answers before an IPv4 one, and
# the editor waits up to network/limits/tcp/connect_timeout_seconds for each
const CONNECT_TIMEOUT_SECONDS = 90
## How long a request may go without receiving anything before it is retried
const STALL_TIMEOUT_SECONDS = 20
const CHUNKED_STALL_TIMEOUT_SECONDS = 120
const IDLE_POLL_MSEC = 1
const RETRY_CODES = [500, 502, 503, 504, 520, 521, 522, 523, 524]
const RETRY_RESULTS = [HTTPRequest.RESULT_CANT_CONNECT, HTTPRequest.RESULT_CANT_RESOLVE, HTTPRequest.RESULT_CONNECTION_ERROR,
	HTTPRequest.RESULT_NO_RESPONSE, HTTPRequest.RESULT_TIMEOUT]
# Failures that would fail every other download too, so the remaining downloads are skipped
const UNREACHABLE_RESULTS = [HTTPRequest.RESULT_CANT_CONNECT, HTTPRequest.RESULT_CANT_RESOLVE,
	HTTPRequest.RESULT_CONNECTION_ERROR, HTTPRequest.RESULT_NO_RESPONSE, HTTPRequest.RESULT_TIMEOUT,
	HTTPRequest.RESULT_TLS_HANDSHAKE_ERROR]
const POLL_SECONDS = 0.1
const IMPORT_TIMEOUT_SECONDS = 120
const MAX_IMPORT_SCANS = 3
const SCAN_START_SECONDS = 0.5
# File systems limit names to 255 bytes, so this leaves room for the extension, the unique suffix and the hash the
# editor appends to imported files
const MAX_NAME_BYTES = 100
const INVALID_CHARACTERS = "<>:\"/\\|?*"
const RESERVED_NAMES = ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
	"LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³"]

## True once canceled. The remaining downloads are skipped.
var canceled : bool = false
var arcweave_asset : ArcweaveAsset
var _api_request : APIRequest
# Every download reuses one connection, instead of connecting to the server for each file. Requests run on a thread,
# which is the only one using the connection while a request runs.
var _client : HTTPClient
var _client_origin : String
var _connect_timeouts = 0
var _thread : Thread
# Guards canceled and the progress of the request in flight, which the request thread shares with the main thread
var _mutex = Mutex.new()
var _status : String
var _status_fraction : float
# Decompresses compressed responses on the request thread
var _decompressor : RefCounted

func _init(object : ArcweaveAsset):
	arcweave_asset = object
	_api_request = APIRequest.new(object)
	add_child(_api_request)

func _notification(what):
	# The download needs the tree, so it is canceled when the downloader leaves it, and the request thread never outlives
	# the downloader
	if what == NOTIFICATION_EXIT_TREE or what == NOTIFICATION_PREDELETE:
		cancel()
		if _thread != null:
			_thread.wait_to_finish()
			_thread = null

## Cancels the download. Media that was not downloaded keeps its previous copy.
func cancel():
	_mutex.lock()
	canceled = true
	_mutex.unlock()

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
	_decompressor = load("res://addons/arcweave/Editor/MediaFileWriter.cs").new()
	# Media is already compressed, so it is requested as is. Responses compressed anyway, eg. when env.json asks for it,
	# are decompressed when they arrive.
	if not Array(headers).any(func(header : String): return header.to_lower().begins_with("accept-encoding:")):
		headers.append("Accept-Encoding: identity")
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
		var response = await _fetch(url, headers, info, float(i) / ids.size(), 1.0 / ids.size())
		if canceled:
			break
		if response[0] == HTTPRequest.RESULT_SUCCESS and response[1] == 200:
			downloads[id] = response[3]
			continue
		push_warning("[Arcweave] Could not download '%s': %s." % [asset_name, _describe(response)])
		if response[0] in UNREACHABLE_RESULTS or response[1] == 401:
			push_warning("[Arcweave] Skipping the remaining media downloads. Media that was not downloaded keeps its previous copy.")
			break
	_close_client()
	_decompressor = null
	if canceled:
		push_warning("[Arcweave] Media download canceled. Media that was not downloaded keeps its previous copy.")

	# Nothing awaits until the files are written, so the paths resolved there are still current.
	if not _is_saved(asset):
		push_warning("[Arcweave] Skipping media update because the ArcweaveAsset was deleted during the download.")
		asset.is_downloading_media = false
		return
	var saved = _save_media(asset, media, downloads)
	await _link_resources(asset, saved.written, saved.replaced)
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
	if Engine.is_editor_hint() and Engine.has_singleton("EditorInterface"):
		var filesystem = Engine.get_singleton("EditorInterface").get_resource_filesystem()
		# A scan forgets the deleted files. It is skipped while the editor imports, which would crash it, and the
		# editor's next scan updates the FileSystem dock instead.
		if not _is_busy(filesystem):
			filesystem.scan()
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
	var writer = load("res://addons/arcweave/Editor/MediaFileWriter.cs").new()
	for id in downloads:
		var file_name = _file_name(writer.Normalize(media[id].name))
		var previous_path : String = previous_paths.get(id, "")
		var path = previous_path
		# A file keeps its path once it is downloaded, even when its asset is renamed in Arcweave, so the scenes that use
		# it keep working. Only a changed extension needs a new file.
		if previous_path.is_empty() or previous_path.get_extension().to_lower() != file_name.get_extension().to_lower():
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
			# Named after the asset, or "name 2.ext" when the name is taken, so files the downloader does not own, or that
			# belong to other assets, are never overwritten
			path = _unique_path(folder.path_join(file_name.get_basename()), file_name.get_extension(), writer)
		var bytes : PackedByteArray = downloads[id]
		var sha256 = _sha256(bytes)
		if owned.get(path) != sha256:
			var replacing = FileAccess.file_exists(path)
			var error : String = writer.Write(path, bytes)
			if not error.is_empty():
				# Eg. a read-only file checked in to version control
				push_warning("[Arcweave] Could not save '%s' to %s: %s" % [media[id].name, path, error])
				continue
			owned[path] = sha256
			written.append(path)
			if replacing:
				replaced.append(path)
		paths[id] = path
		saved += 1

	asset.downloaded_media = paths
	asset.owned_media = owned
	asset.emit_changed()
	print("[Arcweave] Downloaded %d of %d media files for %s" % [saved, media.size(), asset_path.get_file()])
	return { "written": written, "replaced": replaced }

## Imports the written files in one go and links the imported resources to the ArcweaveAsset.
## Outside the editor the files can't be imported, so the resources are left as they are.
func _link_resources(asset : ArcweaveAsset, written_paths : PackedStringArray, replaced_paths : PackedStringArray):
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
				filesystem.scan()
				scans += 1
				# Gives the scan time to start, so a scan request that was ignored is retried
				await get_tree().create_timer(SCAN_START_SECONDS).timeout
				# Canceled, also when the downloader left the tree
				if canceled:
					break
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

## Returns the file name of an asset: its Arcweave name made safe on every platform, and shortened so the files the
## editor generates for it stay within file name limits
static func _file_name(asset_name : String) -> String:
	var file_name = _sanitize(asset_name)
	var extension = file_name.get_extension()
	var stem = file_name.get_basename() if not extension.is_empty() else file_name
	if stem.to_utf8_buffer().size() > MAX_NAME_BYTES:
		# Shortened by whole characters, then trimmed again, since shortening can leave a trailing dot or space
		while stem.to_utf8_buffer().size() > MAX_NAME_BYTES:
			stem = stem.left(-1)
		stem = _sanitize(stem)
	return stem + ("." + extension if not extension.is_empty() else "")

## Makes a file name safe on every platform, so a name can never leave the media folder
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

## Downloads a file, retrying when rate limited, when the connection fails and on server errors.
## Returns the last response, as [result, response_code, headers, body].
func _fetch(url : String, headers : PackedStringArray, info : String, value : float, step : float) -> Array:
	var response = _response(HTTPRequest.RESULT_REQUEST_FAILED)
	for attempt in range(1, MAX_ATTEMPTS + 1):
		if canceled:
			break
		progress_changed.emit(info, value)
		var start = Time.get_ticks_msec()
		response = await _request(url, headers, func(status : String, fraction : float):
			progress_changed.emit("%s (%s)" % [info, status], value + fraction * step))
		if canceled:
			break
		print_verbose("[Arcweave] GET %s: %s in %d ms" % [url, _describe(response), Time.get_ticks_msec() - start])
		var delay = _retry_delay_seconds(response, attempt)
		if delay < 0 or attempt == MAX_ATTEMPTS:
			break
		var reason = _describe(response)
		var retry_at = Time.get_ticks_msec() + int(delay * 1000)
		while Time.get_ticks_msec() < retry_at and not canceled:
			var seconds = ceili((retry_at - Time.get_ticks_msec()) / 1000.0)
			progress_changed.emit("%s (%s, retrying in %ds)" % [info, reason, seconds], value)
			await get_tree().create_timer(POLL_SECONDS).timeout
	return response

## Sends a GET request, following redirects, and returns [result, response_code, headers, body]. report(status, fraction)
## is called as the request progresses.
func _request(url : String, headers : PackedStringArray, report : Callable) -> Array:
	var target = _parse_url(url)
	if target.is_empty():
		return _response(HTTPRequest.RESULT_CANT_CONNECT)
	for _redirect in MAX_REDIRECTS + 1:
		var reused = _client != null and _client_origin == target.origin
		var response = await _send(target, headers, report)
		# The server may close a connection while it waits between requests, so a reused connection that fails before
		# responding is tried again on a new one
		if reused and not canceled and response[1] == 0 and response[0] in [HTTPRequest.RESULT_CONNECTION_ERROR, HTTPRequest.RESULT_NO_RESPONSE]:
			response = await _send(target, headers, report)
		var location = _header(response[2], "Location")
		if canceled or response[0] != HTTPRequest.RESULT_SUCCESS or not response[1] in [301, 302, 303, 307, 308] or location.is_empty():
			return response
		var next = _parse_url(_resolve_url(target, location))
		# A redirect the downloader can't follow, eg. to another scheme, fails like any other unexpected response
		if next.is_empty():
			return response
		# The API key is only sent to the server it belongs to
		if next.origin != target.origin:
			headers = PackedStringArray(Array(headers).filter(func(header : String): return not header.to_lower().begins_with("authorization:")))
		target = next
	return _response(HTTPRequest.RESULT_REDIRECT_LIMIT_REACHED)

## Sends a GET request over the open connection, connecting first if there is none, and returns
## [result, response_code, headers, body]. The request runs on its own thread, so it doesn't depend on the editor's
## frame rate, which drops to 10 frames a second when the editor is in the background. The thread is not one of the
## WorkerThreadPool's, which the editor shares and may block on while this request waits.
func _send(target : Dictionary, headers : PackedStringArray, report : Callable) -> Array:
	_set_status("", 0.0)
	var thread = Thread.new()
	if thread.start(_send_blocking.bind(target, headers)) != OK:
		return _response(HTTPRequest.RESULT_REQUEST_FAILED)
	_thread = thread
	var reported = ""
	# The thread is waited for elsewhere if the downloader leaves the tree
	while _thread == thread and thread.is_alive():
		_mutex.lock()
		var status = _status
		var fraction = _status_fraction
		_mutex.unlock()
		if status != reported and not status.is_empty():
			reported = status
			report.call(status, fraction)
		await get_tree().process_frame
	if _thread != thread:
		return _response(HTTPRequest.RESULT_REQUEST_FAILED)
	_thread = null
	return thread.wait_to_finish()

## Sends a GET request and blocks until it ends. The connection stays open for the next request, unless it fails or
## the server closes it.
func _send_blocking(target : Dictionary, headers : PackedStringArray) -> Array:
	if _client != null:
		_client.poll()
		if _client_origin != target.origin or _client.get_status() != HTTPClient.STATUS_CONNECTED:
			_close_client()
	var client = _client
	if client == null:
		client = HTTPClient.new()
		_client = client
		_client_origin = target.origin
		if client.connect_to_host(target.host, target.port, TLSOptions.client() if target.tls else null) != OK:
			_close_client()
			return _response(HTTPRequest.RESULT_CANT_CONNECT)
		_set_status("connecting to %s" % target.host, 0.0)
		var timeout = FAST_CONNECT_TIMEOUT_SECONDS if _connect_timeouts < FAST_CONNECT_ATTEMPTS else CONNECT_TIMEOUT_SECONDS
		var deadline = Time.get_ticks_msec() + timeout * 1000
		while true:
			if _is_canceled():
				_close_client()
				return _response(HTTPRequest.RESULT_REQUEST_FAILED)
			client.poll()
			var status = client.get_status()
			if status == HTTPClient.STATUS_CONNECTED:
				break
			if status != HTTPClient.STATUS_RESOLVING and status != HTTPClient.STATUS_CONNECTING:
				_close_client()
				return _response(_result(status, false))
			if Time.get_ticks_msec() > deadline:
				_connect_timeouts += 1
				_close_client()
				return _response(HTTPRequest.RESULT_TIMEOUT)
			OS.delay_msec(IDLE_POLL_MSEC)

	if client.request(HTTPClient.METHOD_GET, target.path, headers) != OK:
		_close_client()
		return _response(HTTPRequest.RESULT_CONNECTION_ERROR)
	_set_status("waiting for the server", 0.0)
	var response_code = 0
	var response_headers = PackedStringArray()
	var length = -1
	var chunked = false
	var body = PackedByteArray()
	var last_activity = Time.get_ticks_msec()
	while true:
		if _is_canceled():
			_close_client()
			return _response(HTTPRequest.RESULT_REQUEST_FAILED)
		client.poll()
		var status = client.get_status()
		var progressed = false
		if response_code == 0 and client.has_response():
			response_code = client.get_response_code()
			response_headers = client.get_response_headers()
			length = client.get_response_body_length()
			chunked = client.is_response_chunked()
			progressed = true
		if status == HTTPClient.STATUS_BODY:
			var chunk = client.read_response_body_chunk()
			if not chunk.is_empty():
				body.append_array(chunk)
				progressed = true
			# Checked before polling again, which would see that a server that sent Connection: close closed it
			status = client.get_status()
		if status != HTTPClient.STATUS_REQUESTING and status != HTTPClient.STATUS_BODY:
			# A body without a length or chunks ends when the server closes the connection. A chunked body only ends with
			# its last chunk, so one cut short is never saved. A compressed body must have a length or chunks, since .NET
			# decompresses a truncated body without failing.
			var encoding = _header(response_headers, "Content-Encoding").to_lower()
			var compressed = not encoding in ["", "identity"]
			var complete = response_code != 0 and (status == HTTPClient.STATUS_CONNECTED
				or (length >= 0 and body.size() == length)
				or (status == HTTPClient.STATUS_DISCONNECTED and length < 0 and not chunked and not compressed))
			if not complete or status != HTTPClient.STATUS_CONNECTED or _header(response_headers, "Connection").to_lower() == "close":
				_close_client()
			if not complete:
				return _response(_result(status, response_code != 0), response_code, response_headers)
			# Only the media is decompressed, so a failed decompression never hides a response that is retried or followed
			if compressed and response_code == 200:
				body = _decompressor.Decompress(body, encoding)
				if body.is_empty():
					return _response(HTTPRequest.RESULT_BODY_DECOMPRESS_FAILED, response_code, response_headers)
			return [HTTPRequest.RESULT_SUCCESS, response_code, response_headers, body]

		var now = Time.get_ticks_msec()
		if progressed:
			last_activity = now
			if response_code != 0:
				var received = String.humanize_size(body.size())
				var fraction = float(body.size()) / length if length > 0 else 0.0
				_set_status(received + " of " + String.humanize_size(length) if length >= 0 else received, fraction)
		else:
			# The editor only returns a chunk once all of it arrived, so a large chunk on a slow connection looks stalled
			var stall_timeout = CHUNKED_STALL_TIMEOUT_SECONDS if chunked else STALL_TIMEOUT_SECONDS
			if now - last_activity > stall_timeout * 1000:
				_close_client()
				return _response(HTTPRequest.RESULT_TIMEOUT, response_code, response_headers)
			OS.delay_msec(IDLE_POLL_MSEC)
	return _response(HTTPRequest.RESULT_REQUEST_FAILED)

func _set_status(status : String, fraction : float):
	_mutex.lock()
	_status = status
	_status_fraction = fraction
	_mutex.unlock()

func _is_canceled() -> bool:
	_mutex.lock()
	var result = canceled
	_mutex.unlock()
	return result

func _close_client():
	if _client != null:
		_client.close()
		_client = null

## Returns the HTTPRequest result of a request that ended with the connection status
static func _result(status : int, responded : bool) -> int:
	match status:
		HTTPClient.STATUS_CANT_RESOLVE:
			return HTTPRequest.RESULT_CANT_RESOLVE
		HTTPClient.STATUS_CANT_CONNECT:
			return HTTPRequest.RESULT_CANT_CONNECT
		HTTPClient.STATUS_TLS_HANDSHAKE_ERROR:
			return HTTPRequest.RESULT_TLS_HANDSHAKE_ERROR
		HTTPClient.STATUS_DISCONNECTED:
			return HTTPRequest.RESULT_CONNECTION_ERROR if responded else HTTPRequest.RESULT_NO_RESPONSE
	return HTTPRequest.RESULT_CONNECTION_ERROR

static func _response(result : int, response_code : int = 0, headers : PackedStringArray = PackedStringArray()) -> Array:
	return [result, response_code, headers, PackedByteArray()]

## Resolves the Location of a redirect against the URL that was requested,
## following RFC 3986. Fragments are dropped, since they are never sent.
static func _resolve_url(target : Dictionary, location : String) -> String:
	location = location.get_slice("#", 0)
	if location.is_empty():
		return target.origin + target.path
	var scheme = RegEx.create_from_string("^[A-Za-z][A-Za-z0-9+.-]*:").search(location)
	if scheme != null:
		# Other schemes, eg. ftp: or mailto:, return an empty URL, which can't be parsed
		var name = scheme.get_string().to_lower()
		return location if name == "http:" or name == "https:" else ""
	if location.begins_with("//"):
		return ("https:" if target.tls else "http:") + location
	var base_path = target.path.get_slice("?", 0)
	if location.begins_with("?"):
		return target.origin + base_path + location
	var path = location.get_slice("?", 0)
	var query = location.substr(path.length())
	if not path.begins_with("/"):
		# Relative to the folder of the requested path, which is everything up to its last slash
		path = base_path.left(base_path.rfind("/") + 1) + path
	return target.origin + _remove_dot_segments(path) + query

## Removes the "." and ".." segments of an absolute URL path, keeping empty segments and the trailing slash
static func _remove_dot_segments(path : String) -> String:
	var segments = path.split("/")
	var output = PackedStringArray()
	for i in segments.size():
		var segment = segments[i]
		if segment == "." or segment == "..":
			# The first segment is the empty one before the leading slash, which ".." never removes
			if segment == ".." and output.size() > 1:
				output.remove_at(output.size() - 1)
			if i == segments.size() - 1:
				output.append("")
			continue
		output.append(segment)
	return "/".join(output)

## Splits an http or https URL into { tls, host, port, path, origin }, without its fragment and with the "." and ".."
## segments of its path removed. The origin has the lowercase host and the port,
## so URLs of the same server always have the same origin. Returns {} if it is not an http or https URL.
static func _parse_url(url : String) -> Dictionary:
	var tls = url.to_lower().begins_with("https://")
	if not tls and not url.to_lower().begins_with("http://"):
		return {}
	# The fragment is never sent
	var rest = url.get_slice("#", 0).substr(8 if tls else 7)
	# The authority ends at the path or, without one, at the query
	var end = rest.length()
	for separator in ["/", "?"]:
		if rest.find(separator) >= 0:
			end = mini(end, rest.find(separator))
	var authority = rest.left(end)
	var path = rest.substr(end)
	if not path.begins_with("/"):
		path = "/" + path
	var path_end = path.find("?") if path.contains("?") else path.length()
	path = _remove_dot_segments(path.left(path_end)) + path.substr(path_end)
	# Credentials in the URL are not supported
	if authority.contains("@"):
		return {}
	var host = authority
	var port = 443 if tls else 80
	# The port follows the last colon, unless the colon is part of an IPv6 address such as [::1]
	var colon = authority.rfind(":")
	if colon > authority.rfind("]"):
		if not authority.substr(colon + 1).is_valid_int():
			return {}
		port = authority.substr(colon + 1).to_int()
		host = authority.left(colon)
	host = host.trim_prefix("[").trim_suffix("]").to_lower()
	if host.is_empty():
		return {}
	var origin = "%s://%s:%d" % ["https" if tls else "http", "[%s]" % host if host.contains(":") else host, port]
	return { "tls": tls, "host": host, "port": port, "path": path, "origin": origin }

## Returns how long to wait before retrying a request, or -1 if it succeeded or retrying would not help
static func _retry_delay_seconds(response : Array, attempt : int) -> float:
	var result : int = response[0]
	var response_code : int = response[1]
	if result == HTTPRequest.RESULT_SUCCESS and response_code == 429:
		return _retry_after_seconds(_header(response[2], "Retry-After"))
	if (result == HTTPRequest.RESULT_SUCCESS and response_code in RETRY_CODES) or result in RETRY_RESULTS:
		return mini(1 << (attempt - 1), MAX_BACKOFF_SECONDS)
	return -1

## Describes the outcome of a request, eg. for a warning
static func _describe(response : Array) -> String:
	match response[0]:
		HTTPRequest.RESULT_SUCCESS:
			return "rate limited" if response[1] == 429 else "HTTP %d" % response[1]
		HTTPRequest.RESULT_CANT_RESOLVE:
			return "could not resolve the server's address"
		HTTPRequest.RESULT_CANT_CONNECT:
			return "could not connect to the server"
		HTTPRequest.RESULT_TLS_HANDSHAKE_ERROR:
			return "the TLS handshake failed"
		HTTPRequest.RESULT_NO_RESPONSE:
			return "the server closed the connection without responding"
		HTTPRequest.RESULT_CONNECTION_ERROR:
			return "the connection was lost"
		HTTPRequest.RESULT_TIMEOUT:
			return "the server stopped responding"
		HTTPRequest.RESULT_REDIRECT_LIMIT_REACHED:
			return "too many redirects"
		HTTPRequest.RESULT_BODY_DECOMPRESS_FAILED:
			return "could not decompress the response"
	return "error %d" % response[0]

## Returns how long to wait before retrying a rate limited request, from its Retry-After header in seconds or as a date,
## capped so a download never waits for long.
static func _retry_after_seconds(retry_after : String) -> float:
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
## Names are compared after normalizing them with the writer, when there is one
static func _unique_path(base : String, extension : String, writer = null) -> String:
	var suffix = "" if extension.is_empty() else "." + extension
	# Compared ignoring case and Unicode normalization, so the project also works on Windows and macOS when it was made on
	# Linux, and a name never overwrites a file that macOS stores with a decomposed accent
	var taken = {}
	var directory = DirAccess.open(base.get_base_dir())
	if directory != null:
		directory.include_hidden = true
		for entry in Array(directory.get_files()) + Array(directory.get_directories()):
			taken[_compare_key(entry, writer)] = true
	var path = base + suffix
	var number = 2
	while taken.has(_compare_key(path.get_file(), writer)) or FileAccess.file_exists(path) or DirAccess.dir_exists_absolute(path):
		path = "%s %d%s" % [base, number, suffix]
		number += 1
	return path

static func _compare_key(file_name : String, writer) -> String:
	return (writer.Normalize(file_name) if writer != null else file_name).to_lower()

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

