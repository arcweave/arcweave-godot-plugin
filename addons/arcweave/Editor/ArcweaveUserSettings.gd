@tool
class_name ArcweaveUserSettings extends RefCounted
## Arcweave settings saved for the current user of this Godot project, in [code]user://arcweave_settings.cfg[/code].
##
## [code]user://[/code] is the project's user data folder, outside the project folder, so the settings are never
## committed to version control or included in exports. Each developer keeps their own API key there instead of
## sharing one saved in an ArcweaveAsset. Games run from the editor read the same folder, so they can refresh from the
## WebAPI with the key too.

## The environment variable the API key is read from when none is saved in the user settings, eg. on a build server.
const API_KEY_ENVIRONMENT_VARIABLE = "ARCWEAVE_API_KEY"

## The result of [method move_api_key_out_of_asset]
enum MoveResult {
	## The asset has no API key
	NO_KEY,
	## The key was moved to the user settings
	MOVED,
	## The key was removed from the asset, keeping the key already found in the user settings or the environment
	KEPT_EXISTING,
	## The key could not be moved, and the asset keeps it
	FAILED,
}

const _SECTION = "api"
const _API_KEY = "api_key"

## The settings file. Tests point it at another file.
static var file_path := "user://arcweave_settings.cfg"

## Returns the API key saved for the current user, or an empty String if none is saved.
static func get_api_key() -> String:
	var config := ConfigFile.new()
	if config.load(file_path) != OK:
		return ""
	return str(config.get_value(_SECTION, _API_KEY, "")).strip_edges()

## Saves the API key for the current user. An empty key removes the saved one.
## Returns the error if the file can't be read or written, in which case the saved key doesn't change.
static func set_api_key(key: String) -> Error:
	var config := ConfigFile.new()
	var error := config.load(file_path)
	# A file that exists but can't be read is not overwritten, so its other settings aren't lost
	if error != OK and error != ERR_FILE_NOT_FOUND:
		printerr("[Arcweave] Could not read the Arcweave user settings in \"" + file_path + "\" (error " + str(error) + ")")
		return error
	key = key.strip_edges()
	if key.is_empty():
		if not config.has_section_key(_SECTION, _API_KEY):
			return OK
		config.erase_section_key(_SECTION, _API_KEY)
	else:
		config.set_value(_SECTION, _API_KEY, key)
	error = config.save(file_path)
	if error != OK:
		printerr("[Arcweave] Could not save the Arcweave user settings in \"" + file_path + "\" (error " + str(error) + ")")
	return error

## Returns the API key in the ARCWEAVE_API_KEY environment variable, or an empty String if it is not set.
static func get_environment_api_key() -> String:
	return OS.get_environment(API_KEY_ENVIRONMENT_VARIABLE).strip_edges()

## Returns the API key saved in the user settings, otherwise the one in the ARCWEAVE_API_KEY environment variable.
## Empty if neither has one.
static func resolve_api_key() -> String:
	var key := get_api_key()
	if not key.is_empty():
		return key
	return get_environment_api_key()

## Removes the API key that older versions of the plugin saved in an ArcweaveAsset, and saves the asset.
## The key is moved to the user settings, unless a key is already found in the user settings or the ARCWEAVE_API_KEY
## environment variable, which is kept. The key is saved in the user settings before it is removed from the asset, so a
## failure never loses it.
static func move_api_key_out_of_asset(asset: ArcweaveAsset) -> MoveResult:
	if asset == null or not asset.has_legacy_api_key():
		return MoveResult.NO_KEY
	var legacy_key := asset.get_legacy_api_key()
	var moved := resolve_api_key().is_empty()
	if moved and set_api_key(legacy_key) != OK:
		return MoveResult.FAILED
	asset.clear_legacy_api_key()
	var path := asset.resource_path
	# A resource saved inside a scene is saved with the scene
	if not path.is_empty() and not path.contains("::"):
		var error := ResourceSaver.save(asset, path)
		if error != OK:
			printerr("[Arcweave] Could not save \"" + path + "\" without its API key (error " + str(error) + ")")
			asset.set_legacy_api_key(legacy_key)
			return MoveResult.FAILED
	return MoveResult.MOVED if moved else MoveResult.KEPT_EXISTING
