namespace Arcweave.Interpreter.INodes
{
    public partial interface INode
    {
        string Id { get; }
        Arcweave.Project.Project Project { get; }

        Arcweave.Project.Path ResolvePath(Arcweave.Project.Path path);
    }
}
