#if GODOT
using Godot.Collections;
#else
using System.Collections.Generic;
#endif
namespace Arcweave.Interpreter.INodes
{
    public interface IOptions
    {
        public Arcweave.Project.Element Element { get; set; }
        public Array<Arcweave.Project.Path> Paths { get; set; }
        public bool HasPaths => Paths != null;
        public bool HasOptions => HasPaths && ( Paths.Count > 1 || !string.IsNullOrEmpty(Paths[0].label) );
    }

    public interface IPath
    {
        public string label { get; set; }
        public Arcweave.Project.Element TargetElement { get; set; }
        public Array<Arcweave.Project.Connection> _connections { get; set; }
        internal bool IsValid => TargetElement != null;

        internal static IPath Invalid => default(IPath);

        internal void AppendConnection(Arcweave.Project.Connection connection);

        public void ExecuteAppendedConnectionLabels();
    }
}
