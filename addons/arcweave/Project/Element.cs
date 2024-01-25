using System.Collections.Generic;
using Arcweave.Interpreter;
using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Element : GodotObject, IElement
    {
        public int Visits { get; set; }

        public string Id { get; private set; }

        public Project Project { get; private set; }
        
        public List<IConnection> Outputs { get; private set; }

        public List<IAttribute> Attributes { get; private set; }

        public string Title { get; private set; }
        public string Content { get; private set; }

        public Element(string id, string title, string content, Project project, List<IConnection> outputs, List<IAttribute> attributes)
        {
            Visits = 0;
            Id = id;
            Title = title;
            Content = content;
            Project = project;
            Outputs = outputs;
            Attributes = attributes;
        }

        public void AddOutput(Connection connection)
        {
            Outputs.Add(connection);
        }

        public void AddAttribute(IAttribute attribute) {  Attributes.Add(attribute); }

        public string GetRuntimeContent()
        {
            AwInterpreter i = new AwInterpreter(Project, Id);
            var output = i.RunScript(Content);
            // TODO: figure out variable changes
            return output.Output;
        }

        IPath INode.ResolvePath(IPath path)
        {
            if (string.IsNullOrEmpty(path.label)) { path.label = Title; }
            path.TargetElement = this;
            return path;
        }

        public IOptions GetOptions()
        {
            return new Options(this);
        }
    }
}
