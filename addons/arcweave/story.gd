extends Object
class_name Story

var Component = load("res://addons/arcweave/component.gd")
var Element = load("res://addons/arcweave/element.gd")
var Board = load("res://addons/arcweave/board.gd")
var StateExport = load("res://addons/arcweave/state_export.gd")
var Utils = load("res://addons/arcweave/utils.gd")

var name: String = ""
var boards: Dictionary = {}
var elements: Dictionary = {}
var components: Dictionary = {}
var jumpers: Dictionary = {}
var connections: Dictionary = {}
var branches: Dictionary = {}
var conditions: Dictionary = {}

var utils: Utils
var state: StateExport

var starting_element: Element

var current_options: Array = []
var history: Array = []


func _init():
	self.utils = Utils.new()
	var DataExport = load("res://addons/arcweave/data_export.gd")
	var data = DataExport.new(self.utils)
	
	# Create State
	self.state = StateExport.new()
	
	data = data.get_data()
	self.name = data.name
	self.current_options = []
	
	# Create Components
	for component_id in data.components:
		var component = data.components[component_id]
		if "children" in component:
			continue
		var cover = {}
		if 'cover' in component:
			cover = component.cover
		var comp_attrs = {}
		for attr in component.attributes:
			comp_attrs[attr] = data.attributes[attr]
			comp_attrs[attr].id = attr
		self.components[component_id] = Component.new(component_id, component.name, comp_attrs.duplicate(true), cover)
	
	# Create Connections
	for connection_id in data.connections:
		var connection = data.connections[connection_id]
		self.connections[connection_id] = connection.duplicate()
	
	# Create Conditions
	self.conditions = data.conditions.duplicate(true)
	
	# Create Elements
	for element_id in data.elements:
		var element = data.elements[element_id]
		var component_array = []
		for component in element.components:
			component_array.append(self.components[component])
		var connection_array = []
		for connection in element.outputs:
			connection_array.append(self.connections[connection])
		var cover = {}
		if 'cover' in element:
			cover = element.cover
		self.elements[element_id] = Element.new(element_id, element.title,
				element.content, element.theme, component_array.duplicate(),
				connection_array.duplicate(), {}, cover)
	self.starting_element = self.elements[data.startingElement]
	self.state.set_current_element(self.elements[data.startingElement])
	# Create Jumpers
	for jumper_id in data.jumpers:
		var jumper = data.jumpers[jumper_id]
		var target_element = null
		if jumper.elementId:
			target_element = self.elements[jumper.elementId]
		jumpers[jumper_id] = { "element": target_element }
	
	# Create Branches
	for branch_id in data.branches:
		var branch = data.branches[branch_id]
		var branch_conditions = {
			"if_condition": self.conditions[branch.conditions.ifCondition]
		}
		if "elseCondition" in branch.conditions:
			branch_conditions.else_condition = self.conditions[branch.conditions.elseCondition]
		if "elseIfConditions" in branch.conditions:
			branch_conditions.else_if_conditions = []
			for el_if_cond in branch.conditions.elseIfConditions:
				branch_conditions.else_if_conditions.append(self.conditions[el_if_cond])
		self.branches[branch_id] = {
			"theme": branch.theme,
			"conditions": branch_conditions,
		}
	
	# Finally Create Boards
	for board_id in data.boards:
		if "children" in data.boards[board_id]:
			continue
		var board = data.boards[board_id]
		
		var custom_id = ""
		if "customId" in board:
			custom_id = board.customId
		
		var board_nodes = {
			"elements": {},
			"connections": {},
#			"notes": {},
			"jumpers": {},
			"branches": {},
		}

		for node_type in board_nodes:
			for node_id in board[node_type]:
				board_nodes[node_type][node_id] = self[node_type][node_id]
		self.boards[board_id] = Board.new(board_id, board.name, custom_id, board_nodes)
	
	self.state.increment_visits(data.startingElement)
	self.generate_current_options()

func start():
	pass

func get_current_element() -> Element:
	return self.state.get_current_element()

func set_current_element(id: String) -> Element:
	self.state.set_current_element(self.elements[id])
	self.generate_current_options()
	return self.state.get_current_element()

func get_current_content():
	return self.state.get_current_element().get_content(self.state)

func print_elements_content():
	for element_id in self.elements:
		var element: Element = self.elements[element_id]
		print(element.get_content(self.state))

func get_current_options():
	return current_options

func get_element(element_id: String) -> Element:
	var e: Element = self.elements[element_id]
	return e

func get_state() -> Dictionary:
	return self.state.get_current_state()

func set_state(state):
	self.state.set_state(state)

func generate_current_options():
	self.current_options = []
	for output in self.state.get_current_element().outputs:
		if output.targetType == 'elements':
			self.current_options.append({"targetid": output.targetid, "connectionPath": [output]})
		if output.targetType == 'jumpers':
			self.current_options.append({
				"targetid": self.jumpers[output.targetid].element.id,
				"connectionPath": [output],
			})
		elif output.targetType == 'branches':
			var connection = self._get_truthy_condition(output.targetid)
			var branch_connection_path = [output, connection]
			while (connection and connection.targetType == 'branches'):
				connection = self._get_truthy_condition(connection.targetid)
				branch_connection_path.append(connection)
			if connection:
				if connection.targetType == 'elements':
					self.current_options.append({
						"targetid": connection.targetid,
						"connectionPath": branch_connection_path,
					})
				elif connection.targetType == 'jumpers':
					self.current_options.append({
						"targetid": self.jumpers[connection.targetid].element.id,
						"connectionPath": branch_connection_path,
					})

func _get_truthy_condition(branchId):
	var branch = self.branches[branchId]
	if branch.conditions.if_condition.script:
		var cond = branch.conditions.if_condition
		if evaluate(cond.script):
			return self.connections[cond.output]
	if "else_if_conditions" in branch.conditions:
		for cond in branch.conditions.else_if_conditions:
			if evaluate(cond.script):
				return self.connections[cond.output]
	if "else_condition" in branch.conditions:
		var cond = branch.conditions.else_condition
		return self.connections[cond.output]
	return null
	
func select_option(optionId):
	self.state.increment_visits(optionId)
	self.state.set_current_element(self.elements[optionId])
	self.generate_current_options()

func evaluate(command, variable_names = [], variable_values = []):
	var expression = Expression.new()
	var error = expression.parse(command, variable_names)
	if error != OK:
		push_error(expression.get_error_text())
		return

	var result = expression.execute(variable_values, self)

	if not expression.has_execute_failed():
		return result

func _to_string():
	return JSON.print({
		"name": self.name,
		"boards": self.boards,
		"elements": self.elements,
		"components": self.components,
	}, "\t")
