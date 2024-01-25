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
		private Project.Project project { get; set; }
		private Dictionary ProjectData { get; set; }
		private IElement CurrentElement { get; set; }

		public Story() {}
		public Story(Dictionary projectData)
		{
			ProjectData = projectData;
			ProjectMaker projectMaker = new ProjectMaker(projectData);
			project = projectMaker.MakeProject();

			CurrentElement = project.StartingElement as Element;
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

			project = project.Merge(newProject);
			var currentElement = project.ElementWithId(CurrentElement.Id);
			if (currentElement != null)
			{
				CurrentElement = currentElement;
			}
			else
			{
				CurrentElement = project.StartingElement;
			}
			return this;
		}

		/// <summary>
		/// Sets the current element to the element with the provided ID
		/// </summary>
		/// <param name="id">The element's ID</param>
		public void SetCurrentElement(string id)
		{
			CurrentElement = project.ElementWithId(id);
		}

		/// <summary>
		/// Sets the current element to the provided element
		/// </summary>
		/// <param name="element">The element</param>
		public void SetCurrentElement(IElement element)
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
			CurrentElement.Visits++;
		}

		public Project.Project GetProject()
		{
			return project;
		}

		public Element GetCurrentElement()
		{
			return CurrentElement as Element;
		}

		public string GetCurrentRuntimeContent()
		{
			return CurrentElement.GetRuntimeContent();
		}
	}
}
