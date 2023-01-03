extends Object
class_name Component

var id: String = ""
var name: String = ""
var cover: Dictionary = {}
"""
{
	"attr1": {
		"id": 'abcde-fghij-klmno'
		"name": "type",
		"value": {
			"data": "item",
			"type": "string"
		}
	},
	"attr2": {
		...
	}
}
"""
var attributes: Dictionary = {}

func _init(id, name, attr = {}, cover = {}):
	self.id = id
	self.name = name
	self.attributes = attr
	self.cover = cover

func get_name() -> String:
	return name

func get_attribute_by_name(name: String) -> Dictionary:
	for attrId in self.attributes:
		if self.attributes[attrId].name == name:
			return self.attributes[attrId]
	return {}

func search_attributes_by_name(name: String) -> Array:
	var response: Array = []
	for attrId in self.attributes:
		if self.attributes[attrId].name == name:
			response.append(self.attributes[attrId])
	return response

func get_cover():
	return self.cover

func _to_string():
	return JSON.print({
		"name": self.name,
		"attributes": self.attributes,
	})
