using Arcweave.Interpreter.INodes;
using Godot;
using Godot.Collections;

namespace Arcweave.Project
{
    public partial class Branch : GodotObject, INode
    {
        public string Id { get; private set; }

        public Project Project { get; private set; }

        public Array<Condition> Conditions { get; private set; }

        public Branch(string id, Project project, Array<Condition> conditions)
        {
            Id = id;
            Project = project;
            Conditions = conditions;
        }

        Condition GetTrueCondition()
        {
            foreach (Condition condition in Conditions)
            {
                if (condition.Evaluate()) return condition;
            }
            return null;
        }

        IPath INode.ResolvePath(IPath path)
        {
            Condition condition = GetTrueCondition();
            return condition != null ? (condition as INode).ResolvePath(path) : IPath.Invalid;
        }
    }
}
