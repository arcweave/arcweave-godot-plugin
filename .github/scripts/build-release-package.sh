#!/usr/bin/env bash
# Builds ArcweaveForGodot.zip from the addons/arcweave folder committed at a git ref.
# Extracting it into a Godot project's root folder adds the plugin as addons/arcweave.
#
# Usage: build-release-package.sh [ref] [output directory]
#
# The README links to this file name through
# https://github.com/arcweave/arcweave-godot-plugin/releases/latest/download/ArcweaveForGodot.zip,
# so renaming it breaks that link.
set -euo pipefail

ref="${1:-HEAD}"
mkdir -p "${2:-dist}"
out_dir=$(cd "${2:-dist}" && pwd)
zip="$out_dir/ArcweaveForGodot.zip"

# The package only contains addons/arcweave, so it carries the license through a copy there.
git diff --quiet "$ref:LICENSE" "$ref:addons/arcweave/LICENSE" ||
  { echo "addons/arcweave/LICENSE differs from LICENSE" >&2; exit 1; }

# .gitattributes keeps everything outside addons/arcweave out of the archive, the same way
# the Asset Library downloads it, and only committed files are packaged, so local untracked
# files never end up in a release.
git archive --format=zip -o "$zip" "$ref"

# Fails if a change to .gitattributes lets the example project in.
entries=$(git archive "$ref" | tar -t)
outside=$(grep -v -e '^addons/$' -e '^addons/arcweave/' <<< "$entries" || true)
[ -z "$outside" ] || { printf 'The package contains files outside addons/arcweave:\n%s\n' "$outside" >&2; exit 1; }
grep -qx 'addons/arcweave/plugin.cfg' <<< "$entries" ||
  { echo "The package has no addons/arcweave/plugin.cfg" >&2; exit 1; }

echo "Built $zip from $ref"
