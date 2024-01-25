#if TOOLS
using Godot;
using Godot.Collections;

namespace Arcweave.Editor;

[Tool]
public partial class ArcweavePlugin : EditorPlugin
{
	private string _envFileLocation = "res://addons/Arcweave/env.json";
	private string _defaultDomain = "https://www.arcweave.com/";
	
	private Resource _logoResource = GD.Load<Resource>("res://addons/Arcweave/Editor/icon.svg");
	private CompressedTexture2D _logo;
	private string _resourceName = "ArcweaveResource";
	private GDScript _arcweaveResource;
	private EditorInspectorPlugin _gdInspector;
	
	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
		var inspectorScript = GD.Load<GDScript>("res://addons/Arcweave/Editor/Inspector/ArcweaveInspector.gd");
		_gdInspector = (EditorInspectorPlugin) inspectorScript.New();
		AddInspectorPlugin(_gdInspector);

		_logo = (CompressedTexture2D)GD.Load("res://addons/Arcweave/Editor/icon.svg");
		
		AddArcweaveNodes();
		AddArcweaveResource();
	}

	public override void _ExitTree()
	{
		RemoveArcweaveResource();
		RemoveArcweaveNodes();
		RemoveInspectorPlugin(_gdInspector);
	}

	/// <summary>
	/// Adds the Plugin's Custom Nodes
	/// </summary>
	private void AddArcweaveNodes()
	{
		var script = GD.Load<Script>("res://addons/Arcweave/Editor/ArcweaveNode.cs");
		
		AddCustomType("ArcweaveNode", "Node", script, _logo);
	}

	/// <summary>
	/// Removes the Plugin's Custom Nodes
	/// </summary>
	private void RemoveArcweaveNodes()
	{
		RemoveCustomType("ArcweaveNode");
	}

	/// <summary>
	/// Adds the Plugin's Custom Resources
	/// </summary>
	private void AddArcweaveResource()
	{
		_arcweaveResource = GD.Load<GDScript>("res://addons/Arcweave/Editor/ArcweaveResource.gd");
		AddCustomType(_resourceName, "Resource", _arcweaveResource, _logo);
	}

	/// <summary>
	/// Removes the Plugin's Custom Resources
	/// </summary>
	private void RemoveArcweaveResource()
	{
		RemoveCustomType(_resourceName);
	}
}
#endif
