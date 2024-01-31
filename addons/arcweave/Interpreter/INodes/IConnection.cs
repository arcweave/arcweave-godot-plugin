namespace Arcweave.Interpreter.INodes
{
    public interface IConnection
    {
        public string Id { get; }
        public string Label { get; }

        public INode Source { get; }
        public INode Target { get; }

        public Arcweave.Project.Project Project { get; }

        public void Set(string label, INode source, INode target);

        public string GetRuntimeLabel();

        public IPath ResolvePath(IPath p);
    }
}
