using Godot.Collections;
using System.Linq;
using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Project
    {
        [Export]
        public string Name {  get; private set; }
        [Export]
        public Dictionary<string, Board> Boards { get; private set; }
        [Export]
        public Dictionary<string, Component> Components { get; private set; }
        [Export]
        public Dictionary<string, Variable> Variables { get; private set; }
        [Export]
        public Dictionary<string, Element> Elements { get; private set; }
        [Export]
        public Element StartingElement { get; private set; }
        public Element ElementWithId(string id)
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

        public Project(string name, Element startingElement, Dictionary<string, Board> boards, Dictionary<string, Component> components, Dictionary<string, Variable> variables, Dictionary<string, Element> elements)
        {
            Name = name;
            Boards = boards;
            Components = components;
            Variables = variables;
            Elements = elements;
            StartingElement = startingElement;
        }

        public void Set(Element startingElement, Dictionary<string, Board> boards, Dictionary<string, Component> components, Dictionary<string, Variable> variables, Dictionary<string, Element> elements)
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
