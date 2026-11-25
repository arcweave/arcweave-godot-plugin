@tool
extends EditorExportPlugin
## Warns when an export includes an ArcweaveAsset that still contains an API key saved by an older version of the
## plugin, since the key is then exported with it. Checks ArcweaveAsset files and the ArcweaveAssets saved inside other
## resources and scenes.

func _get_name() -> String:
	return "Arcweave"

func _export_file(path: String, type: String, features: PackedStringArray):
	if not ArcweaveUserSettings.find_assets_with_api_key(path).is_empty():
		push_warning("[Arcweave] The export includes \"" + path + "\", which contains an ArcweaveAsset with an API key. "
			+ "Select the ArcweaveAsset and use \"Move API Key Out of Resource\" in its inspector to remove the key from it.")
