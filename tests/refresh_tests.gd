extends RefCounted
## Tests refreshing an ArcweaveAsset from a project file, and which project data a refresh accepts.

const TEST_DIR = "user://arcweave_tests"

var _failures := 0
var _current_test := ""

func Run() -> int:
	_failures = 0
	DirAccess.make_dir_recursive_absolute(TEST_DIR)
	for test in [
		"test_valid_projects",
		"test_invalid_projects",
		"test_refresh_from_a_file_that_omits_collections",
		"test_failed_refresh_from_a_file_keeps_the_project",
	]:
		_current_test = test
		var failures_before = _failures
		# Each test returns true at its end, so a test that stops on a script error fails
		if call(test) != true:
			_check(false, "stopped before its end")
		elif _failures == failures_before:
			print("  PASS ", test)
	return _failures

func _check(condition: bool, message: String):
	if not condition:
		_failures += 1
		printerr("  FAIL ", _current_test, ": ", message)

func _write(path: String, text: String):
	var file = FileAccess.open(path, FileAccess.WRITE)
	file.store_string(text)
	file.close()

func _fixture() -> Dictionary:
	return JSON.parse_string(FileAccess.get_file_as_string("res://tests/fixtures/project.json"))

func test_valid_projects():
	var project = _fixture()
	_check(ArcweaveAsset.is_valid_project(project), "complete project")
	# Arcweave exports omit empty collections or write them as null, and the starting element may be missing
	for key in ["boards", "branches", "jumpers", "conditions", "connections", "components", "attributes", "variables",
			"notes", "assets", "startingElement"]:
		var omitted = project.duplicate(true)
		omitted.erase(key)
		_check(ArcweaveAsset.is_valid_project(omitted), "project without " + key)
		var null_value = project.duplicate(true)
		null_value[key] = null
		_check(ArcweaveAsset.is_valid_project(null_value), "project with a null " + key)
	return true

func test_invalid_projects():
	var project = _fixture()
	for data in [null, [], "project", {}, {"project": project}]:
		_check(not ArcweaveAsset.is_valid_project(data), "rejects " + str(data).left(40))
	for key in ["name", "elements"]:
		var missing = project.duplicate(true)
		missing.erase(key)
		_check(not ArcweaveAsset.is_valid_project(missing), "rejects a project without " + key)
	var wrong_type = project.duplicate(true)
	wrong_type.boards = []
	_check(not ArcweaveAsset.is_valid_project(wrong_type), "rejects boards that aren't a Dictionary")
	return true

func test_refresh_from_a_file_that_omits_collections():
	var project = _fixture()
	for key in ["branches", "jumpers", "conditions", "connections", "components", "attributes", "variables", "notes"]:
		project.erase(key)
	var path = TEST_DIR + "/omitted.json"
	_write(path, JSON.stringify(project))
	var asset = ArcweaveAsset.new()
	asset.project_file = ProjectSettings.globalize_path(path)
	var updates = []
	asset.project_updated.connect(func(settings): updates.append(settings))
	asset.refresh_project(null)
	_check(updates.size() == 1 and updates[0] is Dictionary, "refreshed: " + str(updates).left(60))
	_check(asset.project_settings.get("name") == "Fixture", "project settings replaced")
	var story = load("res://addons/arcweave/Story.cs").new(asset.project_settings)
	_check(story.GetCurrentElement().Id == "start", "the story starts")
	story.free()
	return true

func test_failed_refresh_from_a_file_keeps_the_project():
	var path = TEST_DIR + "/wrapped.json"
	_write(path, JSON.stringify({"project": _fixture()}))
	var asset = ArcweaveAsset.new(_fixture())
	asset.project_file = ProjectSettings.globalize_path(path)
	var updates = []
	asset.project_updated.connect(func(settings): updates.append(settings))
	asset.refresh_project(null)
	_check(updates == [null], "reported the failure: " + str(updates).left(60))
	_check(asset.project_settings.get("name") == "Fixture", "project settings kept")
	return true
