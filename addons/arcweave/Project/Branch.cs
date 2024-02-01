using Arcweave.Interpreter.INodes;
using Godot;
using Godot.Collections;

namespace Arcweave.Project
{
    public partial class Branch : GodotObject, INode
    {
        [Export] public string Id { get; private set; }

        [Export] public Project Project { get; private set; }

        [Export] public Array<Condition> Conditions { get; private set; }

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

        Path INode.ResolvePath(Path path)
        {
            Condition condition = GetTrueCondition();
            return condition != null ? (condition as INode).ResolvePath(path) : Path.Invalid;
        }
    }
}
