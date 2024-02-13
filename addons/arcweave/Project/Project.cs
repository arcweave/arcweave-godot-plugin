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
        public Element ElementWithId(string id)
        {
            return Elements?[id];
        }

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

        public void Set(Element startingElement, Dictionary<string, Board> boards, Dictionary<string, Component> components, Dictionary<string, Variable> variables, Dictionary<string, Element> elements, Dictionary<string, Asset> assets)
        {
            Boards = boards;
            Components = components;
            Variables = variables;
            Elements = elements;
            Assets = assets;
            StartingElement = startingElement;
        }

        public Project Merge(Project project)
        {
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
            
            return project;
        }

        public Godot.Collections.Dictionary<string, Board> GetBoards()
        {
            var boards = new Godot.Collections.Dictionary<string, Board>();
            foreach (var entry in Boards)
            {
                boards[entry.Key] = Boards[entry.Key] as Board;
            }
            return boards;
        }
        
        ///<summary>Sets the variable with name to a new value. Returns if variable exists in the first place.</summary>
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
        
        ///<summary>Returns a string of the saved variables that can be loaded later.</summary>
        public Dictionary<string, Variant> SaveVariables() {
            var save = new Dictionary<string, Variant>();
            foreach ( var entry in Variables )
            {
                save[entry.Key] = entry.Value.Value;
            }
            return save;
        }

        ///<summary>Loads a previously saved string made with SaveVariables.</summary>
        public void LoadVariables(Dictionary<string, Variant> save) {
            foreach (var entry in save)
            {
                Variables[entry.Key].Value = entry.Value;
                Variables[entry.Key].Changed = false;
            }
        }
    }
}
