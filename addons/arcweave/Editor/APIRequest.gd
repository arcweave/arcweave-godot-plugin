@tool
class_name APIRequest extends Node

signal project_updated(project_settings: Dictionary)
var http_request: HTTPRequest
var api_key : String
var project_hash : String
var env_vars : Dictionary
var default_domain = "https://arcweave.com/"

func _init(object : ArcweaveResource):
	http_request = HTTPRequest.new()
	add_child(http_request)
	http_request.request_completed.connect(_on_request_completed)
	api_key = object.api_key
	project_hash = object.project_hash

func request():
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
	print("[Arcweave] Retrieving: " + request_url)
	var error = http_request.request(request_url, headers)
	if error != OK:
		printerr(error)

func _on_request_completed(result: int, response_code: int, headers: PackedStringArray, body: PackedByteArray):
	if response_code != 200:
		printerr("[Arcweave] There was an error retrieving the Godot Export")
		printerr("[Arcweave] Response Code: " + str(response_code))
		printerr(body.get_string_from_utf8())
		return
	var project_settings = JSON.parse_string(body.get_string_from_utf8())
	project_updated.emit(project_settings)

func _update_env_vars():
	if FileAccess.file_exists("res://addons/Arcweave/env.json"):
		var file = FileAccess.open("res://addons/Arcweave/env.json", FileAccess.READ)
		env_vars = JSON.parse_string(file.get_as_text())
		if "API_DOMAIN" in env_vars:
			default_domain = env_vars["API_DOMAIN"]
			if not default_domain.ends_with("/"):
				default_domain += "/"
		file.close()
