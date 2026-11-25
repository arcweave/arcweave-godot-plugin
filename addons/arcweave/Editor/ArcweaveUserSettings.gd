@tool
class_name ArcweaveUserSettings extends RefCounted
## Saves the API key of the current user of this Godot project in [member ArcweaveAsset.user_settings_path]
## ([code]user://arcweave_settings.cfg[/code]), and moves the keys older versions of the plugin saved in ArcweaveAssets
## there.
##
## [code]user://[/code] is the project's user data folder, outside the project folder, so the settings are never
## committed to version control or included in exports. Each developer keeps their own API key there instead of
## sharing one saved in an ArcweaveAsset. Games run from the editor read the same folder, so they can refresh from the
## WebAPI with the key too.
## The game only needs ArcweaveAsset to read the key, so exports that only include selected resources keep working.

## The environment variable the API key is read from when none is saved in the user settings, eg. on a build server.
const API_KEY_ENVIRONMENT_VARIABLE = ArcweaveAsset.API_KEY_ENVIRONMENT_VARIABLE

## The result of [method move_api_key_out_of_asset]
enum MoveResult {
	## The asset has no API key
	NO_KEY,
	## The key was moved to the user settings
	MOVED,
	## The key was removed from the asset. The same key is already in the user settings or the environment.
	KEPT_EXISTING,
	## Nothing changed: a different key is already in the user settings or the environment. Pass
	## discard_different_key to remove the asset's key and keep the existing one.
	DIFFERENT_KEY_EXISTS,
	## The key was moved or removed, but the asset is saved inside a scene, which must be saved to remove it from the file
	SAVE_SCENE,
	## The key could not be moved, and the asset keeps it
	FAILED,
}

const _SECTION = ArcweaveAsset._USER_SETTINGS_SECTION
const _API_KEY = ArcweaveAsset._USER_SETTINGS_API_KEY

## Returns the API key saved for the current user, or an empty String if none is saved.
static func get_api_key() -> String:
	return ArcweaveAsset.get_user_api_key()

## Returns the settings file, [member ArcweaveAsset.user_settings_path]
static func get_file_path() -> String:
	return ArcweaveAsset.user_settings_path

## Saves the API key for the current user. An empty key removes the saved one.
## Returns the error if the file can't be read or written, in which case the saved key doesn't change.
static func set_api_key(key: String) -> Error:
	var config := ConfigFile.new()
	var error := config.load(get_file_path())
	# A file that exists but can't be read is not overwritten, so its other settings aren't lost
	if error != OK and error != ERR_FILE_NOT_FOUND:
		printerr("[Arcweave] Could not read the Arcweave user settings in \"" + get_file_path() + "\" (error " + str(error) + ")")
		return error
	key = key.strip_edges()
	if key.is_empty():
		if not config.has_section_key(_SECTION, _API_KEY):
			return OK
		config.erase_section_key(_SECTION, _API_KEY)
	else:
		config.set_value(_SECTION, _API_KEY, key)
	error = config.save(get_file_path())
	if error != OK:
		printerr("[Arcweave] Could not save the Arcweave user settings in \"" + get_file_path() + "\" (error " + str(error) + ")")
	return error

## Returns the API key in the ARCWEAVE_API_KEY environment variable, or an empty String if it is not set.
static func get_environment_api_key() -> String:
	return ArcweaveAsset.get_environment_api_key()

## Returns the API key saved in the user settings, otherwise the one in the ARCWEAVE_API_KEY environment variable.
## Empty if neither has one.
static func resolve_api_key() -> String:
	var key := get_api_key()
	if not key.is_empty():
		return key
	return get_environment_api_key()

## Removes the API key that older versions of the plugin saved in an ArcweaveAsset, and saves the resource file that
## contains the asset.
## The key is moved to the user settings, unless a key is already found in the user settings or the ARCWEAVE_API_KEY
## environment variable. If that key is the same, it is kept and the asset's key is removed. If it is different, nothing
## changes unless [param discard_different_key] is true. The key is saved in the user settings and read back before it is
## removed from the asset, so a failure never loses it.
static func move_api_key_out_of_asset(asset: ArcweaveAsset, discard_different_key := false) -> MoveResult:
	if asset == null or not asset.has_legacy_api_key():
		return MoveResult.NO_KEY
	var legacy_key := asset.get_legacy_api_key()
	var existing_key := resolve_api_key()
	var moved := existing_key.is_empty()
	if not moved and existing_key != legacy_key and not discard_different_key:
		return MoveResult.DIFFERENT_KEY_EXISTS
	if moved and (set_api_key(legacy_key) != OK or get_api_key() != legacy_key):
		printerr("[Arcweave] Could not save the API key in \"" + get_file_path() + "\". The resource keeps its key.")
		return MoveResult.FAILED

	asset.clear_legacy_api_key()
	# An asset saved inside another file (eg. "res://scene.tscn::Resource_abc") is saved with that file
	var file := asset.resource_path.get_slice("::", 0)
	if file.is_empty() or file.get_extension() in ["tscn", "scn"]:
		return MoveResult.SAVE_SCENE
	# The loaded file that contains the asset, not a new copy of it that still has the key
	var owner = asset if file == asset.resource_path else (ResourceLoader.load(file) if ResourceLoader.has_cached(file) else null)
	var error := ResourceSaver.save(owner, file) if owner is Resource else ERR_CANT_OPEN
	if error != OK:
		printerr("[Arcweave] Could not save \"" + file + "\" without its API key (error " + str(error) + ")")
		asset.set_legacy_api_key(legacy_key)
		return MoveResult.FAILED
	return MoveResult.MOVED if moved else MoveResult.KEPT_EXISTING

## Returns the ArcweaveAssets with an API key saved in the resource or scene file at [param path], including the ones
## saved inside it. ArcweaveAssets saved in their own files are checked when those files are exported.
static func find_assets_with_api_key(path: String) -> Array[ArcweaveAsset]:
	var assets : Array[ArcweaveAsset] = []
	var extension = path.get_extension()
	if not extension in ["tres", "res", "tscn", "scn"]:
		return assets
	# A text file only contains an ArcweaveAsset if it references its script, so other files aren't loaded
	var asset_script = "res://addons/arcweave/Editor/ArcweaveAssetScript.gd"
	if extension in ["tres", "tscn"] and not FileAccess.get_file_as_string(path).contains(asset_script):
		return assets
	var resource = ResourceLoader.load(path)
	var visited = {}
	if resource is PackedScene:
		var state : SceneState = resource.get_state()
		for node in state.get_node_count():
			for property in state.get_node_property_count(node):
				_collect(state.get_node_property_value(node, property), path, assets, visited)
	else:
		_collect(resource, path, assets, visited)
	return assets

static func _collect(value, path: String, assets: Array[ArcweaveAsset], visited: Dictionary):
	if value is Resource:
		# Resources saved in other files are checked when those files are exported
		var file = value.resource_path.get_slice("::", 0)
		if visited.has(value) or (not file.is_empty() and file != path):
			return
		visited[value] = true
		if value is ArcweaveAsset:
			if value.has_legacy_api_key():
				assets.append(value)
			return
		for property in value.get_property_list():
			if property.usage & PROPERTY_USAGE_STORAGE and property.type in [TYPE_OBJECT, TYPE_ARRAY, TYPE_DICTIONARY]:
				_collect(value.get(property.name), path, assets, visited)
	elif value is Array:
		for item in value:
			_collect(item, path, assets, visited)
	elif value is Dictionary:
		for key in value:
			_collect(key, path, assets, visited)
			_collect(value[key], path, assets, visited)
