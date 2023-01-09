tool
extends EditorPlugin

var dock
var username: LineEdit
var api_key: LineEdit
var project_hash: LineEdit
var refresh_button: Button
var select_project_files_button: Button
var source_option_button: Button
var selected_folder: String
var selected_folder_input: LineEdit
var refresh_from_folder: Button
var folder_source: VBoxContainer
var api_source: VBoxContainer
var http_request
var story
var _file_dialog: EditorFileDialog
var env_vars: Dictionary = {}
var default_domain: String = "https://arcweave.com/"

func _enter_tree():
	dock = preload("res://addons/arcweave/Arcweave.tscn").instance()
	add_control_to_dock(DOCK_SLOT_RIGHT_UL, dock)
	
	folder_source = dock.get_node("VBoxContainer/FolderSource")
	api_source = dock.get_node("VBoxContainer/APISource")
	
	username = dock.get_node("VBoxContainer/APISource/Form/Inputs/UsernameInput")
	api_key = dock.get_node("VBoxContainer/APISource/Form/Inputs/APIInput")
	project_hash = dock.get_node("VBoxContainer/APISource/Form/Inputs/HashInput")
	
	selected_folder_input = dock.get_node("VBoxContainer/FolderSource/HBoxContainer/Inputs/SelectedFolder")
	refresh_from_folder = dock.get_node("VBoxContainer/FolderSource/RefreshFromFolder")
	refresh_from_folder.connect("pressed", self, "_refresh_from_folder")
	
	self._update_env_vars()
	
	var file = File.new()
	var saved_info = null
	if file.file_exists("res://addons/arcweave/data.save"):
		file.open("res://addons/arcweave/data.save", File.READ)
		saved_info = file.get_var(true)
		fill_input_data(saved_info)
	file.close()
	
	if selected_folder:
		refresh_from_folder.disabled = false
	
	refresh_button = dock.get_node("VBoxContainer/APISource/RefreshButton")
	refresh_button.connect("pressed", self, "_refresh_button_pressed")
	
	select_project_files_button = dock.get_node("VBoxContainer/FolderSource/SelectProjectFiles")
	select_project_files_button.connect("pressed", self, "_select_project_files_button_pressed")
	
	_file_dialog = EditorFileDialog.new()
	_file_dialog.mode = EditorFileDialog.MODE_OPEN_DIR
	_file_dialog.connect("dir_selected", self, "_project_dir_selected")
	_file_dialog.access = EditorFileDialog.ACCESS_FILESYSTEM
	_file_dialog.current_dir = "res://"
	
	
	var editor_interface = get_editor_interface()
	var base_control = editor_interface.get_base_control()
	base_control.add_child(_file_dialog)
	
	source_option_button = dock.get_node("VBoxContainer/SourceSelection/Inputs/OptionButton")
	source_option_button.connect("item_selected", self, "_change_source_button_pressed")
	if saved_info and 'source_option' in saved_info:
		source_option_button.select(saved_info.source_option)
		_change_source_button_pressed(saved_info.source_option)

	http_request = HTTPRequest.new()
	add_child(http_request)
	http_request.connect("request_completed", self, "_on_request_completed")
	
func fill_input_data(info):
	username.set_text(info.username)
	api_key.set_text(info.api_key)
	project_hash.set_text(info.project_hash)
	if 'selected_folder' in info:
		selected_folder_input.set_text(info.selected_folder)
		selected_folder = info.selected_folder

func _update_env_vars():
	var file = File.new()
	if file.file_exists("res://addons/arcweave/env.json"):
		file.open("res://addons/arcweave/env.json", File.READ)
		env_vars = JSON.parse(file.get_as_text()).result
		if "API_DOMAIN" in env_vars:
			default_domain = env_vars["API_DOMAIN"]
	file.close()

func _exit_tree():
	remove_control_from_docks(dock)
	get_editor_interface().get_base_control().remove_child(_file_dialog)
	dock.free()

func _change_source_button_pressed(option):
	var item_text = source_option_button.get_item_text(option)
	if item_text == "API":
		api_source.visible = true
		folder_source.visible = false
	else:
		api_source.visible = false
		folder_source.visible = true
	
	var info = {
		"username": username.get_text(),
		"api_key": api_key.get_text(),
		"project_hash": project_hash.get_text(),
		"source_option": option,
	}
	
	if selected_folder:
		info.selected_folder = selected_folder
	
	self._save_details(info)

func _save_details(info):
	var file = File.new()
	file.open("res://addons/arcweave/data.save", File.WRITE)
	file.store_var(info)
	file.close()

func _refresh_button_pressed():
	self._update_env_vars()
	
	var info = {
		"username": username.get_text(),
		"api_key": api_key.get_text(),
		"project_hash": project_hash.get_text(),
		"source_option": source_option_button.get_selected_id(),
	}
	if selected_folder:
		info.selected_folder = selected_folder
	self._save_details(info)
	
	var request_url = self.default_domain + "api/"+info.project_hash+"/godot"
	var headers = []
	var auth_header = false
	if "headers" in self.env_vars:
		for header in self.env_vars.headers:
			if header == "Authorization":
				auth_header = true
				headers.append("Authorization: "+self.env_vars.headers[header]+", Bearer "+info.api_key)
			else:
				headers.append(header + ': ' + self.env_vars.headers[header])
	if not auth_header:
		headers.append("Authorization: Bearer "+info.api_key)
	print("Retrieving: "+request_url)
	http_request.request(request_url, headers)

func _select_project_files_button_pressed():
	_file_dialog.popup_centered_ratio(0.5)

func _project_dir_selected(dir: String):
	selected_folder_input.set_text(dir)
	selected_folder = dir
	refresh_from_folder.disabled = false

func _refresh_from_folder():
	var file = File.new()
	file.open(selected_folder+"/data_export.gd", File.READ)
	var data = file.get_as_text()
	file.close()
	file = File.new()
	file.open("res://addons/arcweave/data_export.gd", File.WRITE)
	file.store_string(data)
	file.close()
	
	file = File.new()
	file.open(selected_folder+"/state_export.gd", File.READ)
	var state = file.get_as_text()
	file.close()
	file = File.new()
	file.open("res://addons/arcweave/state_export.gd", File.WRITE)
	file.store_string(state)
	file.close()
	print("Arcweave: Successfully refreshed from folder!")
	

func _on_request_completed(result, response_code, headers, body):
	if response_code != 200:
		print("There was an error retrieving the Godot Export")
		print("Response Code: "+ str(response_code))
		print(body.get_string_from_utf8())
		return
	else:
		print("Arcweave: Successfully refreshed from API!")
	var res = JSON.parse(body.get_string_from_utf8()).result
	var file = File.new()
	file.open("res://addons/arcweave/data_export.gd", File.WRITE)
	file.store_string(res.data)
	file.close()
	file = File.new()
	file.open("res://addons/arcweave/state_export.gd", File.WRITE)
	file.store_string(res.state)
	file.close()

