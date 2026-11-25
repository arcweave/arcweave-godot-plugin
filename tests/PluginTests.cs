using System;
using System.Linq;
using Arcweave.Interpreter;
using Arcweave.Project;
using Godot;
using Godot.Collections;

namespace Arcweave.Tests;

/// <summary>
/// Tests for the plugin's C# code, run in headless Godot by tests/run_tests.gd, since the project classes need the
/// engine. Run returns the number of failed tests.
/// </summary>
public partial class PluginTests : RefCounted
{
	private int _failures;

	public int Run()
	{
		_failures = 0;
		Test("OmittedListsAreImportedAsEmpty", () => ListsAreImportedAsEmpty(nullLists: false));
		Test("NullListsAreImportedAsEmpty", () => ListsAreImportedAsEmpty(nullLists: true));
		Test(nameof(MissingStartingElementIsImportedAsNone), MissingStartingElementIsImportedAsNone);
		Test(nameof(ResetAllWithoutArgumentsRestoresAllVariableDefaults), ResetAllWithoutArgumentsRestoresAllVariableDefaults);
		Test(nameof(ResetAllWithArgumentsKeepsTheGivenVariables), ResetAllWithArgumentsKeepsTheGivenVariables);
		Test(nameof(ConditionResultsArePositiveNumbersAndNonEmptyStrings), ConditionResultsArePositiveNumbersAndNonEmptyStrings);
		Test(nameof(ConditionsEvaluateWithTheProjectVariables), ConditionsEvaluateWithTheProjectVariables);
		Test(nameof(ResetAllWithoutVariables), ResetAllWithoutVariables);
		Test(nameof(FoldersWithOmittedChildrenAreNotImported), FoldersWithOmittedChildrenAreNotImported);
		Test(nameof(InvalidElementsAndPathsKeepTheStoryUsable), InvalidElementsAndPathsKeepTheStoryUsable);
		Test(nameof(MissingStartingElementKeepsItsId), MissingStartingElementKeepsItsId);
		return _failures;
	}

	private void Test(string name, Action test)
	{
		try
		{
			test();
			GD.Print("  PASS ", name);
		}
		catch (Exception e)
		{
			_failures++;
			GD.PrintErr("  FAIL ", name, ": ", e.Message);
		}
	}

	private static void Assert(bool condition, string message)
	{
		if (!condition) throw new Exception(message);
	}

	private static void AreEqual<T>(T expected, T actual, string what)
	{
		if (!Equals(expected, actual)) throw new Exception($"{what}: expected '{expected}', got '{actual}'");
	}

	// Arcweave exports omit empty lists, eg. an ending element has no 'outputs' and most elements have no
	// 'components'. Ported from the Unity plugin's OmittedListsTests.
	private void ListsAreImportedAsEmpty(bool nullLists)
	{
		var project = new ProjectMaker(Parse(OmittedListsProjectJson(nullLists))).MakeProject();

		var board = project.Boards.Values.Single();
		Assert(board.Notes.Count == 0, "the board has no notes");
		Assert(board.Jumpers.Count == 0, "the board has no jumpers");
		Assert(board.Connections.Count == 0, "the board has no connections");
		Assert(board.Attributes.Count == 0, "the board has no attributes");

		var start = project.Elements["start"];
		var end = project.Elements["end"];
		Assert(project.StartingElement == start, "the starting element is 'start'");
		AreEqual(2, board.Elements.Count, "board elements");
		var branch = board.Branches.Single();

		Assert(start.Components.Count == 0, "'start' has no components");
		AreEqual(1, start.Outputs.Count, "'start' outputs");
		Assert(start.Outputs[0].Target == branch, "'start' goes to the branch");
		Assert(end.Outputs.Count == 0, "'end' has no outputs");
		Assert(end.Components.Count == 0, "'end' has no components");
		Assert(end.Attributes.Count == 0, "'end' has no attributes");
		AreEqual("", end.RawContent ?? "", "'end' content");

		// A branch without else-if or else conditions only has its if condition
		var condition = branch.Conditions.Single();
		Assert(condition.Output.Target == end, "the if condition goes to 'end'");

		var party = start.Attributes.Single();
		AreEqual(Interpreter.INodes.IAttribute.DataType.ComponentList, party.Type, "'party' type");
		Assert(((Array<Component>)party.data).Count == 0, "'party' has no components");
		Assert(project.Components.Values.Single().Attributes.Count == 0, "the component has no attributes");
		Assert(project.Assets.Count == 0, "the project has no assets");

		var story = new Story(Parse(OmittedListsProjectJson(nullLists)));
		AreEqual("start", story.GetCurrentElement().Id, "story current element");
		var options = story.GenerateCurrentOptions();
		story.SelectPath(options.Paths.Single());
		AreEqual("end", story.GetCurrentElement().Id, "story element after the branch");
	}

