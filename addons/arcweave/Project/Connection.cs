using System;
using Arcweave.Interpreter;
using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Connection
    {
        [Export] public string Id { get; private set; }
        [Export] public string RawLabel { get; private set; }
        [Export] public string RuntimeLabel { get; private set; }
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

        public void Set(string rawLabel, INode source, INode target)
        {
            RawLabel = rawLabel;
            Source = source;
            Target = target;
        }

        public void RunLabelScript()
        {
            if (string.IsNullOrEmpty(RawLabel))
            {
                RuntimeLabel = null;
                return;
            }

            AwInterpreter i = new AwInterpreter(Project);
            var output = i.RunScript(RawLabel);
            if ( output.Changes.Count > 0 ) {
                foreach ( var change in output.Changes ) {
                    Project.SetVariable(change.Key, change.Value);
                }
            }

            RuntimeLabel = output.Output;
        }

        public Path ResolvePath(Path p)
        {
            p.AppendConnection(this);
            RunLabelScript();
            p.label = RuntimeLabel;
            return Target.ResolvePath(p);

        }

        void IConnection.Set(string label, INode source, INode target)
        {
            throw new NotImplementedException();
        }
    }
}
