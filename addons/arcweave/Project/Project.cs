using System.Collections.Generic;
using System.Linq;
using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Project : GodotObject, IProject
    {
        public string Name {  get; private set; }
        public Dictionary<string, IBoard> Boards { get; private set; }
        public Dictionary<string, IComponent> Components { get; private set; }
        public Dictionary<string, IVariable> Variables { get; private set; }
        public Dictionary<string, IElement> Elements { get; private set; }
        public IElement StartingElement { get; private set; }
        public IElement ElementWithId(string id)
        {
            return Elements?[id];
        }

        public object GetVariable(string name)
        {
            return Variables.Values.First(x => x.Name == name).Value;
        }
        
        public Project() : this("") {}

        public Project(string name)
        {
            Name = name;
        }

        public Project(string name, IElement startingElement, Dictionary<string, IBoard> boards, Dictionary<string, IComponent> components, Dictionary<string, IVariable> variables, Dictionary<string, IElement> elements)
        {
            Name = name;
            Boards = boards;
            Components = components;
            Variables = variables;
            Elements = elements;
            StartingElement = startingElement;
        }

        public void Set(IElement startingElement, Dictionary<string, IBoard> boards, Dictionary<string, IComponent> components, Dictionary<string, IVariable> variables, Dictionary<string, IElement> elements)
        {
            Boards = boards;
            Components = components;
            Variables = variables;
            Elements = elements;
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
    }
}
