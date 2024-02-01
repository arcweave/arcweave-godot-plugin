using System;
using Arcweave.Interpreter;
using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Connection
    {
        [Export] public string Id { get; private set; }
        [Export] public string Label { get; private set; }
        public INode Source { get; private set; }
        public INode Target { get; private set; }

        [Export] public Project Project
        {
            get { return Source.Project; }
            set {}
        }

        public Connection(string id)
        {
            Id = id;
        }

        public void Set(string label, INode source, INode target)
        {
            Label = label;
            Source = source;
            Target = target;
        }

        public string GetRuntimeLabel()
        {
            if (string.IsNullOrEmpty(Label))
            {
                return null;
            }

            AwInterpreter i = new AwInterpreter(Project);
            var output = i.RunScript(Label);
            return output.Output;
        }

        public Path ResolvePath(Path p)
        {
            p.AppendConnection(this);
            p.label = GetRuntimeLabel();
            return Target.ResolvePath(p);

        }

        void IConnection.Set(string label, INode source, INode target)
        {
            throw new NotImplementedException();
        }
    }
}
