using System;
using System.Linq;
using Arcweave.Interpreter;
using Arcweave.Interpreter.INodes;
using Arcweave.Project;
using Godot;

namespace Arcweave.Tests;

public partial class ComponentBoardVariableTests : Node
{
    private const string FixturePath = "res://tests/fixtures/component-board-variables.json";

    public override void _Ready()
    {
        try
        {
            RunTests();
            GD.Print("ARCWEAVE_COMPONENT_BOARD_VARIABLES_PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private static void RunTests()
    {
        var project = MakeProject();
        var board = project.Boards["board-1"];
        var component = project.Components["component-1"];

        AssertEqual(1, project.Variables.Count, "global variable count");
        AssertEqual("castle", board.CustomId, "board custom ID");
        AssertEqual("hero", component.CustomId, "component custom ID");
        AssertEqual(4, board.Variables.Count, "board variable count");
        AssertEqual(2, component.Variables.Count, "component variable count");
        AssertEqual(7, project.GetAllVariables().Count(), "all variable count");

        AssertVariable(project.GetVariable("health"), "global-health", 100, null);
        AssertVariable(project.GetScopedVariable("health", "castle"), "board-health", 10, board);
        AssertVariable(project.GetScopedVariable("health", "hero"), "component-health", 20, component);
        AssertVariable(project.GetScopedVariable("is_open", "castle"), "board-open", true, board);
        AssertVariable(project.GetScopedVariable("rate", "castle"), "board-rate", 1.5d, board);
        AssertVariable(project.GetScopedVariable("title", "castle"), "board-title", string.Empty, board);
        AssertVariable(project.GetScopedVariable("label", "hero"), "component-label", string.Empty, component);
        Assert(project.GetScopedVariable("summary", "castle") == null, "rich text must not become a variable");
        Assert(board.Variables.All(variable => variable.Id != "board-ordinary"),
            "attribute without a custom ID must not become a variable");
        AssertVariable(project.Call(nameof(Arcweave.Project.Project.GetVariable), "health").AsGodotObject() as Variable,
            "global-health", 100, null);
        AssertVariable(project.Call(nameof(Arcweave.Project.Project.GetScopedVariable), "health", "castle")
            .AsGodotObject() as Variable, "board-health", 10, board);

        project.StartingElement.RunContentScript();
        AssertEqual(101, project.GetVariable("health").ObjectValue, "global assignment");
        AssertEqual(11, project.GetScopedVariable("health", "castle").ObjectValue, "board assignment");
        AssertEqual(21, project.GetScopedVariable("health", "hero").ObjectValue, "component assignment");

        var resetAll = new AwInterpreter(project).RunScript(
            "<pre><code>resetAll(hero.health)</code></pre>");
        Assert(resetAll.Changes.ContainsKey("global-health"), "resetAll must include the global ID");
        Assert(resetAll.Changes.ContainsKey("board-health"), "resetAll must include the board ID");
        Assert(!resetAll.Changes.ContainsKey("component-health"),
            "resetAll must exclude the exact component ID");

        var save = project.SaveVariables();
        Assert(save.ContainsKey("global-health"), "save data must be keyed by stable global ID");
        Assert(save.ContainsKey("board-health"), "save data must include board variables");
        Assert(save.ContainsKey("component-health"), "save data must include component variables");
        project.ResetVariables();
        project.LoadVariables(save);
        AssertEqual(101, project.GetVariable("health").ObjectValue, "global save/load");
        AssertEqual(11, project.GetScopedVariable("health", "castle").ObjectValue, "board save/load");
        AssertEqual(21, project.GetScopedVariable("health", "hero").ObjectValue, "component save/load");

        var refreshedProject = MakeProject();
        project.Merge(refreshedProject);
        AssertEqual(101, refreshedProject.GetVariableById("global-health").ObjectValue, "global refresh merge");
        AssertEqual(11, refreshedProject.GetVariableById("board-health").ObjectValue, "board refresh merge");
        AssertEqual(21, refreshedProject.GetVariableById("component-health").ObjectValue,
            "component refresh merge");
        AssertEqual(20, VariantToObject(refreshedProject.GetVariableById("component-health").DefaultValue),
            "refresh merge must preserve the authored default");

        AssertEqual(5, (int)IAttribute.DataType.Boolean, "boolean enum value");
        AssertEqual(6, (int)IAttribute.DataType.Integer, "integer enum value");
        AssertEqual(7, (int)IAttribute.DataType.Float, "float enum value");
        AssertEqual(3, (int)IAttribute.ContainerType.Board, "board container enum value");

        DisposeProject(refreshedProject);
        DisposeProject(project);

        var story = new Story(ReadProjectData());
        AssertEqual(7, story.VariableChanges.Variables.Count, "story variable tracking count");
        var storyChanges = story.GetVariableChanges();
        Assert(storyChanges.ContainsKey("global-health"), "story changes must use the global ID");
        Assert(storyChanges.ContainsKey("board-health"), "story changes must use the board ID");
        Assert(storyChanges.ContainsKey("component-health"), "story changes must use the component ID");
        var storySave = story.GetSave()["variables"].AsGodotDictionary<string, Variant>();
        Assert(storySave.ContainsKey("global-health"), "story saves must use stable IDs");
        Assert(storySave.ContainsKey("board-health"), "story saves must include board variables");
        Assert(storySave.ContainsKey("component-health"), "story saves must include component variables");
        var previousProject = story.GetProject();
        var previousChanges = story.VariableChanges;
        story.UpdateStory(ReadProjectData());
        var refreshedStoryChanges = story.GetVariableChanges();
        AssertEqual(3, refreshedStoryChanges.Count, "story refresh change count");
        Assert(refreshedStoryChanges.ContainsKey("global-health"), "story refresh global change");
        Assert(refreshedStoryChanges.ContainsKey("board-health"), "story refresh board change");
        Assert(refreshedStoryChanges.ContainsKey("component-health"), "story refresh component change");
        DisposeProject(previousProject);
        previousChanges.Free();
        DisposeProject(story.GetProject());
        story.VariableChanges.Free();
        story.Free();
    }

    private static Arcweave.Project.Project MakeProject()
    {
        return new ProjectMaker(ReadProjectData()).MakeProject();
    }

    private static Godot.Collections.Dictionary ReadProjectData()
    {
        var json = FileAccess.GetFileAsString(FixturePath);
        return Json.ParseString(json).AsGodotDictionary();
    }

    private static void AssertVariable(Variable variable, string id, object value, IHasVariables parent)
    {
        Assert(variable != null, $"variable '{id}' must exist");
        AssertEqual(id, variable.Id, $"{id} stable ID");
        AssertEqual(value, variable.ObjectValue, $"{id} current value");
        AssertEqual(value, VariantToObject(variable.DefaultValue), $"{id} default value");
        Assert(ReferenceEquals(parent, variable.Parent), $"{id} parent scope");
    }

    private static void DisposeProject(Arcweave.Project.Project project)
    {
        foreach (var variable in project.GetAllVariables()) variable.Free();
        foreach (var attribute in project.Boards.Values.SelectMany(board => board.Attributes)
                     .Concat(project.Components.Values.SelectMany(component => component.Attributes))
                     .Concat(project.Elements.Values.SelectMany(element => element.Attributes))
                     .Distinct())
        {
            attribute.Free();
        }
        foreach (var element in project.Elements.Values) element.Free();
        foreach (var component in project.Components.Values) component.Free();
        foreach (var board in project.Boards.Values) board.Free();
        foreach (var asset in project.Assets.Values) asset.Free();
        project.Free();
    }

    private static object VariantToObject(Variant value)
    {
        return value.VariantType switch
        {
            Variant.Type.String => value.AsString(),
            Variant.Type.Bool => value.AsBool(),
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.Float => value.AsDouble(),
            _ => null
        };
    }

    private static void AssertEqual(object expected, object actual, string message)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected '{expected}', got '{actual}'");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
