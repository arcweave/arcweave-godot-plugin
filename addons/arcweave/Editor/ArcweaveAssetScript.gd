@tool
class_name ArcweaveAsset extends Resource

signal project_updated(project_settings)
@export_group("File Settings")
## The JSON file exported from Arcweave
@export_global_file() var project_file : String 
@export_group("API Settings")
# The API key is not saved in the resource, since the resource is committed and exported with the game. The inspector
# saves it in the user settings instead, see ArcweaveUserSettings and resolve_api_key().
## The Project Hash
@export var project_hash : String
## Download the images and audio the project uses into a "<Resource Name> Media" folder next to this resource when
## initializing from the WebAPI in the editor, and link them to the project's assets.
## The resource then references the downloaded media, so it is loaded and exported along with it.
## Disabled by default. When disabled, assets only have their Arcweave folder path.
@export var download_media : bool = false

@export_group("")
## The retrieve method to use
@export_enum("File", "WebAPI") var receive_method: String = "File" 
@export var _placeholder : bool = false

@export_category("Retrieved Settings")
## The retrieved Project Settings
@export var project_settings : Dictionary

# Media downloaded by WebAPI initializations, as Arcweave asset id -> res:// path where it was saved.
@export_storage var downloaded_media : Dictionary = {}
# The imported downloaded media, as Arcweave asset id -> Resource. Referencing the resources makes exports include
# them and keeps the links when the files are moved.
@export_storage var media_resources : Dictionary = {}
# The files the downloader wrote, as res:// path -> SHA-256 of their contents. A file is only replaced or deleted
# while its contents still match, so files the downloader does not own are never touched.
@export_storage var owned_media : Dictionary = {}
@export_storage var media_folder : String = ""
# Detects duplicated resources, which must not manage the original resource's media
@export_storage var media_owner : String = ""
@export_storage var media_owner_path : String = ""

## True while the media of this resource is being downloaded
var is_downloading_media : bool = false

# The API key saved in the resource by an older version of the plugin. It is still saved, so it isn't lost before the
# inspector moves it to the user settings, and still used when no other key is found.
var _legacy_api_key : String = ""

## [param _api_key] is deprecated: it is saved in the resource like the API key of older versions of the plugin.
## Pass the key to [method refresh_project] instead.
func _init(_project_settings = {}, _api_key = "", _project_hash = ""):
	project_settings = _project_settings
	_legacy_api_key = _api_key
	project_hash = _project_hash

# Older versions of the plugin saved the API key in an exported "api_key" property. Loading such a resource sets it here,
# and it is saved again only while it isn't empty.
func _get_property_list():
	if _legacy_api_key.is_empty():
		return []
	return [{"name": "api_key", "type": TYPE_STRING, "usage": PROPERTY_USAGE_STORAGE}]

func _set(property, value):
	if property == "api_key":
		_legacy_api_key = "" if value == null else str(value)
		return true
	return false

func _get(property):
	if property == "api_key":
		return _legacy_api_key
	return null

## Returns true if the resource contains an API key saved by an older version of the plugin.
func has_legacy_api_key() -> bool:
	return not _legacy_api_key.strip_edges().is_empty()

## Returns the API key saved in the resource by an older version of the plugin, or an empty String.
func get_legacy_api_key() -> String:
	return _legacy_api_key.strip_edges()

## Removes the API key saved in the resource by an older version of the plugin. Save the resource to remove it from
## the file. ArcweaveUserSettings.move_api_key_out_of_asset() moves the key to the user settings first.
func clear_legacy_api_key():
	set_legacy_api_key("")

func set_legacy_api_key(key: String):
	_legacy_api_key = key
	notify_property_list_changed()

## Returns the API key refreshes from the WebAPI use: [param api_key] if it isn't empty, then the key saved in the user
## settings, then the ARCWEAVE_API_KEY environment variable, then a key saved in the resource by an older version of the
## plugin. Empty if there is none.
func resolve_api_key(api_key := "", warn_if_saved_in_resource := false) -> String:
	api_key = api_key.strip_edges()
	if not api_key.is_empty():
		return api_key
	api_key = ArcweaveUserSettings.resolve_api_key()
	if not api_key.is_empty() or not has_legacy_api_key():
		return api_key
	if warn_if_saved_in_resource:
		push_warning("[Arcweave] \"" + resource_path + "\" uses an API key saved in the resource, which commits it to "
			+ "version control and includes it in exports. Select the resource and use \"Move API Key Out of Resource\" in "
			+ "its inspector.")
	return get_legacy_api_key()

## Returns the downloaded media that still exists, as Arcweave asset id -> res:// path.
## Empty when download_media is disabled, so the project never relies on media it no longer downloads.
func get_media_paths() -> Dictionary:
	var paths = {}
	if not download_media:
		return paths
	for asset_id in downloaded_media:
		var path : String = downloaded_media[asset_id]
		# The resource follows the file when it is moved
		var resource = media_resources.get(asset_id)
		if resource is Resource and not resource.resource_path.is_empty():
			path = resource.resource_path
		if ResourceLoader.exists(path):
			paths[asset_id] = path
	return paths

## [param api_key] is the API key refreshes from the WebAPI use. When empty, the key is found like in
## [method resolve_api_key].
func refresh_project(api_request: APIRequest, api_key := ""):
	if receive_method == "WebAPI":
		print("[Arcweave] Refreshing from API")
		_refresh_from_api(api_request, api_key)
		return
	print("[Arcweave] Refreshing from file")
	_refresh_from_file()

func _refresh_from_api(api_request: APIRequest, api_key := ""):
	api_request.project_updated.connect(_on_project_updated.bind(api_request), CONNECT_ONE_SHOT)
	api_request.request(api_key)

func _on_project_updated(new_project_settings, api_request : APIRequest):
	if new_project_settings == null:
		printerr("[Arcweave] Project Update Failed")
		project_updated.emit(null)
		return
	project_settings = new_project_settings
	print("[Arcweave] Successfully refreshed from API!")
	project_updated.emit(new_project_settings)

func _refresh_from_file():
	if not FileAccess.file_exists(project_file):
		printerr("[Arcweave] Project File not found in path: \"" + project_file + "\"")
	var file = FileAccess.open(project_file, FileAccess.READ)
	var data = file.get_as_text()
	file.close()
	project_settings = JSON.parse_string(data)
	print("[Arcweave] Successfully refreshed from File!")
	project_updated.emit(project_settings)
