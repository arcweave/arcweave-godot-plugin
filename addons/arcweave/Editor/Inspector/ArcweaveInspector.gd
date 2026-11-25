@tool
extends EditorInspectorPlugin

var api_request : APIRequest

func _can_handle(object):
	return object is ArcweaveAsset

func _parse_property(object, type, name, hint_type, hint_string, usage_flags, wide):
	if name == "project_hash":
		# Shown before the project hash, in the API Settings group
		add_custom_control(_create_api_key_editor(object))
		return false
	if name == "_placeholder":
		var InitButton = Button.new()
		api_request = APIRequest.new(object)
		InitButton.add_child(api_request)
		InitButton.add_theme_color_override("font_color", Color.DARK_ORANGE)
		InitButton.text = "Initialize Arcweave Asset"
		InitButton.pressed.connect(pressed.bind(object))
		add_custom_control(InitButton)
		return true
	return false

func pressed(object: ArcweaveAsset):
	if object.is_downloading_media:
		push_warning("[Arcweave] Wait for the media download to finish before initializing again.")
		return
	object.project_updated.connect(on_project_updated.bind(object), CONNECT_ONE_SHOT)
	object.refresh_project(api_request)

func on_project_updated(new_project_settings, object: ArcweaveAsset):
	if new_project_settings == null:
		return
	var downloader = null
	if object.download_media and object.receive_method == "WebAPI":
		downloader = await download_media(object, new_project_settings)
	elif not object.download_media:
		# The project no longer uses the downloaded media, so the resource stops loading and exporting it.
		# The files are kept, and deleted by a later download if the project doesn't use them anymore.
		object.media_resources = {}
	var error = _save_resource(object)
	if downloader != null:
		# Unused media is deleted once the resource no longer references it, so a failed save never leaves the resource
		# referencing deleted media
		if error == OK and downloader.delete_unused_media():
			_save_resource(object)
		downloader.queue_free()

func _save_resource(object: ArcweaveAsset) -> Error:
	var error = ResourceSaver.save(object, object.resource_path)
	if error != OK:
		printerr("[Arcweave] Error saving resource!")
		printerr(error)
	return error

## Downloads the media of the project with a cancelable progress dialog and returns the downloader, which the caller
## frees. Both are added to the editor's base control, so they outlive this inspector when another object is selected.
func download_media(object: ArcweaveAsset, project_settings: Dictionary) -> ArcweaveMediaDownloader:
	var downloader = ArcweaveMediaDownloader.new(object)
	var dialog = AcceptDialog.new()
	dialog.title = "Arcweave"
	dialog.ok_button_text = "Cancel"
	var container = VBoxContainer.new()
	var label = Label.new()
	label.text = "Downloading media..."
	var progress_bar = ProgressBar.new()
	progress_bar.max_value = 1.0
	progress_bar.custom_minimum_size = Vector2(400, 0)
	container.add_child(label)
	container.add_child(progress_bar)
	dialog.add_child(container)
	dialog.confirmed.connect(downloader.cancel)
	dialog.canceled.connect(downloader.cancel)
	downloader.progress_changed.connect(func(info, value):
		label.text = info
		progress_bar.value = value)

	var base_control = EditorInterface.get_base_control()
	base_control.add_child(downloader)
	base_control.add_child(dialog)
	dialog.popup_centered()
	await downloader.download(project_settings)
	dialog.queue_free()
	return downloader

