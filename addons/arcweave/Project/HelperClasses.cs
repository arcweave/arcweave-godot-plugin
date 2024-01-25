using Godot;
using System.Collections.Generic;
using Arcweave.Interpreter.INodes;

namespace Arcweave.Project
{
    public partial class Options : GodotObject, IOptions
    {
        public IElement Element { get; set; }
        public IPath[] Paths { get; set; }
        public bool HasPaths => Paths != null;
        public bool HasOptions => HasPaths && ( Paths.Length > 1 || !string.IsNullOrEmpty(Paths[0].label) );

        public Options(IElement element)
        {
            Element = element;
            var validPaths = new List<IPath>();
            foreach (var output in element.Outputs)
            {
                var path = output.ResolvePath(new Path());
                if (path.IsValid) { validPaths.Add(path); }
            }
            Paths = validPaths.Count > 0 ? validPaths.ToArray() : null;

            if (Paths == null || Paths.Length != 1) return;
            if (Paths[0].label == Paths[0].TargetElement.Title)
            {
                Paths[0].label = null;
            }
        }

        public Godot.Collections.Array<Path> GetPaths()
        {
            if (Paths == null)
            {
                return null;
            } 
            var paths = new Godot.Collections.Array<Path>();
            foreach (var path in Paths)
            {
                paths.Add(path as Path);
            }
            return paths;
        }
    }

    public partial class Path : GodotObject, IPath
    {
        public string label { get; set; }
        public IElement TargetElement { get; set; }
        public List<IConnection> _connections { get; set; }

        internal bool IsValid => TargetElement != null;

        internal static Path Invalid => default(Path);

        public void AppendConnection(IConnection connection)
        {
            if (_connections == null) { _connections = new List<IConnection>(); }
            _connections.Add(connection);
        }

        public void  ExecuteAppendedConnectionLabels()
        {
            foreach  ( var connection in _connections)
            {
                var runtimeLabel = connection.GetRuntimeLabel();
                GD.Print(TargetElement.GetRuntimeContent());
                GD.Print(runtimeLabel);
            }
        }
    }
}
