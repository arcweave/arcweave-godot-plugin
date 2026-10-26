using Godot;
using Godot.Collections;

namespace Arcweave.Editor;

public partial class ArcweaveNode : Node
{
	[Export] public GodotObject ArcweaveAsset { get; set; }
	[Signal] public delegate void ProjectUpdatedEventHandler();
	[Export] public Story Story { get; private set; }
	private Node ApiRequest { get; set; }
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var requestScript = GD.Load<GDScript>("res://addons/arcweave/Editor/APIRequestScript.gd");
		ApiRequest = (Node)requestScript.New(ArcweaveAsset);
		ArcweaveAsset.Connect("project_updated", Callable.From((Variant projectSettings) => OnProjectUpdate(projectSettings)));
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
	/// Signal handler for "project_updated" signal from ArcweaveAsset. A failed update sends null instead of the
	/// project data; the current story is then kept as it is and "ProjectUpdated" is not emitted.
	/// </summary>
	/// <param name="projectSettings">The new project data, or null if the update failed</param>
	private void OnProjectUpdate(Variant projectSettings)
	{
		if (projectSettings.VariantType != Variant.Type.Dictionary)
		{
			GD.PushWarning("[Arcweave] Project update failed, keeping the current story");
			return;
		}
		Story = Story.UpdateStory(projectSettings.AsGodotDictionary(), GetMediaPaths());
		EmitSignal(SignalName.ProjectUpdated);
	}

	/// <summary>
	/// Creates a Story from the current ProjectSettings of the ArcweaveAsset.
	/// </summary>
	/// <returns>The newly created Story</returns>
	public Story CreateStory()
	{
		Dictionary projectSettings = (Dictionary)ArcweaveAsset.Get("project_settings");
		return new Story(projectSettings, GetMediaPaths());
	}

	/// <summary>
	/// Returns the media downloaded from Arcweave's web API by asset ID.
	/// </summary>
	private Dictionary GetMediaPaths()
	{
		return ArcweaveAsset.HasMethod("get_media_paths")
			? ArcweaveAsset.Call("get_media_paths").AsGodotDictionary()
			: new Dictionary();
	}
}