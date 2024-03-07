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
	public MenuButton MenuButton;
	public Button RefreshButton;
	public Button RestartButton;
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
		MenuButton = GetNode<MenuButton>("MenuButton");
		MenuButton.Pressed += OnMenuButtonPressed;
		var popup = MenuButton.GetPopup();
		popup.AddItem("Save Game", 0);
		popup.AddItem("Load Game", 1);
		popup.IdPressed += MenuItemPressed;
		
		RefreshButton = GetNode<Button>("StoryContainer/UIButtonsContainer/RefreshButton");
		RefreshButton.Pressed += RefreshProject;

		RestartButton = GetNode<Button>("StoryContainer/UIButtonsContainer/RestartButton");
		RestartButton.Pressed += RestartProject;
		
		TextContainer.BbcodeEnabled = true;

		Repaint();
	}

	private void OnMenuButtonPressed()
	{
		var popup = MenuButton.GetPopup();
		popup.SetItemDisabled(1, !FileAccess.FileExists("user://savegame.save"));
		popup.Position = new Vector2I(25, 460);
	}

	private void MenuItemPressed(long id)
	{
		if (id == 0)
		{
			SaveGame();
		} else if (id == 1)
		{
			LoadGame();
		}
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
	/// Refreshes the project.
	/// </summary>
	private void RestartProject()
	{
		ArcweaveNode.Story.ResetStory();
		Repaint();
	}

	/// <summary>
	/// Signal Handler for Project Updates
	/// </summary>
	private void OnProjectUpdated()
	{
		Repaint();
	}

	private void SaveGame()
	{
		using var saveGame = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Write);

		var save = ArcweaveNode.Story.GetSave();
		saveGame.StoreVar(save);
	}
	
	private void LoadGame()
	{
		using var saveGame = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Read);

		var save = saveGame.GetVar().AsGodotDictionary<string, Variant>();
		ArcweaveNode.Story.LoadSave(save);
		Repaint();
	}
}
