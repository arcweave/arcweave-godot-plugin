@tool
extends EditorPlugin

var dock
var env_vars: Dictionary
var default_domain = "https://arcweave.com/"

var folder_source: VBoxContainer
var api_source: VBoxContainer
var username: LineEdit
var api_key: LineEdit
var project_hash: LineEdit
var selected_folder_input: LineEdit
var selected_folder: String
var refresh_from_folder: Button
var refresh_button: Button
var select_project_files_button: Button
var source_option_button: OptionButton
var _file_dialog: EditorFileDialog
var http_request: HTTPRequest

func _enter_tree():
	dock = preload("res://addons/arcweave/Arcweave.tscn").instantiate()
	add_control_to_dock(DOCK_SLOT_RIGHT_UL, dock)
	
	folder_source = dock.get_node("VBoxContainer/FolderSource")
	api_source = dock.get_node("VBoxContainer/APISource")
	
	username = dock.get_node("VBoxContainer/APISource/Form/Inputs/UsernameInput")
	api_key = dock.get_node("VBoxContainer/APISource/Form/Inputs/APIInput")
	project_hash = dock.get_node("VBoxContainer/APISource/Form/Inputs/HashInput")
	
	selected_folder_input = dock.get_node("VBoxContainer/FolderSource/HBoxContainer/Inputs/SelectedFolder")
	refresh_from_folder = dock.get_node("VBoxContainer/FolderSource/RefreshFromFolder")
	refresh_from_folder.pressed.connect(_refresh_from_folder)

	refresh_button = dock.get_node("VBoxContainer/APISource/RefreshButton")
	refresh_button.pressed.connect(_refresh_from_api)

	select_project_files_button = dock.get_node("VBoxContainer/FolderSource/SelectProjectFiles")
	select_project_files_button.pressed.connect(_select_project_files_button_pressed)

	source_option_button = dock.get_node("VBoxContainer/SourceSelection/Inputs/OptionButton")
	source_option_button.item_selected.connect(_change_source_button_pressed)

	_update_env_vars()
	
	var file = null
	var saved_info = null
	if FileAccess.file_exists("res://addons/arcweave/data.save"):
		file = FileAccess.open("res://addons/arcweave/data.save", FileAccess.READ)
		saved_info = file.get_var(true)
		_fill_input_data(saved_info)
		file.close()
	
	if selected_folder:
		refresh_from_folder.disabled = true
	
	_file_dialog = EditorFileDialog.new()
	_file_dialog.file_mode = EditorFileDialog.FILE_MODE_OPEN_DIR
	_file_dialog.dir_selected.connect(_project_dir_selected)
	_file_dialog.access = EditorFileDialog.ACCESS_FILESYSTEM
	_file_dialog.current_dir = "res://"
	
	var editor_interface = get_editor_interface()
	var base_control = editor_interface.get_base_control()
	base_control.add_child(_file_dialog)
	
	if saved_info and 'source_option' in saved_info:
		source_option_button.select(saved_info.source_option)
		_change_source_button_pressed(saved_info.source_option)

	http_request = HTTPRequest.new()
	add_child(http_request)
	http_request.request_completed.connect(_on_request_completed)
	
func _exit_tree():
	remove_control_from_docks(dock)
	get_editor_interface().get_base_control().remove_child(_file_dialog)
	dock.free()

func _update_env_vars():
	if FileAccess.file_exists("res://addons/arcweave/env.json"):
		var file = FileAccess.open("res://addons/arcweave/env.json", FileAccess.READ)
		env_vars = JSON.parse_string(file.get_as_text())
		if "API_DOMAIN" in env_vars:
			default_domain = env_vars["API_DOMAIN"]
		file.close()

func _fill_input_data(info: Dictionary):
	username.set_text(info.username)
	api_key.set_text(info.api_key)
	project_hash.set_text(info.project_hash)
	if 'selected_folder' in info:
		selected_folder_input.set_text(info.selected_folder)
		selected_folder = info.selected_folder

func _refresh_from_folder():
	var file = FileAccess.open(selected_folder+"/data_export.gd", FileAccess.READ)
	var data = file.get_as_text()
	file.close()
	file = FileAccess.open(selected_folder+"/state_export.gd", FileAccess.READ)
	var state = file.get_as_text()
	file.close()
	
	_save_project_files(data, state)
	
	print("Arcweave: Succesfully refreshed from folder!")

func _refresh_from_api():
	_update_env_vars()
	
	var info = {
		"username": username.text,
		"api_key": api_key.text,
		"project_hash": project_hash.text,
		"source_option": source_option_button.get_selected_id(),
	}
	
	if selected_folder:
		info.selected_folder = selected_folder
	_save_details(info)
	
	var request_url = default_domain + "api/"+info.project_hash+"/godot/v4"
	var headers = []
	var auth_header = false
	
	if "headers" in env_vars:
		for header in env_vars.headers:
			if header == "Authorization":
				auth_header = true
				headers.append("Authorization: " + env_vars.headers[header] + ", Bearer "+info.api_key)
			else:
				headers.append(header + ': ' + env_vars.headers[header])
	if not auth_header:
		headers.append("Authorization: Bearer " + info.api_key)
	print("Retrieving: " + request_url)
	http_request.request(request_url, headers)
	
func _on_request_completed(result: int, response_code: int, headers: PackedStringArray, body: PackedByteArray):
	if response_code != 200:
		print("There was an error retrieving the Godot Export")
		print("Response Code: "+ str(response_code))
		print(body.get_string_from_utf8())
		return
	else:
		print("Arcweave: Successfully refresh from API!")
	
	var res = JSON.parse_string(body.get_string_from_utf8())
	_save_project_files(res.data, res.state)

func _save_project_files(data: String, state: String):
	var file = FileAccess.open("res://addons/arcweave/data_export.gd", FileAccess.WRITE)
	file.store_string(data)
	file.close()
	file = FileAccess.open("res://addons/arcweave/state_export.gd", FileAccess.WRITE)
	file.store_string(state)
	file.close()

func _select_project_files_button_pressed():
	_file_dialog.popup_centered_ratio(0.5)

func _project_dir_selected(dir: String):
	selected_folder_input.set_text(dir)
	selected_folder = dir
	refresh_from_folder.disabled = false

func _change_source_button_pressed(option):
	var item_text = source_option_button.get_item_text(option)
	if item_text == "API":
		api_source.visible = true
		folder_source.visible = false
	else:
		api_source.visible = false
		folder_source.visible = true
	
	var info = {
		"username": username.text,
		"api_key": api_key.text,
		"project_hash": project_hash.text,
		"source_option": option,
	}
	
	if selected_folder:
		info.selected_folder = selected_folder
	
	_save_details(info)

func _save_details(info):
	var file = FileAccess.open("res://addons/arcweave/data.save", FileAccess.WRITE)
	file.store_var(info)
	file.close()
