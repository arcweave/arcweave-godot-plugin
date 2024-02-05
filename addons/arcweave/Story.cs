using Arcweave.Interpreter.INodes;
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
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
			}
		}

		public Story() {}
		public Story(Dictionary projectData)
		{
			ProjectData = projectData;
			ProjectMaker projectMaker = new ProjectMaker(projectData);
			Project = projectMaker.MakeProject();

			CurrentElement = Project.StartingElement as Element;
		}

		/// <summary>
		/// Updates the current story instance based on the retreived Project Settings.
		/// All the Project Data except Variables are overwritten.
		/// Variables are being merged, holding just the new Variables from <paramref name="projectData"/> and their
		/// values are overwritten to the current project values. 
		/// If the current element has been removed from <paramref name="projectData"/>, it references the project's
		/// starting element.
		/// </summary>
		/// <param name="projectData"></param>
		/// <returns></returns>
		public Story UpdateStory(Dictionary projectData)
		{
			ProjectData = projectData;
			ProjectMaker projectMaker = new ProjectMaker(projectData);
			var newProject = projectMaker.MakeProject();

			Project = Project.Merge(newProject);
			var currentElement = Project.ElementWithId(CurrentElement.Id);
			if (currentElement != null)
			{
				CurrentElement = currentElement;
			}
			else
			{
				CurrentElement = Project.StartingElement;
			}
			return this;
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

		public Project.Project GetProject()
		{
			return Project;
		}

		public Element GetCurrentElement()
		{
			return CurrentElement as Element;
		}

		public string GetCurrentRuntimeContent()
		{
			return CurrentElement.RuntimeContent;
		}
	}
}