	private void MissingStartingElementIsImportedAsNone()
	{
		foreach (var startingElement in new[] { null, "\"\"", "\"missing\"" })
		{
			var json = OmittedListsProjectJson(false).Replace("\"startingElement\": \"start\",",
				startingElement == null ? "" : $"\"startingElement\": {startingElement},");
			var project = new ProjectMaker(Parse(json)).MakeProject();
			Assert(project.StartingElement == null, $"no starting element for {startingElement ?? "a missing key"}");
		}

		// The story reports the missing starting element instead of throwing, and can still be started by id
		var story = new Story(Parse(OmittedListsProjectJson(false).Replace("\"startingElement\": \"start\",", "")));
		Assert(story.GetCurrentElement() == null, "the story has no current element");
		Assert(story.GetCurrentRuntimeContent() == null, "no runtime content");
		Assert(story.GenerateCurrentOptions() == null, "no options");
		AreEqual("", story.GetSave()["currentElement"].AsString(), "saved current element");
		story.ResetStory();
		Assert(story.GetCurrentElement() == null, "resetting keeps no current element");
		story.SetCurrentElementById("end");
		AreEqual("end", story.GetCurrentElement().Id, "story element set by id");
	}

	private void ResetAllWithoutArgumentsRestoresAllVariableDefaults()
	{
		var project = new ProjectMaker(Parse(VariablesProjectJson)).MakeProject();
		project.SetVariable("health", 90);
		project.SetVariable("name", "Changed");

		var output = new AwInterpreter(project).RunScript("<pre><code>resetAll()</code></pre>");

		AreEqual(2, output.Changes.Count, "variable changes");
		AreEqual(100, ((Variant)output.Changes["health"]).AsInt32(), "health");
		AreEqual("Hero", ((Variant)output.Changes["name"]).AsString(), "name");
	}

	private void ResetAllWithArgumentsKeepsTheGivenVariables()
	{
		var project = new ProjectMaker(Parse(VariablesProjectJson)).MakeProject();
		project.SetVariable("health", 90);
		project.SetVariable("name", "Changed");

		var output = new AwInterpreter(project).RunScript("<pre><code>resetAll(name)</code></pre>");

		AreEqual(1, output.Changes.Count, "variable changes");
		AreEqual(100, ((Variant)output.Changes["health"]).AsInt32(), "health");
	}

	// Matches GetBoolValue in the C# and the valueToBool in the C++ reference interpreters
	private void ConditionResultsArePositiveNumbersAndNonEmptyStrings()
	{
		object[] truthy = { true, 1, 2.5, 2.5f, 1L, "text", Variant.From(true), Variant.From(3), Variant.From(0.5), Variant.From("a") };
		object[] falsy = { false, 0, -1, 0.0, -2.5, -2.5f, -1L, "", null, new object(), Variant.From(-1), Variant.From(-0.5), Variant.From(""), new Variant() };
		foreach (var value in truthy) Assert(Condition.IsTruthy(value), $"{Describe(value)} is true");
		foreach (var value in falsy) Assert(!Condition.IsTruthy(value), $"{Describe(value)} is false");
	}

	private void ConditionsEvaluateWithTheProjectVariables()
	{
		var project = new ProjectMaker(Parse(VariablesProjectJson)).MakeProject();
		(string script, bool expected)[] cases =
		{
			("health > 50", true),
			("health < 50", false),
			("health", true),
			("name", true),
			("", true),
			("health - 200", false),
			("-health", false),
			("health / 3.0", true),
			// Not a condition: the result isn't a bool, which used to throw
			("health = 10", false),
		};
		foreach (var (script, expected) in cases)
		{
			AreEqual(expected, new Condition("c", project, script, null).Evaluate(), $"'{script}'");
		}
	}

	// Each of these reports an error and leaves the current element unchanged
	private void InvalidElementsAndPathsKeepTheStoryUsable()
	{
		var story = new Story(Parse(OmittedListsProjectJson(false)));
		var start = story.GetCurrentElement();
		var visits = start.Visits;

		story.SetCurrentElementById("unknown");
		story.SetCurrentElementById("");
		story.SetCurrentElementById(null);
		story.SetCurrentElement(null);
		story.SelectPath(null);
		story.SelectPath(new Path());
		story.SelectPath(new Path { label = "no target" });

		Assert(story.GetCurrentElement() == start, "the current element doesn't change");
		AreEqual(visits, start.Visits, "visits of the current element");
		AreEqual("<p>Hello</p>".Replace("<p>", "").Replace("</p>", ""), story.GetCurrentRuntimeContent(), "runtime content");

		// The story still works
		var options = story.GenerateCurrentOptions();
		story.SelectPath(options.Paths.Single());
		AreEqual("end", story.GetCurrentElement().Id, "story element after the branch");
		story.SetCurrentElementById("start");
		AreEqual("start", story.GetCurrentElement().Id, "story element set by id");
	}

	private void MissingStartingElementKeepsItsId()
	{
		var json = OmittedListsProjectJson(false).Replace("\"startingElement\": \"start\"", "\"startingElement\": \"missing\"");
		var project = new ProjectMaker(Parse(json)).MakeProject();
		Assert(project.StartingElement == null, "no starting element");
		AreEqual("missing", project.StartingElementId, "starting element ID");
		AreEqual("start", new ProjectMaker(Parse(OmittedListsProjectJson(false))).MakeProject().StartingElementId, "existing starting element ID");
		AreEqual("", new ProjectMaker(Parse(OmittedListsProjectJson(false).Replace("\"startingElement\": \"start\",", ""))).MakeProject().StartingElementId, "no starting element ID");
	}

