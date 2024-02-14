using System;
using Godot.Collections;
using System.Linq;
using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Project
    {
        [Export] public string Name {  get; private set; }
        [Export] public Dictionary<string, Board> Boards { get; private set; }
        [Export] public Dictionary<string, Component> Components { get; private set; }
        [Export] public Dictionary<string, Variable> Variables { get; private set; }
        [Export] public Dictionary<string, Element> Elements { get; private set; }
        [Export] public Dictionary<string, Asset> Assets { get; private set; }
        [Export] public Element StartingElement { get; private set; }
        
        public Project() : this("") {}

        public Project(string name)
        {
            Name = name;
        }

        public Project(string name, Element startingElement, Dictionary<string, Board> boards, Dictionary<string, Component> components, Dictionary<string, Variable> variables, Dictionary<string, Element> elements)
        {
            Name = name;
            Boards = boards;
            Components = components;
            Variables = variables;
            Elements = elements;
            StartingElement = startingElement;
        }
        
        /// <summary>
        /// Returns an element based on it's ID
        /// </summary>
        /// <param name="id">The element ID</param>
        /// <returns>The element or null if not found</returns>
        public Element ElementWithId(string id)
        {
            if (Elements.TryGetValue(id, out var element))
            {
                return element;
            }
            return null;
        }

        /// <summary>
        /// Resets the project's variables to their default values.
        /// </summary>
        public void ResetVariables()
        {
            foreach (var variable in Variables.Values)
            {
                variable.ResetToDefaultValue();
            }
        }

        /// <summary>
        /// Resets the project's element visits to 0.
        /// </summary>
        public void ResetVisits()
        {
            foreach (var element in Elements.Values)
            {
                element.Visits = 0;
            }
        }

        /// <summary>
        /// Returns a Variable based on it's name
        /// </summary>
        /// <param name="name">The variable name</param>
        /// <returns>The variable of null if not found</returns>
        public Variable GetVariable(string name)
        {
            try
            {
                return Variables.Values.First(x => x.Name == name);
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the Project's values.
        /// </summary>
        /// <param name="startingElement">The project starting element</param>
        /// <param name="boards">The project's boards</param>
        /// <param name="components">The project's components</param>
        /// <param name="variables">The project's variables</param>
        /// <param name="elements">The project's elements</param>
        /// <param name="assets">The project's assets</param>
        public void Set(Element startingElement, Dictionary<string, Board> boards, Dictionary<string, Component> components, Dictionary<string, Variable> variables, Dictionary<string, Element> elements, Dictionary<string, Asset> assets)
        {
            Boards = boards;
            Components = components;
            Variables = variables;
            Elements = elements;
            Assets = assets;
            StartingElement = startingElement;
        }

        /// <summary>
        /// Merges the current project to the project provided.
        /// Overwrites the values of the variables with the current values
        /// and the element visits with the current visits.
        /// </summary>
        /// <param name="project">The project to merge</param>
        /// <returns>The merged project</returns>
        public Project Merge(Project project)
        {
            // Set the old variable values to the new project
            foreach (var variableId in Variables.Keys)
            {
                if (project.Variables.ContainsKey(variableId))
                {
                    if (project.Variables[variableId].Type == Variables[variableId].Type)
                    {
                        project.Variables[variableId].Value = Variables[variableId].Value;
                    }
                }
            }

            // Set the old element visits in the new project
            foreach (var elementId in Elements.Keys)
            {
                if (project.Elements.TryGetValue(elementId, out var element))
                {
                    element.Visits = Elements[elementId].Visits;
                }
            }
            
            return project;
        }

        /// <summary>
        /// Returns the project Boards
        /// </summary>
        /// <returns>The project's Boards</returns>
        public Dictionary<string, Board> GetBoards()
        {
            return Boards;
        }
        
        /// <summary>
        /// Sets the variable with name to a new value.
        /// Returns if variable exists in the first place.
        /// </summary>
        /// <param name="name">The variable name</param>
        /// <param name="value">The new value</param>
        /// <returns>True if the variable is set, False if the variable doesn't exist</returns>
        public bool SetVariable(string name, object value)
        {
            try
            {
                Variable variable = Variables.Values.First(x => x.Name == name);
                if (value is Variant)
                {
                    variable.Value = (Variant)value;
                }
                else
                {
                    switch (Type.GetTypeCode(value.GetType()))
                    {
                        case TypeCode.String:
                            variable.Value = (string)value;
                            break;
                        case TypeCode.Boolean:
                            variable.Value = (bool)value;
                            break;
                        case TypeCode.Int32:
                            variable.Value = (int)value;
                            break;
                        case TypeCode.Double:
                            variable.Value = (double)value;
                            break;
                        default:
                            variable.Value = default;
                            break;
                    }
                }
                
            }
            catch (System.InvalidOperationException)
            {
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// Returns a dictionary of the saved variables that can be loaded later.
        /// </summary>
        /// <returns>A dictionary with key the variable name and value the variable value</returns>
        public Dictionary<string, Variant> SaveVariables() {
            var save = new Dictionary<string, Variant>();
            foreach ( var entry in Variables )
            {
                save[entry.Key] = entry.Value.Value;
            }
            return save;
        }

        /// <summary>
        /// Loads a previously saved string made with SaveVariables.
        /// </summary>
        /// <param name="save">The previously saved variables</param>
        public void LoadVariables(Dictionary<string, Variant> save) {
            foreach (var entry in save)
            {
                Variables[entry.Key].Value = entry.Value;
                Variables[entry.Key].Changed = false;
            }
        }
    }
}
