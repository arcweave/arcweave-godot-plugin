extends Node2D

var text_container: RichTextLabel
var option_container: VBoxContainer
var save_button: Button
var refresh_button: Button

var arcweave_asset: ArcweaveAsset = preload("res://ArcweaveAsset.tres")
var api_request: APIRequest
var Story = load("res://addons/arcweave/Story.cs")
var story

# Called when the node enters the scene tree for the first time.
func _ready():
	print("Using Godot Script ./GodotSceneScriptWithoutArcweaveNode.gd")

	api_request = APIRequest.new(arcweave_asset)
	arcweave_asset.project_updated.connect(_on_project_updated)
	add_child(api_request)

	story = Story.new(arcweave_asset.project_settings)
	
	text_container = get_node("TextContainer")
	option_container = get_node("OptionContainer")
	save_button = get_node("SaveButton")
	refresh_button = get_node("RefreshButton")
	refresh_button.pressed.connect(_on_refresh_pressed)
	
	text_container.bbcode_enabled = true
	repaint()

func _on_project_updated(project_settings):
	story.UpdateStory(project_settings)
	repaint()

func _on_refresh_pressed():
	arcweave_asset.refresh_project(api_request)

func repaint():
	text_container.text = story.GetCurrentRuntimeContent()
	add_options()

func add_options():
	for option in option_container.get_children():
		option_container.remove_child(option)
		option.queue_free()
	
	var options = story.GenerateCurrentOptions()
	var paths = options.Paths
	if paths != null:
		for path in paths:
			if path.IsValid:
				var button : Button = create_button(path)
				option_container.add_child(button)

func create_button(path):
	var button : Button = Button.new()
	button.text = path.label
	button.pressed.connect(option_button_pressed.bind(path))
	return button

func option_button_pressed(path):
	story.SelectPath(path)
	repaint()
