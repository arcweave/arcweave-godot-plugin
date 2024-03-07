using Arcweave.Interpreter.INodes;
using Godot;
using Godot.Collections;
using System;
using Arcweave.Project;

namespace Arcweave
{
	public partial class Story : Node
	{
		[Export] private Project.Project Project { get; set; }
		private Dictionary ProjectData { get; set; }
		private Element _currentElement;
		[Export] private Element CurrentElement
		{
			get => _currentElement;
			set
			{
				_currentElement = value;
				_currentElement.Visits++;
				_currentElement.RunContentScript();
				VariableChanges.TrackChanges();
			}
		}

		public VariableChanges VariableChanges;

		public Story() {}
		public Story(Dictionary projectData)
		{
			ProjectData = projectData;
			ProjectMaker projectMaker = new ProjectMaker(projectData);
			Project = projectMaker.MakeProject();
			VariableChanges = new VariableChanges();
			foreach (var variable in Project.Variables)
			{
				VariableChanges.AddVariable(variable);
			}
			
			CurrentElement = Project.StartingElement as Element;
		}

		/// <summary>
		/// Updates the current story instance based on the retreived Project Settings.
		/// All the Project Data except Variables are overwritten.
		/// <br/>
		/// Variables are being merged, holding just the new Variables from <paramref name="projectData"/> and their
		/// values are overwritten to the current project values.
		/// <br/>
		/// The Element visits are transfered to the new elements and the current element script is being rerun with
		/// the previous variable values.
		/// <br/>
		/// If the current element has been removed from <paramref name="projectData"/>, it references the project's
		/// starting element.
		/// </summary>
		/// <param name="projectData">The dictionary containing the new project data</param>
		/// <returns>The story instance</returns>
		public Story UpdateStory(Dictionary projectData)
		{
			// Create the new project
			ProjectData = projectData;
			ProjectMaker projectMaker = new ProjectMaker(projectData);
			var newProject = projectMaker.MakeProject();
			
			// Merge the old project with the new one
			Project = Project.Merge(newProject);
			
			// Reset the latest variable changes
			VariableChanges.UndoChanges(Project, VariableChanges);
			
			// Start tracking the new variables
			VariableChanges = new VariableChanges();
			foreach (var variable in Project.Variables)
			{
				VariableChanges.AddVariable(variable);
			}

			// Set the current element
			var currentElement = Project.ElementWithId(CurrentElement.Id);
			if (currentElement != null)
			{
				_currentElement = currentElement;
				_currentElement.RunContentScript();
				VariableChanges.TrackChanges();
			}
			else
			{
				CurrentElement = Project.StartingElement;
			}
			return this;
		}

		/// <summary>
		/// Resets the Story. This will reset the project variables to the
		/// default values, the element visits to 0 and will set the current
		/// element to the Project's starting element.
		/// </summary>
		public void ResetStory()
		{
			Project.ResetVariables();
			Project.ResetVisits();
			CurrentElement = Project.StartingElement;
		}

		/// <summary>
		/// Sets the current element to the element with the provided ID
		/// </summary>
		/// <param name="id">The element's ID</param>
		public void SetCurrentElementById(string id)
		{
			CurrentElement = Project.ElementWithId(id);
		}

		/// <summary>
		/// Sets the current element to the provided element
		/// </summary>
		/// <param name="element">The element</param>
		public void SetCurrentElement(Element element)
		{
			CurrentElement = element;
		}

		/// <summary>
		/// Generates the options for the current element
		/// </summary>
		/// <returns></returns>
		public Options GenerateCurrentOptions()
		{
			return CurrentElement.GetOptions() as Options;
		}

		/// <summary>
		/// Selects a Path/Option
		/// </summary>
		/// <param name="path">The path to select</param>
		public void SelectPath(Path path)
		{
			
			path.ExecuteAppendedConnectionLabels();
			CurrentElement = path.TargetElement;
		}

		/// <summary>
		/// Returns the project
		/// </summary>
		/// <returns>The Project</returns>
		public Project.Project GetProject()
		{
			return Project;
		}

