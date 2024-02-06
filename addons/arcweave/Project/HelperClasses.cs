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
                var save = Element.Project.SaveVariables();
                var path = output.ResolvePath(new Path());
                if (path != null && path.IsValid) { validPaths.Add(path); }
                Element.Project.LoadVariables(save);
            }
            Paths = validPaths.Count > 0 ? validPaths : null;
            if (Paths == null || Paths.Count != 1) return;
            if (Paths[0].label == Paths[0].TargetElement.Title)
            {
                Paths[0].label = null;
            }
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
                connection.RunLabelScript();
            }
        }
    }
}
