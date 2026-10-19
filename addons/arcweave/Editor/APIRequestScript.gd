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
	# Reads up to 1 MiB of what arrived each frame instead of 64 KiB, since the editor processes only 10 frames a second
	# while it is in the background. Not threaded, since canceling a threaded request blocks until the server responds.
	http_request.download_chunk_size = 1 << 20
	add_child(http_request)
	http_request.request_completed.connect(_on_request_completed)

func request():
	var request_url = get_api_url() + project_hash + "/godot"
	var headers = get_headers()
	print("[Arcweave] Retrieving: " + request_url)
	var error = http_request.request(request_url, headers)
	if error != OK:
		printerr(error)

## Returns the base url of Arcweave's web API, eg. "https://arcweave.com/api/"
func get_api_url() -> String:
	_update_settings()
	return default_domain + "api/"

## Returns the headers that authenticate a request to Arcweave's web API
func get_headers() -> PackedStringArray:
	_update_settings()
	var headers = PackedStringArray()
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
	# Makes authentication errors respond with 401 instead of redirecting to the login page
	headers.append("Accept: application/json")
	return headers

func _update_settings():
	api_key = arcweave_asset.api_key
	project_hash = arcweave_asset.project_hash
	_update_env_vars()

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
	var response = JSON.parse_string(body.get_string_from_utf8())
	var project_settings = response["project"]
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
