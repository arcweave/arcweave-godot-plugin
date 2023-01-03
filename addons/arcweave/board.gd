extends Object
class_name Board

var id: String = ""
var customId: String = ""
var name: String = ""
var elements: Dictionary = {}
var connections: Dictionary = {}
var notes: Dictionary = {}
var jumpers: Dictionary = {}
var branches: Dictionary = {}

func _init(id: String, name: String, customId: String, nodes: Dictionary):
	self.id = id
	self.name = name
	self.customId = customId
	for node in nodes:
		self[node] = nodes[node]

func _to_string():
	return JSON.print({
		"id": self.id,
		"name": self.name,
	}, "\t")
