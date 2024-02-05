using System.Linq;
using Godot.Collections;
using Arcweave.Interpreter;
using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Element
    {
        [Export] public int Visits { get; set; }

        [Export] public string Id { get; private set; }

        [Export] public Project Project { get; private set; }
        
        [Export] public Array<Connection> Outputs { get; private set; }

        [Export] public Array<Attribute> Attributes { get; private set; }
        [Export] public Array<Component> Components { get; private set; }
        [Export] public string Title { get; private set; }
        [Export] public string Content { get; private set; }

        public Element(string id, string title, string content, Project project, Array<Connection> outputs, Array<Component> components, Array<Attribute> attributes)
        {
            Visits = 0;
            Id = id;
            Title = title;
            Content = content;
            Project = project;
            Outputs = outputs;
            Components = components;
            Attributes = attributes;
        }

        public void AddOutput(Connection connection)
        {
            Outputs.Add(connection);
        }

        public void AddAttribute(Attribute attribute) {  Attributes.Add(attribute); }

        public string GetRuntimeContent()
        {
            AwInterpreter i = new AwInterpreter(Project, Id);
            var output = i.RunScript(Content);
            if ( output.Changes.Count > 0 ) {
                foreach ( var change in output.Changes ) {
                    Project.SetVariable(change.Key, change.Value);
                }
            }
            return output.Output;
        }

        Path INode.ResolvePath(Path path)
        {
            if (string.IsNullOrEmpty(path.label)) { path.label = Title; }
            path.TargetElement = this;
            return path;
        }

        public Options GetOptions()
        {
            return new Options(this);
        }

        public Component GetComponent(string componentName)
        {
            return Components.First(component => component.Name == componentName);
        }

        public Attribute GetAttribute(string attributeName)
        {
            return Attributes.First(attribute => attribute.Name == attributeName);
        }
    }
}
