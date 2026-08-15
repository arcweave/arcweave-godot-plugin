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
        [Export] public Array<Variable> Variables { get; private set; }
        [Export] public Dictionary<string, Element> Elements { get; private set; }
        [Export] public Dictionary<string, Asset> Assets { get; private set; }
        [Export] public Element StartingElement { get; private set; }
        
        public Project() : this("") {}

        public Project(string name)
        {
            Name = name;
        }

        public Project(string name, Element startingElement, Dictionary<string, Board> boards, Dictionary<string, Component> components, Array<Variable> variables, Dictionary<string, Element> elements)
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
            foreach (var variable in GetAllVariables())
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
        /// Returns a global variable by name, or a scoped variable when an owner custom ID is provided.
        /// </summary>
        /// <param name="name">The variable name</param>
        /// <returns>The variable of null if not found</returns>
        public Variable GetVariable(string name, string scope = null)
        {
            if (scope == null)
            {
                return Variables.FirstOrDefault(variable => variable.Name == name);
            }

            var container = Boards.Values.Cast<IHasVariables>()
                .Concat(Components.Values)
                .FirstOrDefault(candidate => candidate.CustomId == scope);
            return container?.Variables.FirstOrDefault(variable => variable.Name == name);
        }

        public Variable GetVariableById(string id)
        {
            return GetAllVariables().FirstOrDefault(variable => variable.Id == id);
        }

        public System.Collections.Generic.IEnumerable<Variable> GetAllVariables()
        {
            var globals = Variables ?? new Array<Variable>();
            var boardVariables = Boards == null
                ? Enumerable.Empty<Variable>()
                : Boards.Values.SelectMany(board => board.Variables ?? new Array<Variable>());
            var componentVariables = Components == null
                ? Enumerable.Empty<Variable>()
                : Components.Values.SelectMany(component => component.Variables ?? new Array<Variable>());
            return globals.Concat(boardVariables).Concat(componentVariables);
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
        public void Set(Element startingElement, Dictionary<string, Board> boards, Dictionary<string, Component> components, Array<Variable> variables, Dictionary<string, Element> elements, Dictionary<string, Asset> assets)
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
            foreach (var variable in GetAllVariables())
            {
                var projVariable = project.GetVariableById(variable.Id);
                if (projVariable != null)
                {
                    if (projVariable.Type == variable.Type)
                    {
                        projVariable.Value = variable.Value;
                        projVariable.Changed = false;
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
            return SetVariableValue(GetVariable(name), value);
        }

        public bool SetScopedVariable(string name, string scope, object value)
        {
            return SetVariableValue(GetVariable(name, scope), value);
        }

        public bool SetVariableById(string id, object value)
        {
            return SetVariableValue(GetVariableById(id), value);
        }

        private static bool SetVariableValue(Variable variable, object value)
        {
            if (variable == null || value == null) return false;

            if (value is Variant variant)
            {
                if (variant.VariantType == Variant.Type.Nil) return false;
                variable.Value = variant;
                return true;
            }

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
                case TypeCode.Int64:
                    variable.Value = (long)value;
                    break;
                case TypeCode.Single:
                    variable.Value = (float)value;
                    break;
                case TypeCode.Double:
                    variable.Value = (double)value;
                    break;
                default:
                    return false;
            }

            return true;
        }
        
        /// <summary>
        /// Returns a dictionary of the saved variables that can be loaded later.
        /// </summary>
        /// <returns>A dictionary with stable variable IDs as keys and current values as values.</returns>
        public Dictionary<string, Variant> SaveVariables() {
            var save = new Dictionary<string, Variant>();
            foreach ( var variable in GetAllVariables() )
            {
                save[variable.Id] = variable.Value;
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
                var variable = GetVariableById(entry.Key) ??
                               Variables.FirstOrDefault(global => global.Name == entry.Key);
                if (variable != null)
                {
                    variable.Value = entry.Value;
                    variable.Changed = false;
                }
            }
        }
    }
}
