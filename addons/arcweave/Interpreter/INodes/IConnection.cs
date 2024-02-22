namespace Arcweave.Interpreter.INodes
{
    public interface IConnection
    {
        public string Id { get; }
        public string RawLabel { get; }
        public string RuntimeLabel { get; }

        public INode Source { get; }
        public INode Target { get; }

        public Arcweave.Project.Project Project { get; }

        public void Set(string label, INode source, INode target);

        public void RunLabelScript();

        public Arcweave.Project.Path ResolvePath(Arcweave.Project.Path p);
    }
}
