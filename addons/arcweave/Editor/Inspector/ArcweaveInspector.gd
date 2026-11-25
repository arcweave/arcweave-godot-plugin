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
	label.tooltip_text = ("Saved for you only, in " + ArcweaveUserSettings.file_path + ", outside the project folder, so "
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
	key_edit.text_changed.connect(func(text):
		if ArcweaveUserSettings.set_api_key(text) != OK:
			push_error("[Arcweave] Could not save the API key in " + ArcweaveUserSettings.file_path))
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
		move_button.tooltip_text = ("Moves the key to your user settings, unless you already have a key there or in the "
			+ ArcweaveUserSettings.API_KEY_ENVIRONMENT_VARIABLE + " environment variable, and saves the resource without it.")
		move_button.pressed.connect(func():
			match ArcweaveUserSettings.move_api_key_out_of_asset(object):
				ArcweaveUserSettings.MoveResult.MOVED:
					print("[Arcweave] Moved the API key of \"" + object.resource_path + "\" to " + ArcweaveUserSettings.file_path)
				ArcweaveUserSettings.MoveResult.KEPT_EXISTING:
					print("[Arcweave] Removed the API key of \"" + object.resource_path
						+ "\", keeping the key already in your user settings or environment")
				ArcweaveUserSettings.MoveResult.FAILED:
					push_error("[Arcweave] Could not move the API key out of \"" + object.resource_path + "\"")
					return
			if object.resource_path.contains("::"):
				push_warning("[Arcweave] Save the scene to remove the API key from the file.")
			# Rebuilds the inspector without the warning
			EditorInterface.inspect_object(null)
			EditorInterface.inspect_object(object))
		container.add_child(warning)
		container.add_child(move_button)
	return container
