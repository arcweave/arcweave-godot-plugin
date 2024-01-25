using System.Collections.Generic;

namespace Arcweave.Interpreter.INodes
{
    public interface IOptions
    {
        public IElement Element { get; set; }
        public IPath[] Paths { get; set; }
        public bool HasPaths => Paths != null;
        public bool HasOptions => HasPaths && ( Paths.Length > 1 || !string.IsNullOrEmpty(Paths[0].label) );
    }

    public interface IPath
    {
        public string label { get; set; }
        public IElement TargetElement { get; set; }
        public List<IConnection> _connections { get; set; }
        internal bool IsValid => TargetElement != null;

        internal static IPath Invalid => default(IPath);

        internal void AppendConnection(IConnection connection);

        public void ExecuteAppendedConnectionLabels();
    }
}
