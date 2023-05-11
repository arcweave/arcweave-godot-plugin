extends Object
class_name StateExport

var aw_visits: Dictionary
var aw_current_element: Element
var variables: Dictionary

var defaults = {
	"score": 0
}

func _init():
	self.aw_visits = {}
	for variable_name in self.defaults:
		self.variables[variable_name] = Variable.new(variable_name, self.defaults[variable_name])

func reset_all_vars(except_vars: Array):
	for variable_name in self.variables:
		if not(variable_name in except_vars):
			self.variables[variable_name].reset_to_default()

func reset_vars(vars: Array):
	for variable_name in vars:
		self.variables[variable_name].reset_to_default()

func get_var(name):
	return self.variables[name].value

func set_var(name, value):
	self.variables[name].value = value

func get_current_state() -> Dictionary:
	var result = {
		"variables": {},
		"visits": self.aw_visits.duplicate(),
	}
	for variable_name in self.variables:
		var variable = self.variables[variable_name]
		result.variables[variable.name] = variable.value
	return result

func set_state(state: Dictionary):
	for variable_name in state.variables:
		self.variables[variable_name].value = state.variables[variable_name]
	self.aw_visits = state.visits.duplicate()

func get_current_element() -> Element:
	return self.aw_current_element

func set_current_element(element: Element):
	self.aw_current_element = element

func increment_visits(element_id: String):
	if self.aw_visits.has(element_id):
		self.aw_visits[element_id] += 1
	else:
		self.aw_visits[element_id] = 1
	return self.aw_visits[element_id]

func decrement_visits(element_id: String):
	if not self.aw_visits.has(element_id):
		self.aw_visits[element_id] = 0
	elif self.aw_visits[element_id] > 0:
		self.aw_visits[element_id] -= 1
	return self.aw_visits[element_id]

func get_visits(element_id: String):
	if not (element_id in self.aw_visits):
		return 0
	return self.aw_visits[element_id]

func set_visits(element_id: String, value: int):
	self.aw_visits[element_id] = value

func init_visits(element_ids: Array):
	for element_id in element_ids:
		self.aw_visits[element_id] = 0
