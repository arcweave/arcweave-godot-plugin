extends Control

@onready var text_window: RichTextLabel = $StoryContainer/TextWindow
@onready var options_container: VBoxContainer = $StoryContainer/OptionsContainer
@onready var refresh_button: Button = $StoryContainer/UIButtonsContainer/RefreshButton
@onready var restart_button: Button = $StoryContainer/UIButtonsContainer/RestartButton
@onready var menu_button: MenuButton = $MenuButton
@onready var arcweave_node: Node = $ArcweaveNode


# Called when the node enters the scene tree for the first time.
func _ready():
	print("Using Godot Script ./GodotSceneScript.gd")
	arcweave_node.connect("ProjectUpdated", _on_project_updated)
	
	menu_button.pressed.connect(_on_menu_pressed)
	var popup = menu_button.get_popup()
	popup.add_item("Save Game", 0)
	popup.add_item("Load Game", 1)
	popup.id_pressed.connect(_on_menu_item_pressed)
	
	refresh_button.pressed.connect(_on_refresh_pressed)
	restart_button.pressed.connect(_on_restart_pressed)
	repaint()

func repaint():
	text_window.text = arcweave_node.Story.GetCurrentRuntimeContent()
	add_options()


func add_options():
	for option in options_container.get_children():
		options_container.remove_child(option)
		option.queue_free()
	
	var options = arcweave_node.Story.GenerateCurrentOptions()
	var paths = options.Paths
	if paths != null:
		for path in paths:
			if path.IsValid:
				var button : Button = create_button(path)
				options_container.add_child(button)


func create_button(path):
	var button : Button = Button.new()
	button.custom_minimum_size.y = 32 # Cheap way to handle empty "next" button.
	button.text = path.label
	button.pressed.connect(_on_option_button_pressed.bind(path))
	return button


func _on_option_button_pressed(path):
	arcweave_node.Story.SelectPath(path)
	repaint()


func _on_project_updated():
	repaint()


func _on_refresh_pressed():
	arcweave_node.UpdateStory()


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
	var save = arcweave_node.Story.GetSave()
	save_game_file.store_var(save)

func load_game():
	var save_game_file = FileAccess.open("user://savegame.save", FileAccess.READ)
	var save = save_game_file.get_var()
	arcweave_node.Story.LoadSave(save)
	repaint()
