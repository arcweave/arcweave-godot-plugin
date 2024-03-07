extends Control

@onready var text_window: RichTextLabel = $StoryContainer/TextWindow
@onready var options_container: VBoxContainer = $StoryContainer/OptionsContainer
@onready var menu_button: MenuButton = $MenuButton
@onready var refresh_button: Button = $StoryContainer/UIButtonsContainer/RefreshButton
@onready var restart_button: Button = $StoryContainer/UIButtonsContainer/RestartButton

var arcweave_asset: ArcweaveAsset = preload("res://resources/ArcweaveAsset.tres")
var api_request: APIRequest
var Story = load("res://addons/arcweave/Story.cs")
var story


func _ready():
	print("Using Godot Script ./GodotSceneScriptWithoutArcweaveNode.gd")
	api_request = APIRequest.new(arcweave_asset)
	arcweave_asset.project_updated.connect(_on_project_updated)
	add_child(api_request)
	story = Story.new(arcweave_asset.project_settings)
	
	menu_button.pressed.connect(_on_menu_pressed)
	var popup = menu_button.get_popup()
	popup.add_item("Save Game", 0)
	popup.add_item("Load Game", 1)
	popup.id_pressed.connect(_on_menu_item_pressed)
	
	refresh_button.pressed.connect(_on_refresh_pressed)
	restart_button.pressed.connect(_on_restart_pressed)
	repaint()


func repaint():
	text_window.text = story.GetCurrentRuntimeContent()
	add_options()


func add_options():
	for option in options_container.get_children():
		options_container.remove_child(option)
		option.queue_free()
	
	var options = story.GenerateCurrentOptions()
	var paths = options.Paths
	if paths != null:
		for path in paths:
			if path.IsValid:
				var button : Button = create_button(path)
				options_container.add_child(button)


func create_button(path):
	var button : Button = Button.new()
	button.text = path.label
	button.pressed.connect(_on_option_button_pressed.bind(path))
	return button


func _on_option_button_pressed(path):
	story.SelectPath(path)
	repaint()


func _on_project_updated(project_settings):
	story.UpdateStory(project_settings)
	repaint()


func _on_refresh_pressed():
	arcweave_asset.refresh_project(api_request)


func _on_restart_pressed():
	get_tree().reload_current_scene()

func _on_menu_pressed():
	var popup = menu_button.get_popup()
	popup.set_item_disabled(1, !FileAccess.file_exists("user://savegame.save"))
	popup.position = Vector2i(25, 460)

func _on_menu_item_pressed(id):
	if id == 0:
		save_game()
	elif id == 1:
		load_game()

func save_game():
	var save_game_file = FileAccess.open("user://savegame.save", FileAccess.WRITE)
	var save = story.GetSave()
	save_game_file.store_var(save)

func load_game():
	var save_game_file = FileAccess.open("user://savegame.save", FileAccess.READ)
	var save = save_game_file.get_var()
	story.LoadSave(save)
	repaint()
