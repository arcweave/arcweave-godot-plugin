using Godot;
using Godot.Collections;
using Arcweave.Interpreter.INodes;

namespace Arcweave.Project
{
    /// <summary>
    /// This class contains the options of an element. An option is
    /// a Path of multiple connections to a target element.
    /// </summary>
    public partial class Options
    {
        /// <summary>
        /// The element for which the options are created
        /// </summary>
        [Export] public Element Element { get; set; }
        /// <summary>
        /// The Paths of the element
        /// </summary>
        [Export] public Array<Path> Paths { get; set; }
        public bool HasPaths => Paths != null;
        public bool HasOptions => HasPaths && ( Paths.Count > 1 || !string.IsNullOrEmpty(Paths[0].label) );

        /// <summary>
        /// Generates the options of the element provided. Before each option
        /// the variables are being saved and restored to have the changes of
        /// a connection script be taken into consideration from any consequent
        /// connections.
        /// </summary>
        /// <param name="element">The element for which to create the options</param>
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

    /// <summary>
    /// A Path is a collection of multiple connections to a target element
    /// </summary>
    public partial class Path
    {
        [Export] public string label { get; set; }
        [Export] public Element TargetElement { get; set; }
        [Export] public Array<Connection> _connections { get; set; }

        internal bool IsValid => TargetElement != null;

        internal static Path Invalid => default(Path);

        /// <summary>
        /// Appends a connection to the Path
        /// </summary>
        /// <param name="connection">The connection to add</param>
        public void AppendConnection(Connection connection)
        {
            if (_connections == null) { _connections = new Array<Connection>(); }
            _connections.Add(connection);
        }

        /// <summary>
        /// Executes the labels of all the connections in this Path
        /// </summary>
        public void ExecuteAppendedConnectionLabels()
        {
            foreach  ( var connection in _connections)
            {
                connection.RunLabelScript();
            }
        }
    }
}
