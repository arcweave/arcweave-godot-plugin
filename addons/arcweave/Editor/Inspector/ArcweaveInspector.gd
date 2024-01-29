@tool
extends EditorInspectorPlugin

var handled_properties : PackedStringArray
var api_request : APIRequest
var ArcweavePropertyEditor = preload("res://addons/arcweave/Editor/Inspector/ArcweavePropertyEditor.gd")

func _init():
	handled_properties = PackedStringArray(["use_api", "project_file", "api_key", "project_hash"])

# We are creating 
func _can_handle(object):
	return object is ArcweaveAsset

func _parse_begin(object):
	add_property_editor_for_multiple_properties("Arcweave Project Settings", handled_properties, ArcweavePropertyEditor.new())
	var InitButton = Button.new()
	api_request = APIRequest.new(object)
	InitButton.add_child(api_request)
	InitButton.add_theme_color_override("font_color", Color.DARK_ORANGE)
	InitButton.text = "Initialize Arcweave Asset"
	InitButton.pressed.connect(pressed.bind(object))
	add_custom_control(InitButton)

func _parse_property(object, type, name, hint_type, hint_string, usage_flags, wide):
	if (handled_properties.has(name)):
		return true
	return false

func pressed(object: ArcweaveAsset):
	object.refresh_project(api_request)
