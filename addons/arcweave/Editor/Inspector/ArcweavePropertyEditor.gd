extends EditorProperty

# The main control for editing the properties.
var source_control : CheckButton = CheckButton.new()
var api_line : LineEdit = LineEdit.new()
var project_hash_line : LineEdit = LineEdit.new()
var project_file_line : LineEdit = LineEdit.new()

var api_container : VBoxContainer = VBoxContainer.new()
var file_container :VBoxContainer = VBoxContainer.new()

var file_dialog : EditorFileDialog = EditorFileDialog.new()

var current_value = false
# A guard against internal changes when the property is updated.
var updating = false

func _init():
	# Add the control as a direct child of EditorProperty node.
	var container = VBoxContainer.new()
	# Source Selector
	var label = Label.new()
	label.text = "File"
	var source_container = VBoxContainer.new()
	var row = HBoxContainer.new()
	row.add_child(label)
	row.add_child(source_control)
	label = Label.new()
	label.text = "API"
	row.add_child(label)
	source_container.add_child(row)
	container.add_child(source_container)
	
	# API Selector
	row = HBoxContainer.new()
	label = Label.new()
	label.text = "API Key"
	row.add_child(label)
	api_line.text_changed.connect(_on_api_key_changed)
	row.add_child(api_line)
	api_container.add_child(row)
	
	row = HBoxContainer.new()
	label = Label.new()
	label.text = "Project Hash"
	row.add_child(label)
	project_hash_line.text_changed.connect(_on_project_hash_changed)
	row.add_child(project_hash_line)
	api_container.add_child(row)
	container.add_child(api_container)
	
	# File Selector
	row = HBoxContainer.new()
	label = Label.new()
	label.text = "Project File"
	row.add_child(label)
	project_file_line.editable = false
	project_file_line.focus_entered.connect(_select_project_file_button_pressed)
	row.add_child(project_file_line)
	file_container.add_child(row)
	
	row = HBoxContainer.new()
	var folder_button = Button.new()
	folder_button.text = "Select Project File"
	row.add_child(folder_button)
	file_container.add_child(row)
	container.add_child(file_container)
	
	
	add_child(container)
	set_bottom_editor(container)

	source_control.pressed.connect(_on_button_pressed)
	
	file_dialog.file_mode = EditorFileDialog.FILE_MODE_OPEN_FILE
	file_dialog.access = EditorFileDialog.ACCESS_FILESYSTEM
	file_dialog.file_selected.connect(_on_file_selected)
	file_dialog.canceled.connect(project_file_line.release_focus)
	folder_button.pressed.connect(_select_project_file_button_pressed)
	
	var base_control = EditorInterface.get_base_control()
	base_control.add_child(file_dialog)

func _on_button_pressed():
	# Ignore the signal if the property is currently being updated.
	if (updating):
		return

	current_value = !current_value
	source_control.button_pressed = current_value
	
	emit_changed("use_api", current_value)

func _select_project_file_button_pressed():
	file_dialog.popup_centered_ratio(0.5)

func _on_file_selected(path: String):
	if (updating):
		return
	project_file_line.release_focus()
	project_file_line.text = path
	
	emit_changed("project_file", path)

func _on_api_key_changed(new_text):
	if (updating):
		return
	emit_changed("api_key", new_text)

func _on_project_hash_changed(new_text):
	if (updating):
		return
	emit_changed("project_hash", new_text)

func _update_property():
	var object : ArcweaveAsset = get_edited_object()
	
	updating = true
	if current_value != object.use_api:
		current_value = object.use_api
		source_control.button_pressed = object.use_api
	if api_line.text != object.api_key:
		api_line.text = object.api_key
	if project_hash_line.text != object.project_hash:
		project_hash_line.text = object.project_hash
	if project_file_line.text != object.project_file:
		project_file_line.text = object.project_file
	updating = false
	
	api_container.visible = object.use_api
	file_container.visible = not object.use_api
