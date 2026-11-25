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
- The key saved in the inspector is used by all the ArcweaveAssets of the project, before a key saved in a resource. If
  your ArcweaveAssets use different keys, pass each one's key to `refresh_project(api_request, api_key)` or
  `ArcweaveNode.UpdateStoryWithApiKey(apiKey)`. The order the key is found in is described in
  [Your API Key](README.md#your-api-key).
- Code that sets `arcweave_asset.api_key` still works, but the key is then saved in the resource again. Pass the key to
  the refresh instead.

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
  element without outputs. Root folders without children, and folders whose children are null, are no longer imported
  as boards, components or assets.
- A project without a starting element, or whose starting element doesn't exist, imports. The story reports it, naming
  the missing ID, and starts once you call `SetCurrentElementById`.
- `resetAll()` without arguments resets all the variables instead of failing.
- A condition that isn't an expression, eg. an assignment such as `score = 10`, is false instead of failing. Conditions
  are evaluated as before otherwise: numbers are true when greater than 0.
- `Story.SetCurrentElementById` with an unknown ID, `SetCurrentElement(null)` and `SelectPath` with a path without a
  target element report an error and keep the current element, instead of throwing.
- A refresh that fails, eg. because of a wrong API key, a missing or unreadable project file, or data that isn't an
  Arcweave project, keeps the current story and project data, reports the error and doesn't emit
  `ArcweaveNode.ProjectUpdated`. `project_updated` is emitted with null. Project data that looks like a project but is
  broken inside, eg. a board that lists a missing element, still replaces the project data and fails to update the
  story.
- A refresh that is pressed again while one is pending, or a request that leaves the scene tree, no longer breaks later
  refreshes. Requests time out after 30 seconds.
- The README's API documentation matches the code.
