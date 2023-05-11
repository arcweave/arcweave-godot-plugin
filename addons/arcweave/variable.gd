extends Object
class_name Variable

var name: String
var value
var _type
var _default_value

func _init(name: String, value):
	self.name = name
	self.value = value
	self._default_value = value
	self._type = typeof(value)

func reset_to_default():
	if self._type == TYPE_STRING:
		self.value = self._default_value as String
	if self._type == TYPE_INT:
		self.value = self._default_value as int
	if self._type == TYPE_REAL:
		self.value = self._default_value as float
	if self._type == TYPE_BOOL:
		self.value = self._default_value as bool
