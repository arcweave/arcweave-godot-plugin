@tool
extends EditorInspectorPlugin

var api_request : APIRequest

func _can_handle(object):
	return object is ArcweaveAsset

func _parse_property(object, type, name, hint_type, hint_string, usage_flags, wide):
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
	if object.download_media and object.receive_method == "WebAPI":
		await download_media(object, new_project_settings)
	elif not object.download_media:
		# The project no longer uses the downloaded media, so the resource stops loading and exporting it.
		# The files are kept, and deleted by a later download if the project doesn't use them anymore.
		object.media_resources = {}
	var error = ResourceSaver.save(object, object.resource_path)
	if error != OK:
		printerr("[Arcweave] Error saving resource!")
		printerr(error)

## Downloads the media of the project with a cancelable progress dialog.
## Both are added to the editor's base control, so they outlive this inspector when another object is selected.
func download_media(object: ArcweaveAsset, project_settings: Dictionary):
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
	downloader.queue_free()
