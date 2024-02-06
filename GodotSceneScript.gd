extends Node2D

var text_container: RichTextLabel
var option_container: VBoxContainer
var save_button: Button
var refresh_button: Button

var arcweave_node

# Called when the node enters the scene tree for the first time.
func _ready():
	print("Using Godot Script ./GodotSceneScript.gd")
	arcweave_node = get_node("ArcweaveNode")
	arcweave_node.connect("ProjectUpdated", _on_project_updated)
	
	text_container = get_node("TextContainer")
	option_container = get_node("OptionContainer")
	save_button = get_node("SaveButton")
	refresh_button = get_node("RefreshButton")
	refresh_button.pressed.connect(_on_refresh_pressed)
	
	text_container.bbcode_enabled = true
	repaint()

func _on_project_updated():
	repaint()

func _on_refresh_pressed():
	arcweave_node.UpdateStory()

func repaint():
	text_container.text = arcweave_node.Story.GetCurrentRuntimeContent()
	add_options()

func add_options():
	for option in option_container.get_children():
		option_container.remove_child(option)
		option.queue_free()
	
	var options = arcweave_node.Story.GenerateCurrentOptions()
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
	arcweave_node.Story.SelectPath(path)
	repaint()
