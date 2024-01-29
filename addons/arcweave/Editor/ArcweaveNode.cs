using Godot;
using Godot.Collections;

namespace Arcweave.Editor;

public partial class ArcweaveNode : Node
{
	[Export] public GodotObject ArcweaveAsset { get; set; }
	[Signal] public delegate void ProjectUpdatedEventHandler();
	public Story Story { get; private set; }
	private Node ApiRequest { get; set; }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var requestScript = GD.Load<GDScript>("res://addons/arcweave/Editor/APIRequestScript.gd");
		ApiRequest = (Node)requestScript.New(ArcweaveAsset);
		ArcweaveAsset.Connect("project_updated", Callable.From((Dictionary projectSettings) => OnProjectUpdate(projectSettings)));
		Story = CreateStory();
		AddChild(ApiRequest);
	}

	/// <summary>
	/// Calls for an update through API for the project settings. When the project settings are updated, a
	/// "ProjectUpdated" signal is emitted. 
	/// </summary>
	public void UpdateStory()
	{
		ArcweaveAsset.Call("refresh_project", ApiRequest);
	}

	/// <summary>
	/// Signal handler for "project_updated" signal from ArcweaveAsset.
	/// </summary>
	/// <param name="projectSettings"></param>
	private void OnProjectUpdate(Dictionary projectSettings)
	{
		Story = Story.UpdateStory(projectSettings);
		EmitSignal(SignalName.ProjectUpdated);
	}

	/// <summary>
	/// Creates a Story from the current ProjectSettings of the ArcweaveAsset.
	/// </summary>
	/// <returns>The newly created Story</returns>
	public Story CreateStory()
	{
		Dictionary projectSettings = (Dictionary)ArcweaveAsset.Get("project_settings");
		return new Story(projectSettings);
	}
}