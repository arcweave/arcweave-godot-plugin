# Arcweave Godot Plugin

Arcweave Godot Plugin is a plugin for importing Arcweave Projects from [arcweave.com](https://arcweave.com/) in Godot and using them in your Projects.

The Arcweave Godot Exports currently are offered to our Pro and Team Accounts.

The exports consist from two `.gd` files, `data_export.gd` and `state_export.gd`. These contain the data from your project as well as helper functions in order to use [arcscript](https://arcweave.com/docs/1.0/arcscript), Arcweave's scripting language in your Godot Projects.

## Table of Contents

- [Arcweave Godot Plugin](#arcweave-godot-plugin)
  - [Installing the Plugin](#installing-the-plugin)
  - [Getting data from Arcweave](#getting-data-from-arcweave)
    - [Folder Import](#folder-import)
    - [API Import](#api-import)
  - [Creating an ArcweaveAsset](#creating-an-arcweaveasset)
    - [Loading the Project Data](#loading-the-project-data)
    - [Using the ArcweaveAsset](#using-the-arcweaveasset)
      - [Use ArcweaveNode](#use-arcweavenode)
      - [Create your own Node](#create-your-own-node)
  - [Our Implementation](#our-implementation)
  - [Using the Plugin](#using-the-plugin)
    - [C#](#c)
    - [GDScript](#gdscript)
  - [API Documentation](#api-documentation)
    - [Story](#story)
    - [Component](#component)
    - [Element](#element)
    - [Board](#board)

---

## Installing the Plugin

Download the plugin and add the `addons/arcweave` folder in your project's `addons` folder.

Then refresh your project, go to `Project` -> `Project Settings` -> `Plugins` and enable it.

## Getting data from Arcweave

You can import your project in two ways, using our API or by downloading the Godot Export and selecting the exported file

### Folder Import

To use the Folder Import you will have to download the Godot Engine Export for your Arcweave Project.

In your Arcweave Project select the Export Option from the top right corner

![Arcweave Export](docs/images/aw_export.png)

Then Select the Engine Tab and Download the Export file for Godot.

![Arcweave Export Tab](docs/images/aw_export_tab.png)

Save the exported file.

### API Import

Feature available to Team account holders only. You can fetch your Arcweave project's data from within Godot, via Arcweave's web API.

To do this, you will need:

* your **API key** as an Arcweave user.
* your **project's hash**.

[This chapter](https://arcweave.com/docs/1.0/api) in the Arcweave Documentation explains where to find both of them.

## Creating an ArcweaveAsset

Either way, to import your data into Godot, you must create an **ArcweaveAsset** in your Godot project. To do this, right-click on your Godot FileSystem tab, select **New Resource** and find the **ArcweaveAsset** option and pick the file name of your choice. 

### Loading the Project Data

Open the newly created resource's inspector. You will see two options for selecting either a file or use the Arcweave API to retrieve the data.

- **Importing from JSON**: Click on **Select Project File**, find your downloaded export file and select it.
- **Importing from API**: Fill the **API key** and **Project Hash** values.

Then click on **Initialize Arcweave Asset** button. This will either load the project from the file, or fetch the data from the API and store it's info in the Resource.

### Using the ArcweaveAsset

You can use ArcweaveAsset in your own way. Use a separate node for the Arcweave Project, integrate it in your own,
or you can use the **ArcweaveNode** we are providing.

#### Use ArcweaveNode

This plugin creates a new **ArcweaveNode** type, that you can use in your scenes to integrate your Arcweave Project
in your game.

Add an **ArcweaveNode** as a child node in your scene and use the Inspector tab connect it to your resource that you created earlier.

This Node has 3 properties:

- ArcweaveAsset
- Story
- ApiRequest

The **Story** is the class that handles the Arcweave Project. It stores all the info regarding your project and it's
the way we are interacting with it.

**ApiRequest** is an instance of a Node that is being added as a child of ArcweaveNode. It handles the connection to
the Arcweave API for requesting and retrieving the updated Arcweave Project.

#### Create your own Node

You can also create your own nodes and interact with the ArcweaveAsset. 

In order to update during runtime though, you would have to add the node `APIRequest.gd` inside your scene. This will insert an HTTPRequest node in your scene and you will be able to make requests. You can see how we implemented owr own [`ArcweaveNode.cs`](./addons/arcweave/Editor/ArcweaveNode.cs) and follow a similar pattern.

## Our Implementation

Most of our implementation, except ArcweaveAsset and some editor GDScript classes, are written in C#. This means that not all of the API is available in GDScript, because of type compatibility issues. The reason is that the interpreter we are using for **arcscript** is written in C# and uses built in types. 

Using functions with the name pattern `GetGodot*` will retrieve the appropriate instance properties in Godot types but doing this will require copying and typecasting, so the experience might be slower.

We are planning to integrate Godot types in our interpreter in the near future that will speed up this process.

## Using the Plugin

Using Godot's functionality for [Cross-language scripting](https://docs.godotengine.org/en/stable/tutorials/scripting/cross_language_scripting.html) you can use the plugin both from GDScript and C# Godot Projects. The only limitation is using the *.NET*  version of Godot Engine.

In this repo we are providing a simple Demo of both implementations.

### C#

<details>
<summary>How to use the plugin in a C# Godot Project</summary>

<br/>

The main Class that the user should use is the [**Story**](#story) class of the plugin. This has most of the functionalities needed to traverse through a Project. Story is stored as a property inside our ArcweaveNode so our examples are based on that.

To start a Project's Story you have to initialize a Story instance with it's project settings, as well as an instance of APIRequest to be able to update your project on runtime. ArcweaveNode handles that in it's `_Ready` function, so if you are using this Node Type, you don't have to do it.

```csharp

public override void _Ready()
{
    Dictionary projectSettings = (Dictionary)ArcweaveAsset.Get("project_settings");
    Story = new Story(projectSettings);

    var requestScript = GD.Load<GDScript>("res://addons/arcweave/Editor/APIRequestScript.gd");
    ApiRequest = (Node)requestScript.New(ArcweaveAsset);
    AddChild(ApiRequest);
}
```

During the initialization, the data is loaded, the starting_element is set and the Element Options are generated.

If you have a text container you can use the `Story.GetCurrentRuntimeContent()` function to get the text of the current element and set it. In our Demo project, we have a function called `Repaint()` that updates our TextContainer with the new content.

```csharp
private void Repaint()
{
    TextContainer.Text = ArcweaveNode.Story.GetCurrentRuntimeContent();
}
```

You can also use `Story.GenerateCurrentOptions()` function to get the options of the current element:

```csharp
private void Repaint()
{
    TextContainer.Text = ArcweaveNode.Story.GetCurrentRuntimeContent();
    AddOptions()
}

private void AddOptions()
{
    // Empty the previous options
    foreach (var b in OptionContainer.GetChildren())
    {
        OptionContainer.RemoveChild(b);
    }
    // Retrieve the current options
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
```

To select a certain option and continue the story path, we add a signal handler for the button in `CreateButton` that we used earlier, where we select the Path that we provided using `Story.SelectPath()`. When the new path is selected, we call the repaint function to recreate our UI. 

```csharp
private Button CreateButton(IPath path)
{
    Button button = new Button();
    button.Text = path.label;
    button.Pressed += () => OptionButtonPressed(path);

    return button;
}

private void OptionButtonPressed(IPath path)
{
    ArcweaveNode.Story.SelectPath(path as Path);
    Repaint();
}
```
</details>

### GDScript

<details>
<summary>How to use the plugin in a GDScript Godot Project</summary>

<br/>

The main Class that the user should use is the [**Story**](#story) class of the plugin. This has most of the functionalities needed to traverse through a Project. Story is stored as a property inside our ArcweaveNode so our examples are based on that.

To start a Project's Story you have to initialize a Story instance with it's project settings, as well as an instance of APIRequest to be able to update your project on runtime. ArcweaveNode handles that in it's `_ready` function, so if you are using this Node Type, you don't have to do it.

```gdscript
# Your ArcweaveAsset resource file
var arcweave_asset: ArcweaveAsset = preload("res://ArcweaveAsset.tres")
var api_request: APIRequest
var Story = load("res://addons/arcweave/Story.cs")
var story

func _ready():
	api_request = APIRequest.new(arcweave_asset)
	arcweave_asset.project_updated.connect(_on_project_updated)
	add_child(api_request)

	story = Story.new(arcweave_asset.project_settings)

```

During the initialization, the data is loaded, the starting_element is set and the Element Options are generated.

If you have a text container you can use the `story.GetCurrentRuntimeContent()` function to get the text of the current element and set it. In our Demo project, we have a function called `repaint()` that updates our TextContainer with the new content.

```gdscript
func repaint():
	text_container.text = story.GetCurrentRuntimeContent()
```

You can also use `story.GenerateCurrentOptions()` function to get the options of the current element:

```gdscript
func repaint():
	text_container.text = story.GetCurrentRuntimeContent()
	add_options()

func add_options():
	for option in option_container.get_children():
		option_container.remove_child(option)
		option.queue_free()
	
	var options = story.GenerateCurrentOptions()
	var paths = options.GetPaths()
	if paths != null:
		for path in paths:
			if path.IsValid:
				var button : Button = create_button(path)
				option_container.add_child(button)
```

To select a certain option and continue the story path, we add a signal handler for the button in `create_button` that we used earlier, where we select the Path that we provided using `story.SelectPath()`. When the new path is selected, we call the repaint function to recreate our UI. 

```gdscript
func create_button(path):
	var button : Button = Button.new()
	button.text = path.label
	button.pressed.connect(option_button_pressed.bind(path))
	return button

func option_button_pressed(path):
	story.SelectPath(path)
	repaint()
```
</details>

## API Documentation


### Story

The story class provides the following functions

| Function Name                                   | Description                               |
| :---                                            | :---                                      |
| `get_current_element() -> Element`              | Returns the current Element               |
| `set_current_element(id: String)`               | Sets the current element                  |
| `get_current_content() -> String`               | Returns the current element's content     |
| `get_current_options() -> Array`                | Returns the current element's options     |
| `get_element(element_id: String) -> Element`    | Returns an element                        |
| `get_state() -> Dictionary`                     | Returns the current state of the project  |
| `set_state(state: Dictionary)`                  | Sets the current state of the project     |
| `select_option(option)`                         | Select's an option                        |

### Component

Variables

* `var id: String`
* `var name: String`
* `var cover: Dictionary`
* `var attributes: Dictionary`

Functions

| Function Name                                       | Description                                       |
| :---                                                | :---                                              |
| `get_name()`                                        | Returns the name of the Component                 |
| `get_attribute_by_name(name: String) -> Dictionary` | Returns the first attribute with this name        |
| `search_attributes_by_name(name: String) -> Array`  | Returns an array with attributes with this name   |
| `get_cover() -> Dictionary`                         | Returns the cover information for the component   |

### Element

Variables

* `var id: String`
* `var title: String`
* `var theme: String`
* `var outputs: Array`
* `var components: Array`
* `var attributes: Dictionary`
* `var cover: Dictionary`
* `var content_ref`

Functions

| Function Name                                       | Description                                           |
| :---                                                | :---                                                  |
| `get_content(state: Dictionary) -> String`          | Returns the content of the element based on the state |
| `get_cover() -> Dictionary`                         | Returns the cover information for the element         |

### Board

Variables 

* `var id: String`
* `var customId: String`
* `var name: String`
* `var elements: Dictionary`
* `var connections: Dictionary`
* `var notes: Dictionary`
* `var jumpers: Dictionary`
* `var branches: Dictionary`