## Edits the API key saved in the user settings, outside the project, and moves a key saved in the resource by an older
## version of the plugin there.
func _create_api_key_editor(object: ArcweaveAsset) -> Control:
	var container = VBoxContainer.new()
	var row = HBoxContainer.new()
	var label = Label.new()
	label.text = "API Key"
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	label.size_flags_stretch_ratio = 0.7
	label.tooltip_text = ("Saved for you only, in " + ArcweaveUserSettings.get_file_path() + ", outside the project folder, so "
		+ "it isn't committed or exported. Used by all the ArcweaveAssets of this project.\nWhen empty, the "
		+ ArcweaveUserSettings.API_KEY_ENVIRONMENT_VARIABLE + " environment variable is used.")
	label.mouse_filter = Control.MOUSE_FILTER_STOP
	var key_edit = LineEdit.new()
	key_edit.secret = true
	key_edit.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	key_edit.text = ArcweaveUserSettings.get_api_key()
	key_edit.tooltip_text = label.tooltip_text
	if not ArcweaveUserSettings.get_environment_api_key().is_empty():
		key_edit.placeholder_text = "Using " + ArcweaveUserSettings.API_KEY_ENVIRONMENT_VARIABLE
	# set_api_key() reports errors
	key_edit.text_changed.connect(func(text): ArcweaveUserSettings.set_api_key(text))
	row.add_child(label)
	row.add_child(key_edit)
	container.add_child(row)

	if object.has_legacy_api_key():
		var warning = Label.new()
		warning.text = ("This resource contains an API key saved by an older version of the plugin, which commits it to "
			+ "version control and includes it in exports.")
		warning.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
		warning.custom_minimum_size = Vector2(100, 0)
		warning.add_theme_color_override("font_color", Color.DARK_ORANGE)
		var move_button = Button.new()
		move_button.text = "Move API Key Out of Resource"
		move_button.tooltip_text = ("Moves the key to your user settings and saves the resource without it. If you already "
			+ "have the same key there or in the " + ArcweaveUserSettings.API_KEY_ENVIRONMENT_VARIABLE + " environment "
			+ "variable, it is kept. If you have a different key, you are asked before the resource's key is removed.")
		move_button.pressed.connect(_move_api_key.bind(object, false, [warning, move_button]))
		container.add_child(warning)
		container.add_child(move_button)
	return container

## [param controls] are the warning and the button, removed once the key is moved
func _move_api_key(object: ArcweaveAsset, discard_different_key: bool, controls: Array):
	var path = object.resource_path
	match ArcweaveUserSettings.move_api_key_out_of_asset(object, discard_different_key):
		ArcweaveUserSettings.MoveResult.MOVED:
			print("[Arcweave] Moved the API key of \"" + path + "\" to " + ArcweaveUserSettings.get_file_path())
		ArcweaveUserSettings.MoveResult.KEPT_EXISTING:
			print("[Arcweave] Removed the API key of \"" + path + "\", keeping the key in your user settings or environment")
		ArcweaveUserSettings.MoveResult.SAVE_SCENE:
			EditorInterface.mark_scene_as_unsaved()
			push_warning("[Arcweave] Removed the API key of the ArcweaveAsset. Save the scene that contains it to remove the "
				+ "key from the file.")
		ArcweaveUserSettings.MoveResult.DIFFERENT_KEY_EXISTS:
			var dialog = ConfirmationDialog.new()
			dialog.title = "Arcweave"
			dialog.dialog_text = ("Your user settings or the " + ArcweaveUserSettings.API_KEY_ENVIRONMENT_VARIABLE
				+ " environment variable already have a different API key.\n\nRemove the key from this resource and keep "
				+ "yours? The resource's key will be lost.")
			dialog.ok_button_text = "Remove the Resource's Key"
			dialog.confirmed.connect(_move_api_key.bind(object, true, controls))
			dialog.visibility_changed.connect(func():
				if not dialog.visible:
					dialog.queue_free())
			EditorInterface.get_base_control().add_child(dialog)
			dialog.popup_centered()
			return
		ArcweaveUserSettings.MoveResult.FAILED:
			push_error("[Arcweave] Could not move the API key out of \"" + path + "\"")
			return
	# Removed after the button's signal, keeping the inspected object, eg. the ArcweaveNode that contains the asset
	for control in controls:
		if is_instance_valid(control):
			control.queue_free()
