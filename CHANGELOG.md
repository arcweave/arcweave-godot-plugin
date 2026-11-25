# Changelog

The notable changes of each release of the Arcweave Godot Plugin. Earlier releases are described in their
[GitHub releases](https://github.com/arcweave/arcweave-godot-plugin/releases).

## 3.2.0

### Upgrading from 3.1

> [!WARNING]
> **If your API key was ever committed or shared, generate a new one in Arcweave.** Earlier versions saved the key in
> the ArcweaveAsset resource, so it is in your version control history and in games you exported, even after you
> move it out of the resource.

- The API key is no longer saved in the ArcweaveAsset resource. After upgrading, select each ArcweaveAsset that has a
  key and click **Move API Key Out of Resource** in its inspector. Until then the plugin still uses the key from the
  resource, and keeps it there, so nothing stops working. See [Your API Key](README.md#your-api-key).
- Code that sets `arcweave_asset.api_key` still works, but the key is then saved in the resource again. Pass the key to
  `refresh_project(api_request, api_key)` or `ArcweaveNode.UpdateStoryWithApiKey(apiKey)` instead.

### Added

- The API key is saved per user in `user://arcweave_settings.cfg`, outside the project folder, from the **API Key**
  field in the ArcweaveAsset inspector. Games run from the editor read it too, so refreshing at runtime keeps working.
- WebAPI refreshes fall back to the `ARCWEAVE_API_KEY` environment variable, eg. on a build server.
- An API key can be passed to a refresh: `ArcweaveNode.UpdateStoryWithApiKey(apiKey)` and
  `ArcweaveAsset.refresh_project(api_request, api_key)`, eg. in an exported game.
- **Move API Key Out of Resource** in the ArcweaveAsset inspector moves a key saved by an older version to your user
  settings. It asks before removing the resource's key if you already have a different one.
- Exporting a game warns about ArcweaveAssets that still contain an API key, also inside scenes and other resources.
- Rich text: underline and strikethrough become `[u]` and `[s]`, and `<b>` and `<i>` are converted too.
- `Project.StartingElementId`: the starting element's ID in the project data, also when no element has it.
- The plugin has a description in Godot's plugin list.

### Fixed

- Rich text: code blocks use BBCode's `[color]` instead of a tag RichTextLabel showed as text, `<br>` becomes a new
  line, and HTML entities such as `&lt;` and `&amp;` are decoded instead of removed or shown as written. A `<` that
  doesn't start a tag is kept.
- Projects whose export omits empty lists or values, or writes them as null, import instead of failing, eg. an ending
  element without outputs or an empty folder. Empty folders are no longer imported as boards, components or assets.
- A project without a starting element, or whose starting element doesn't exist, imports. The story reports it, naming
  the missing ID, and starts once you call `SetCurrentElementById`.
- `resetAll()` without arguments resets all the variables instead of failing.
- A condition whose result isn't a boolean, eg. a decimal number, is evaluated instead of failing. Numbers are true when
  greater than 0, as before.
- `Story.SetCurrentElementById` with an unknown ID, `SetCurrentElement(null)` and `SelectPath` with a path without a
  target element report an error and keep the current element, instead of throwing.
- A failed refresh, eg. with a wrong API key or a missing project file, keeps the current story and project data,
  reports the error and doesn't emit `ArcweaveNode.ProjectUpdated`. `project_updated` is emitted with null.
- A refresh that is pressed again while one is pending, or a request that leaves the scene tree, no longer breaks later
  refreshes. Requests time out after 30 seconds.
- The README's API documentation matches the code.
