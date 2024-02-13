@tool
class_name ArcweaveAsset extends Resource

signal project_updated()
@export_group("File Settings")
## The JSON file exported from Arcweave
@export_global_file() var project_file : String 
@export_group("API Settings")
## The API Key of your Arcweave account
@export var api_key : String
## The Project Hash
@export var project_hash : String

@export_group("")
## The retrieve method to use
@export_enum("File", "WebAPI") var receive_method: String = "File" 
@export var _placeholder : bool = false

@export_category("Retrieved Settings")
## The retrieved Project Settings
@export var project_settings : Dictionary

func _init(_project_settings = {}, _api_key = "", _project_hash = ""):
	project_settings = _project_settings
	api_key = _api_key
	project_hash = _project_hash

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
