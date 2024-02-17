using Godot;
using System;
using Arcweave.Editor;
using Arcweave.Project;
using Arcweave.Interpreter.INodes;
using Godot.Collections;

public partial class CSharpScene : Control
{
	public RichTextLabel TextContainer;
	public VBoxContainer OptionContainer;
	public Button SaveButton;
	public Button RefreshButton;
	public ArcweaveNode ArcweaveNode;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Using CSharp Script ./CSharpSceneScript.cs");
		ArcweaveNode = GetNode<ArcweaveNode>("ArcweaveNode");
		// Add the signal handler for project updates
		ArcweaveNode.ProjectUpdated += OnProjectUpdated;
		
		// Create the UI
		TextContainer = GetNode<RichTextLabel>("StoryContainer/TextWindow");
		OptionContainer = GetNode<VBoxContainer>("StoryContainer/OptionsContainer");
		SaveButton = GetNode<Button>("StoryContainer/UIButtonsContainer/SaveButton");
		RefreshButton = GetNode<Button>("StoryContainer/UIButtonsContainer/RefreshButton");
		RefreshButton.Pressed += RefreshProject;
		TextContainer.BbcodeEnabled = true;

		Repaint();
	}

	/// <summary>
	/// Adds the available options of the current element to the OptionContainer
	/// </summary>
	private void AddOptions()
	{
		foreach (var b in OptionContainer.GetChildren())
		{
			OptionContainer.RemoveChild(b);
		}
		Options options = ArcweaveNode.Story.GenerateCurrentOptions();
		if (options.Paths != null)
		{
			foreach (var path in options.Paths)
			{
				if (path.IsValid)
				{
					Button button = CreateButton(path);
					OptionContainer.AddChild(button);
				}
			}
		}
	}

	/// <summary>
	/// Creates a button for selecting a certain Path
	/// </summary>
	/// <param name="path">The path to select</param>
	/// <returns>The newly created Button</returns>
	private Button CreateButton(IPath path)
	{
		Button button = new Button();
		button.Text = path.label;
		button.Pressed += () => OptionButtonPressed(path);

		return button;
	}

	/// <summary>
	/// Event Handler for pressing an option button
	/// </summary>
	/// <param name="path">The path to select</param>
	private void OptionButtonPressed(IPath path)
	{
		ArcweaveNode.Story.SelectPath(path as Path);
		Repaint();
	}

	/// <summary>
	/// Repaints the current Content and Options
	/// </summary>
	private void Repaint()
	{
		TextContainer.Text = ArcweaveNode.Story.GetCurrentRuntimeContent();
		AddOptions();
	}

	/// <summary>
	/// Refreshes the project.
	/// </summary>
	private void RefreshProject()
	{
		ArcweaveNode.UpdateStory();
	}

	/// <summary>
	/// Signal Handler for Project Updates
	/// </summary>
	private void OnProjectUpdated()
	{
		Repaint();
	}
}
