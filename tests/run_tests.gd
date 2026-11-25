## Runs the plugin's tests in headless Godot and exits with the number of failed tests.
## Build the C# project and import the project first, so the plugin's classes are known, then run from the project folder:
##   dotnet build CSharp_Plugin.csproj
##   godot --headless --path . --import
##   godot --headless --path . --script res://tests/run_tests.gd
extends SceneTree

const TEST_SUITES = [
	"res://tests/PluginTests.cs",
	"res://tests/api_key_tests.gd",
]

func _initialize():
	var failures := 0
	for path in TEST_SUITES:
		print(path)
		var suite = load(path)
		if suite == null or not suite.can_instantiate():
			printerr("  FAIL could not load the test suite. Build the C# project and import the project first.")
			failures += 1
			continue
		failures += suite.new().Run()
	if failures == 0:
		print("All tests passed")
	else:
		printerr(str(failures) + " test(s) failed")
	quit(failures)
