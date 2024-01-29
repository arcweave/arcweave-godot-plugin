@tool
class_name ArcweaveAsset extends Resource

signal project_updated()
@export var use_api : bool = false
@export var api_key : String
@export var project_hash : String
@export var project_file : String
@export var project_settings : Dictionary

func _init(_project_settings = {}, _api_key = "", _project_hash = ""):
	project_settings = _project_settings
	api_key = _api_key
	project_hash = _project_hash

func refresh_project(api_request: APIRequest):
	if use_api:
		print("[Arcweave] Refreshing from API")
		_refresh_from_api(api_request)
		return
	print("[Arcweave] Refreshing from file")	
	_refresh_from_file()

func _refresh_from_api(api_request: APIRequest):
	api_request.project_updated.connect(_on_project_updated.bind(api_request))
	api_request.request()

func _on_project_updated(new_project_settings, api_request : APIRequest):
	project_settings = new_project_settings
	api_request.project_updated.disconnect(_on_project_updated)
	print("[Arcweave] Successfully refreshed from API!")
	project_updated.emit(new_project_settings)

func _refresh_from_file():
	if not FileAccess.file_exists(project_file):
		printerr("[Arcweave] Project File not found in path: \"" + project_file + "\"")
	var file = FileAccess.open(project_file, FileAccess.READ)
	var data = file.get_as_text()
	file.close()
	project_settings = JSON.parse_string(data)
	project_updated.emit(project_settings)
