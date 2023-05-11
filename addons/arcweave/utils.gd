extends Object
class_name Utils

var rng = RandomNumberGenerator.new()

# Returns the absolute of a number
func aw_abs(n):
	return abs(n)

# Returns the square of a number
func aw_sqr(n):
	return n*n

# Returns the square root of a number
func aw_sqrt(n):
	return sqrt(n)

# Returns a random decimal between (and including) 0 and (excluding) 1, i.e. in [0, 1)
func aw_random():
	return randf()

# Returns a roll of an (n) number of (m)-sided dice (see below)
func aw_roll(maxRoll: int, rolls: int = 1):
	rng.randomize()
	var roll_sum := 0
	for i in range(0, rolls):
		roll_sum += rng.randi_range(1, maxRoll)
	return roll_sum
	
# Retuns a rounded number to the nearest integer
func aw_round(n):
	return int(round(n))
	
# Returns the evaluation and concatenation of its argument expressions (see below)
func aw_show():
	pass
	
# Resets the given variables to their initial value
func aw_reset(state, vars: Array):
	state.reset_vars(vars)

# Resets all variables, except the given ones, to their initial value
func aw_resetAll(state, except_vars: Array):
	state.reset_all_vars(except_vars)

# The following are for Godot v.4
#static func aw_min():
#	pass
#
#static func aw_max():
#	pass

func aw_visits(state: StateExport, element_id = null):
	if element_id == null:
		element_id = state.get_current_element().id
	return state.get_visits(element_id)
