using Godot;
using Godot.Collections;
using Arcweave.Interpreter.INodes;

namespace Arcweave.Project
{
    public partial class Options
    {
        [Export] public Element Element { get; set; }
        [Export] public Array<Path> Paths { get; set; }
        public bool HasPaths => Paths != null;
        public bool HasOptions => HasPaths && ( Paths.Count > 1 || !string.IsNullOrEmpty(Paths[0].label) );

        public Options(Element element)
        {
            Element = element;
            var validPaths = new Array<Path>();
            foreach (var output in element.Outputs)
            {
                var path = output.ResolvePath(new Path());
                if (path.IsValid) { validPaths.Add(path); }
            }
            Paths = validPaths.Count > 0 ? validPaths : null;

            if (Paths == null || Paths.Count != 1) return;
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

    public partial class Path
    {
        [Export] public string label { get; set; }
        [Export] public Element TargetElement { get; set; }
        [Export] public Array<Connection> _connections { get; set; }

        internal bool IsValid => TargetElement != null;

        internal static Path Invalid => default(Path);

        public void AppendConnection(Connection connection)
        {
            if (_connections == null) { _connections = new Array<Connection>(); }
            _connections.Add(connection);
        }

        public void ExecuteAppendedConnectionLabels()
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
