namespace Arcweave.Interpreter.INodes
{
    public interface INode
    {
        string Id { get; }
        Project.Project Project { get; }

        IPath ResolvePath(IPath path);
    }
}
