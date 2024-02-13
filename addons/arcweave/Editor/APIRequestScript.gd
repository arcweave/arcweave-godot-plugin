@tool
class_name APIRequest extends Node

signal project_updated(project_settings: Dictionary)
var http_request: HTTPRequest
var api_key : String
var project_hash : String
var env_vars : Dictionary
var default_domain = "https://arcweave.com/"
var arcweave_asset: ArcweaveAsset

func _init(object : ArcweaveAsset):
	arcweave_asset = object
	http_request = HTTPRequest.new()
	add_child(http_request)
	http_request.request_completed.connect(_on_request_completed)

func request():
	api_key = arcweave_asset.api_key
	project_hash = arcweave_asset.project_hash
	_update_env_vars()
	
	var request_url = default_domain + "api/"+project_hash+"/json"
	var headers = []
	var auth_header = false
	
	if "headers" in env_vars:
		for header in env_vars.headers:
			if header == "Authorization":
				auth_header = true
				headers.append("Authorization: " + env_vars.headers[header] + ", Bearer "+api_key)
			else:
				headers.append(header + ': ' + env_vars.headers[header])
	if not auth_header:
		headers.append("Authorization: Bearer " + api_key)
	headers.append("Accept: application/json")
	print("[Arcweave] Retrieving: " + request_url)
	var error = http_request.request(request_url, headers)
	if error != OK:
		printerr(error)

func _on_request_completed(result: int, response_code: int, headers: PackedStringArray, body: PackedByteArray):
	if response_code == 401:
		printerr("[Arcweave] Your API key is invalid")
		project_updated.emit(null)
		return
	if response_code == 403:
		printerr("[Arcweave] You do not have access to this project")
		project_updated.emit(null)
		return
	if response_code != 200:
		printerr("[Arcweave] There was an error retrieving the Godot Export")
		printerr("[Arcweave] Response Code: " + str(response_code))
		printerr(body.get_string_from_utf8())
		project_updated.emit(null)
		return
	var project_settings = JSON.parse_string(body.get_string_from_utf8())
	project_updated.emit(project_settings)

func _update_env_vars():
	if FileAccess.file_exists("res://addons/arcweave/env.json"):
		var file = FileAccess.open("res://addons/arcweave/env.json", FileAccess.READ)
		env_vars = JSON.parse_string(file.get_as_text())
		if "API_DOMAIN" in env_vars:
			default_domain = env_vars["API_DOMAIN"]
			if not default_domain.ends_with("/"):
				default_domain += "/"
		file.close()
