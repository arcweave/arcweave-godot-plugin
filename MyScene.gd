extends Node2D

var textContainer: RichTextLabel
var optionContainer: VBoxContainer
var saveButton: Button
var save_file_location := "user://dataSave.json"

var story: Story
func _ready():
	self.story = Story.new()

	self.loadState()

	self.textContainer = self.get_node("TextContainer")
	self.optionContainer = self.get_node("OptionContainer")
	self.saveButton = self.get_node("SaveButton")
	self.saveButton.connect("pressed", Callable(self, "saveState"))
	
	textContainer.bbcode_enabled = true
	textContainer.text = story.get_current_content()
	self.addOptions(story.get_current_options())
	
func addOptions(options):
	for n in self.optionContainer.get_children():
		self.optionContainer.remove_child(n)
		n.queue_free()

	for option in options:
		var lastLabel = null
		for connection in option.connectionPath:
			if connection.label:
				lastLabel = connection.label
		if lastLabel == null:
			lastLabel = self.story.elements[option.targetid].title
		if lastLabel == null or lastLabel == "":
			lastLabel = self.story.elements[option.targetid].get_content(self.story.state)
		self.createButton(lastLabel, option)

func createButton(text, option):
	var button = Button.new()
	button.text = text
	button.connect("pressed", Callable(self, "_on_option_selection").bind(option))
	self.optionContainer.add_child(button)

func _on_option_selection(option):
	self.story.select_option(option)
	self.redraw()

func redraw():
	self.textContainer.text = story.get_current_content()
	self.addOptions(story.get_current_options())

func saveState():
	var currentState = self.story.get_state()
	var currentElementId = self.story.get_current_element().id
	
	var saveObject = {
		'state': currentState,
		'element': currentElementId,
	}
	
	var file = FileAccess.open(save_file_location, FileAccess.WRITE)
	file.store_string(JSON.stringify(saveObject, '\t'))
	file.close()

func loadState():
	if FileAccess.file_exists(save_file_location):
		var file = FileAccess.open(save_file_location, FileAccess.READ)
		var data = JSON.parse_string(file.get_as_text())
		file.close()
		self.story.set_state(data['state'])
		self.story.set_current_element(data['element'])
