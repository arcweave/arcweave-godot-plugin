@tool
extends EditorInspectorPlugin

var api_request : APIRequest

func _can_handle(object):
	return object is ArcweaveAsset

func _parse_property(object, type, name, hint_type, hint_string, usage_flags, wide):
	if name == "_placeholder":
		var InitButton = Button.new()
		api_request = APIRequest.new(object)
		InitButton.add_child(api_request)
		InitButton.add_theme_color_override("font_color", Color.DARK_ORANGE)
		InitButton.text = "Initialize Arcweave Asset"
		InitButton.pressed.connect(pressed.bind(object))
		add_custom_control(InitButton)
		return true
	return false

func pressed(object: ArcweaveAsset):
	object.project_updated.connect(on_project_updated.bind(object), CONNECT_ONE_SHOT)
	object.refresh_project(api_request)

func on_project_updated(new_project_settings, object: ArcweaveAsset):
	if new_project_settings == null:
		return
	var error = ResourceSaver.save(object, object.resource_path)
	if error != OK:
		printerr("[Arcweave] Error saving resource!")
		printerr(error)
