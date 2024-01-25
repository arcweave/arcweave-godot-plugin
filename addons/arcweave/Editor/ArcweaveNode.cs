using Godot;
using Godot.Collections;

namespace Arcweave.Editor;

public partial class ArcweaveNode : Node
{
	[Export] public GodotObject ArcweaveResource { get; set; }
	[Signal] public delegate void ProjectUpdatedEventHandler();
	public Story Story { get; private set; }
	private Node ApiRequest { get; set; }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var requestScript =  GD.Load<GDScript>("res://addons/Arcweave/Editor/APIRequest.gd");
		ApiRequest = (Node)requestScript.New(ArcweaveResource);
		Story = CreateStory();
		AddChild(ApiRequest);
	}

	/// <summary>
	/// Calls for an update through API for the project settings. When the project settings are updated, a
	/// "ProjectUpdated" signal is emitted. 
	/// </summary>
	public void UpdateStory()
	{
		ArcweaveResource.Connect("project_updated", Callable.From((Dictionary projectSettings) => OnProjectUpdate(projectSettings)));
		ArcweaveResource.Call("refresh_project", ApiRequest);
	}

	/// <summary>
	/// Signal handler for "project_updated" signal from ArcweaveResource.
	/// </summary>
	/// <param name="projectSettings"></param>
	private void OnProjectUpdate(Dictionary projectSettings)
	{
		Story = Story.UpdateStory(projectSettings);
		EmitSignal(SignalName.ProjectUpdated);
	}
	
	/// <summary>
	/// Creates a Story from the current ProjectSettings of the ArcweaveResource.
	/// </summary>
	/// <returns>The newly created Story</returns>
	public Story CreateStory()
	{
		Dictionary projectSettings = (Dictionary) ArcweaveResource.Get("project_settings");
		return new Story(projectSettings);
	}
}