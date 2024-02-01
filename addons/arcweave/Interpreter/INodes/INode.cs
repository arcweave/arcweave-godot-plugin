namespace Arcweave.Interpreter.INodes
{
    public interface INode
    {
        string Id { get; }
        Arcweave.Project.Project Project { get; }

        Arcweave.Project.Path ResolvePath(Arcweave.Project.Path path);
    }
}
