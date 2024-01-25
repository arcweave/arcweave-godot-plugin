using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Jumper : GodotObject, INode
    {
        public string Id { get; private set; }

        public Project Project { get; private set; }

        private IElement Target { get; }

        public Jumper(string id, Project project, IElement target)
        {
            Id = id;
            Project = project;
            Target = target;
        }

        IPath INode.ResolvePath(IPath path)
        {
            return Target?.ResolvePath(path) ?? IPath.Invalid;
        }
    }
}
