@tool
class_name APIRequest extends Node

## Emitted when a request ends: the project data on success, or null on failure.
signal project_updated(project_settings)

## Seconds before a project request gives up
const REQUEST_TIMEOUT = 30.0

var http_request: HTTPRequest
var is_requesting := false
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
	http_request.timeout = REQUEST_TIMEOUT
	add_child(http_request)
	http_request.request_completed.connect(_on_request_completed)

## Leaving the tree cancels the HTTPRequest without emitting request_completed
func _exit_tree():
	cancel()

## Cancels the pending project request, if any, and reports it as failed.
func cancel():
	if not is_requesting:
		return
	http_request.cancel_request()
	is_requesting = false
	printerr("[Arcweave] Project request cancelled")
	project_updated.emit(null)

func request():
	if is_requesting:
		# The pending request will emit project_updated when it ends
		printerr("[Arcweave] A project request is already in progress")
		return
	_update_settings()
	if api_key.strip_edges().is_empty() or project_hash.strip_edges().is_empty():
		printerr("[Arcweave] Set the API key and project hash in the ArcweaveAsset to refresh from the API")
		project_updated.emit(null)
		return
	var request_url = get_api_url() + project_hash + "/godot"
	var headers = get_headers()
	print("[Arcweave] Retrieving: " + request_url)
	var error = http_request.request(request_url, headers)
	if error != OK:
		printerr("[Arcweave] Could not send the request (error " + str(error) + ")")
		project_updated.emit(null)
		return
	is_requesting = true

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
	is_requesting = false
	if result != HTTPRequest.RESULT_SUCCESS:
		printerr("[Arcweave] Could not reach the Arcweave API (HTTPRequest result " + str(result) + ")")
		project_updated.emit(null)
		return
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
	if not response is Dictionary or not ArcweaveAsset.is_valid_project(response.get("project")):
		printerr("[Arcweave] The API response does not contain a valid project")
		project_updated.emit(null)
		return
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
