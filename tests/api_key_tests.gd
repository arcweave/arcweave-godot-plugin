extends RefCounted
## Tests where ArcweaveAsset finds the API key, and the migration of keys saved in the resource by older versions.

const TEST_DIR = "user://arcweave_tests"
const ENV = ArcweaveUserSettings.API_KEY_ENVIRONMENT_VARIABLE

var _failures := 0
var _settings_path : String
var _env_key : String

func Run() -> int:
	_failures = 0
	DirAccess.make_dir_recursive_absolute(TEST_DIR)
	_settings_path = ArcweaveUserSettings.file_path
	_env_key = OS.get_environment(ENV)
	for test in [
		"test_user_settings_save_and_clear_the_key",
		"test_unreadable_user_settings_are_not_overwritten",
		"test_key_precedence",
		"test_new_resources_do_not_save_a_key",
		"test_legacy_key_is_loaded_and_kept_when_saved",
		"test_move_legacy_key_to_user_settings",
		"test_move_keeps_an_existing_key",
		"test_refresh_uses_the_explicit_key",
	]:
		_reset()
		var failures_before = _failures
		call(test)
		if _failures == failures_before:
			print("  PASS ", test)
	_reset()
	ArcweaveUserSettings.file_path = _settings_path
	_set_env(_env_key)
	return _failures

func _reset():
	ArcweaveUserSettings.file_path = TEST_DIR + "/settings.cfg"
	for file in DirAccess.get_files_at(TEST_DIR):
		DirAccess.remove_absolute(TEST_DIR + "/" + file)
	_set_env("")

func _set_env(value: String):
	if value.is_empty():
		OS.unset_environment(ENV)
	else:
		OS.set_environment(ENV, value)

func _check(condition: bool, message: String):
	if not condition:
		_failures += 1
		printerr("  FAIL ", message)

func _check_eq(expected, actual, what: String):
	_check(expected == actual, what + ": expected \"" + str(expected) + "\", got \"" + str(actual) + "\"")

func _read(path: String) -> String:
	return FileAccess.get_file_as_string(path)

func _write(path: String, text: String):
	var file = FileAccess.open(path, FileAccess.WRITE)
	file.store_string(text)
	file.close()

func _legacy_resource(key: String) -> String:
	var path = TEST_DIR + "/legacy.tres"
	_write(path, '[gd_resource type="Resource" script_class="ArcweaveAsset" load_steps=2 format=3]\n\n'
		+ '[ext_resource type="Script" path="res://addons/arcweave/Editor/ArcweaveAssetScript.gd" id="1"]\n\n'
		+ '[resource]\nscript = ExtResource("1")\napi_key = "' + key + '"\nproject_hash = "hash"\nreceive_method = "WebAPI"\n')
	return path

func _load(path: String) -> ArcweaveAsset:
	return ResourceLoader.load(path, "", ResourceLoader.CACHE_MODE_IGNORE)

func test_user_settings_save_and_clear_the_key():
	_check_eq("", ArcweaveUserSettings.get_api_key(), "no key saved")
	_check_eq(OK, ArcweaveUserSettings.set_api_key("  user-key \n"), "save")
	_check_eq("user-key", ArcweaveUserSettings.get_api_key(), "saved key")
	_check_eq(OK, ArcweaveUserSettings.set_api_key(""), "clear")
	_check_eq("", ArcweaveUserSettings.get_api_key(), "cleared key")

func test_unreadable_user_settings_are_not_overwritten():
	var broken = '[api]\napi_key = "unterminated\n'
	_write(ArcweaveUserSettings.file_path, broken)
	_check(ConfigFile.new().load(ArcweaveUserSettings.file_path) != OK, "the test file can't be read")
	_check(ArcweaveUserSettings.set_api_key("user-key") != OK, "saving fails")
	_check_eq(broken, _read(ArcweaveUserSettings.file_path), "settings file")

