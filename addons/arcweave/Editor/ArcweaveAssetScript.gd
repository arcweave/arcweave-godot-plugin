@tool
class_name ArcweaveAsset extends Resource

signal project_updated(project_settings)
@export_group("File Settings")
## The JSON file exported from Arcweave
@export_global_file() var project_file : String 
@export_group("API Settings")
## The API Key of your Arcweave account
@export var api_key : String
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

## True while the media of this resource is being downloaded
var is_downloading_media : bool = false

func _init(_project_settings = {}, _api_key = "", _project_hash = ""):
	project_settings = _project_settings
	api_key = _api_key
	project_hash = _project_hash

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

func refresh_project(api_request: APIRequest):
	if receive_method == "WebAPI":
		print("[Arcweave] Refreshing from API")
		_refresh_from_api(api_request)
		return
	print("[Arcweave] Refreshing from file")
	_refresh_from_file()

func _refresh_from_api(api_request: APIRequest):
	api_request.project_updated.connect(_on_project_updated.bind(api_request), CONNECT_ONE_SHOT)
	api_request.request()

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
