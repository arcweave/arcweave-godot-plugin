@tool
extends EditorExportPlugin
## Warns when an export includes an ArcweaveAsset that still contains an API key saved by an older version of the
## plugin, since the key is then exported with it.

func _get_name() -> String:
	return "Arcweave"

func _export_file(path: String, type: String, features: PackedStringArray):
	if not path.get_extension() in ["tres", "res"]:
		return
	var resource = ResourceLoader.load(path)
	if resource is ArcweaveAsset and resource.has_legacy_api_key():
		push_warning("[Arcweave] The exported \"" + path + "\" contains an API key, so the export includes it. Select "
			+ "the resource and use \"Move API Key Out of Resource\" in its inspector to remove the key from it.")