func test_key_precedence():
	var asset = _load(_legacy_resource("legacy-key"))
	_check_eq("legacy-key", asset.resolve_api_key(), "legacy key is the last fallback")
	_set_env("env-key")
	_check_eq("env-key", asset.resolve_api_key(), "environment variable before the legacy key")
	ArcweaveUserSettings.set_api_key("user-key")
	_check_eq("user-key", asset.resolve_api_key(), "user settings before the environment variable")
	_check_eq("explicit-key", asset.resolve_api_key(" explicit-key "), "explicit key first")

func test_new_resources_do_not_save_a_key():
	ArcweaveUserSettings.set_api_key("user-key")
	var asset = ArcweaveAsset.new()
	asset.project_hash = "hash"
	var path = TEST_DIR + "/new.tres"
	_check_eq(OK, ResourceSaver.save(asset, path), "save")
	_check(not "api_key" in _read(path), "the file has no api_key")
	_check(not "user-key" in _read(path), "the file doesn't contain the key")
	_check(not _load(path).has_legacy_api_key(), "loaded resource has no key")

func test_legacy_key_is_loaded_and_kept_when_saved():
	var path = _legacy_resource("legacy-key")
	var asset = _load(path)
	_check(asset.has_legacy_api_key(), "the legacy key is loaded")
	_check_eq("legacy-key", asset.get("api_key"), "api_key property")
	# Saving for another reason, eg. initializing the asset, must not drop the key before it is moved
	_check_eq(OK, ResourceSaver.save(asset, path), "save")
	_check('api_key = "legacy-key"' in _read(path), "the saved file keeps the key")
	# An empty legacy key, like in resources saved by older versions without a key, is dropped
	var empty = _load(_legacy_resource(""))
	_check(not empty.has_legacy_api_key(), "an empty key is not a legacy key")
	ResourceSaver.save(empty, empty.resource_path)
	_check(not "api_key" in _read(empty.resource_path), "an empty key is not saved")

func test_move_legacy_key_to_user_settings():
	var path = _legacy_resource("legacy-key")
	var asset = _load(path)
	_check_eq(ArcweaveUserSettings.MoveResult.MOVED, ArcweaveUserSettings.move_api_key_out_of_asset(asset), "result")
	_check_eq("legacy-key", ArcweaveUserSettings.get_api_key(), "key in the user settings")
	_check(not asset.has_legacy_api_key(), "the asset has no key")
	_check(not "legacy-key" in _read(path), "the file no longer contains the key")
	_check_eq("hash", _load(path).project_hash, "the rest of the resource is kept")
	_check_eq(ArcweaveUserSettings.MoveResult.NO_KEY, ArcweaveUserSettings.move_api_key_out_of_asset(asset), "second move")

func test_move_keeps_an_existing_key():
	ArcweaveUserSettings.set_api_key("user-key")
	var asset = _load(_legacy_resource("legacy-key"))
	_check_eq(ArcweaveUserSettings.MoveResult.KEPT_EXISTING, ArcweaveUserSettings.move_api_key_out_of_asset(asset), "result")
	_check_eq("user-key", ArcweaveUserSettings.get_api_key(), "the user settings keep their key")
	_check(not "legacy-key" in _read(asset.resource_path), "the file no longer contains the key")

	# A key in the environment variable is kept too, and the user settings stay empty
	ArcweaveUserSettings.set_api_key("")
	_set_env("env-key")
	asset = _load(_legacy_resource("legacy-key"))
	_check_eq(ArcweaveUserSettings.MoveResult.KEPT_EXISTING, ArcweaveUserSettings.move_api_key_out_of_asset(asset), "result with the environment variable")
	_check_eq("", ArcweaveUserSettings.get_api_key(), "the user settings stay empty")

func test_refresh_uses_the_explicit_key():
	ArcweaveUserSettings.set_api_key("user-key")
	var asset = _load(_legacy_resource(""))
	var api_request = APIRequest.new(asset)
	api_request._explicit_api_key = "explicit-key"
	_check("Authorization: Bearer explicit-key" in api_request.get_headers(), "explicit key in the headers")
	api_request._explicit_api_key = ""
	_check("Authorization: Bearer user-key" in api_request.get_headers(), "user key in the headers")
	api_request.free()