	private void ResetAllWithoutVariables()
	{
		var project = new ProjectMaker(Parse(VariablesProjectJson.Replace("\"variables\": {", "\"unused\": {"))).MakeProject();
		var output = new AwInterpreter(project).RunScript("<pre><code>resetAll()</code></pre>");
		AreEqual(0, output.Changes.Count, "variable changes");
	}

	// Empty folders may omit their children, or have null children
	private void FoldersWithOmittedChildrenAreNotImported()
	{
		var json = OmittedListsProjectJson(false)
			.Replace("\"boards\": {", "\"boards\": { \"boards-root\": { \"root\": true, \"name\": \"Root\" },")
			.Replace("\"components\": {", "\"components\": { \"components-root\": { \"root\": true, \"name\": \"Root\" },")
			.Replace("\"assets\": {}", "\"assets\": { \"assets-root\": { \"root\": true, \"children\": [\"folder\", \"empty\"] }, "
				+ "\"folder\": { \"name\": \"Folder\", \"children\": null }, \"empty\": { \"name\": \"Empty\" } }")
			.Replace("\"conditions\": { \"if\":", "\"conditions\": { \"unconnected\": { \"output\": \"\", \"script\": \"false\" }, \"if\":");
		var project = new ProjectMaker(Parse(json)).MakeProject();
		AreEqual(1, project.Boards.Count, "boards");
		AreEqual(1, project.Components.Count, "components");
		AreEqual(0, project.Assets.Count, "assets");
		Assert(json.Contains("unconnected"), "the test project has the unconnected condition");
	}

	private static string Describe(object value) => value switch
	{
		null => "null",
		Variant v => $"Variant {v.VariantType} '{v}'",
		_ => $"{value.GetType().Name} '{value}'"
	};

	private static Dictionary Parse(string json)
	{
		var result = Json.ParseString(json);
		if (result.VariantType != Variant.Type.Dictionary) throw new Exception("Invalid test JSON");
		return result.AsGodotDictionary();
	}

	/// <summary>A project whose empty lists are either omitted, as Arcweave exports them, or null.</summary>
	private static string OmittedListsProjectJson(bool nullLists)
	{
		string List(string key) => nullLists ? $@", ""{key}"": null" : "";
		return $@"{{
			""name"": ""Omitted Lists Test"",
			""startingElement"": ""start"",
			""boards"": {{ ""main"": {{ ""name"": ""Main"", ""elements"": [""start"", ""end""], ""branches"": [""branch""]{List("jumpers")}{List("notes")}{List("connections")}{List("attributes")} }} }},
			""elements"": {{
				""start"": {{ ""title"": ""<p>Start</p>"", ""content"": ""<p>Hello</p>"", ""outputs"": [""to-branch""], ""attributes"": [""party""]{List("components")} }},
				""end"": {{ ""title"": ""<p>Ending</p>""{List("outputs")}{List("components")}{List("attributes")}{List("assets")} }}
			}},
			""branches"": {{ ""branch"": {{ ""conditions"": {{ ""ifCondition"": ""if""{List("elseIfConditions")}{List("elseCondition")} }} }} }},
			""jumpers"": {{}},
			""conditions"": {{ ""if"": {{ ""output"": ""to-end"", ""script"": ""true"" }} }},
			""connections"": {{
				""to-branch"": {{ ""sourceid"": ""start"", ""sourceType"": ""elements"", ""targetid"": ""branch"", ""targetType"": ""branches"" }},
				""to-end"": {{ ""sourceid"": ""if"", ""sourceType"": ""conditions"", ""targetid"": ""end"", ""targetType"": ""elements"" }}
			}},
			""components"": {{ ""hero"": {{ ""name"": ""Hero""{List("attributes")} }} }},
			""attributes"": {{ ""party"": {{ ""name"": ""Party"", ""cType"": ""elements"", ""cId"": ""start"", ""value"": {{ ""type"": ""component-list""{List("data")} }} }} }},
			""variables"": {{}},
			""notes"": {{}},
			""assets"": {{}}
		}}";
	}

	private const string VariablesProjectJson = @"{
		""name"": ""Variables Test"",
		""startingElement"": ""start"",
		""boards"": { ""main"": { ""name"": ""Main"", ""elements"": [""start""], ""jumpers"": [], ""branches"": [], ""connections"": [], ""notes"": [] } },
		""elements"": { ""start"": { ""title"": ""<p>Start</p>"", ""content"": """", ""outputs"": [] } },
		""branches"": {}, ""jumpers"": {}, ""conditions"": {}, ""connections"": {}, ""components"": {},
		""attributes"": {}, ""notes"": {},
		""assets"": { ""root"": { ""root"": true, ""children"": [] } },
		""variables"": {
			""health"": { ""name"": ""health"", ""type"": ""integer"", ""value"": 100 },
			""name"": { ""name"": ""name"", ""type"": ""string"", ""value"": ""Hero"" }
		}
	}";
}
