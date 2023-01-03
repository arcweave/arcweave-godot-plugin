extends Object
class_name StateExport

var paintingExamined: bool

var defaults = {
	"paintingExamined": false
}

func _init():
	for variable in self.defaults:
		self[variable] = self.defaults[variable]

func reset_all_vars(except_vars: Array):
	for variable in self.defaults:
		if not(variable in except_vars):
			self[variable] = self.defaults[variable]

func reset_vars(vars: Array):
	for variable in vars:
		self[variable] = self.defaults[variable]

func get_var(name):
	return self[name]

func set_var(name, value):
	self[name] = value

func get_current_state() -> Dictionary:
	var result = {}
	for variable in self.defaults:
		result[variable] = self[variable]
	return result

func set_state(state: Dictionary):
	for variable in state:
		self[variable] = state[variable]