		/// <summary>
		/// Returns the current element of the story
		/// </summary>
		/// <returns>The current element</returns>
		public Element GetCurrentElement()
		{
			return CurrentElement as Element;
		}

		/// <summary>
		/// Returns the runtime content of the current element
		/// </summary>
		/// <returns>The runtime content</returns>
		public string GetCurrentRuntimeContent()
		{
			return CurrentElement.RuntimeContent;
		}

		/// <summary>
		/// Returns the variable changes that happened from the last path selection.
		/// </summary>
		/// <returns>
		/// A Dictionary where the key is the variable
		/// name and the value is another Dictionary with keys "oldValue" and "newValue"
		/// containing the old and the new variable values respectively.
		/// </returns>
		public Dictionary<string, Dictionary<string, Variant>> GetVariableChanges()
		{
			return VariableChanges.GetChanges();
		}

		/// <summary>
		/// Returns a Save Dictionary including the currentElement ID, the variable values before
		/// the current element changes and the visits (also before the current element visit)
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, Variant> GetSave()
		{
			var visits = new Dictionary<string, int>();
			foreach (var element in Project.Elements.Values)
			{
				visits[element.Id] = element.Visits;
				if (element.Id == _currentElement.Id) visits[element.Id]--;
			}

			var variables = Project.SaveVariables();
			foreach (var variable in variables)
			{
				if (VariableChanges.Changed[variable.Key]) variables[variable.Key] = VariableChanges.OldValues[variable.Key];
			}
			return new Dictionary<string, Variant>()
			{
				{ "currentElement", _currentElement.Id },
				{ "variables", variables },
				{ "visits", visits }
			};
		}

		/// <summary>
		/// Loads the Game Save that was previously created from GetSave()
		/// </summary>
		/// <param name="save"></param>
		public void LoadSave(Dictionary<string, Variant> save)
		{
			Project.ResetVariables();
			Project.LoadVariables(save["variables"].AsGodotDictionary<string, Variant>());
			Project.ResetVisits();
			foreach (var kvp in save["visits"].AsGodotDictionary<string, int>())
			{
				
				if (Project.Elements.TryGetValue(kvp.Key, out var element))
				{
					element.Visits = kvp.Value;
				}
			}

			var newCurrentElement = Project.ElementWithId(save["currentElement"].AsString());
			if (newCurrentElement != null)
			{
				CurrentElement = newCurrentElement;
			}
			else
			{
				_currentElement = Project.StartingElement;
			}
		}
	}

	public partial class VariableChanges : GodotObject
	{
		[Export] public Array<Variable> Variables = new();
		[Export] public Dictionary<string, Variant> OldValues = new();
		[Export] public Dictionary<string, Variant> NewValues = new();
		[Export] public Dictionary<string, bool> Changed = new();

		public void AddVariable(Variable variable)
		{
			Variables.Add(variable);
			OldValues[variable.Name] = variable.Value;
			NewValues[variable.Name] = variable.Value;
			Changed[variable.Name] = false;
		}

		internal void TrackChanges()
		{
			foreach (var variable in Variables)
			{
				Changed[variable.Name] = variable.Changed;
				if (variable.Changed)
				{
					OldValues[variable.Name] = NewValues[variable.Name];
					NewValues[variable.Name] = variable.Value;
					Changed[variable.Name] = true;
					variable.Changed = false;
				}
			}
		}

		public Dictionary<string, Dictionary<string, Variant>> GetChanges()
		{
			var changes = new Dictionary<string, Dictionary<string, Variant>>();
			foreach (var variable in Variables)
			{
				if (Changed[variable.Name])
				{
					changes[variable.Name] = new Dictionary<string, Variant>
					{
						{ "oldValue", OldValues[variable.Name] },
						{ "newValue", NewValues[variable.Name] }
					};
				}
			}

			return changes;
		}

		public static void UndoChanges(Project.Project project, VariableChanges changes)
		{
			var varChanges = changes.GetChanges();
			foreach (var variableName in varChanges.Keys)
			{
				project.SetVariable(variableName, varChanges[variableName]["oldValue"]);
			}
		}
	}
}
