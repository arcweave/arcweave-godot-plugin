extends Object
class_name Element

var content_ref
var id: String = ""
export var title: String = ""
export var theme: String = "default"
var outputs: Array = []
var components: Array = []
var attributes: Dictionary = {}
var cover: Dictionary = {}

func _init(id, title = "", content_ref = null, theme = "default", components = [], outputs = [], attributes = {}, cover = {}):
	if title == null:
		title = ""
	self.id = id
	self.title = title
	self.content_ref = content_ref
	self.theme = theme
	self.components = components
	self.outputs = outputs
	self.attributes = attributes
	self.cover = cover

func get_content(state) -> String:
	if self.content_ref:
		return self.content_ref.call_func(state)
	return ""

func get_cover() -> Dictionary:
	return self.cover
