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
        [Export] public Asset Cover { get; private set; }
        [Export] public Array<Connection> Outputs { get; private set; }

        [Export] public Array<Attribute> Attributes { get; private set; }
        [Export] public Array<Component> Components { get; private set; }
        [Export] public string Title { get; private set; }
        [Export] public string RawContent { get; private set; }
        [Export] public string RuntimeContent { get; private set; }
        public Element(string id, string title, string rawContent, Project project, Array<Connection> outputs, Array<Component> components, Array<Attribute> attributes, Asset cover)
        {
            Visits = 0;
            Id = id;
            Title = title;
            RawContent = rawContent;
            Project = project;
            Outputs = outputs;
            Components = components;
            Attributes = attributes;
            Cover = cover;
        }

        public void AddOutput(Connection connection)
        {
            Outputs.Add(connection);
        }

        public void AddAttribute(Attribute attribute) {  Attributes.Add(attribute); }

        public void RunContentScript()
        {
            AwInterpreter i = new AwInterpreter(Project, Id);
            var output = i.RunScript(RawContent);
            if ( output.Changes.Count > 0 ) {
                foreach ( var change in output.Changes ) {
                    Project.SetVariable(change.Key, change.Value);
                }
            }

            RuntimeContent = output.Output;
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
            try
            {
                return Components.First(component => component.Name == componentName);
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }
        }

        public Attribute GetAttribute(string attributeName)
        {
            try
            {
                return Attributes.First(attribute => attribute.Name == attributeName);
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }
        }
    }
}
