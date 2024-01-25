using Arcweave.Interpreter;
using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Condition : GodotObject, INode
    {
        public string Id { get; set; }
        // If null or empty, it's an "else" condition
        public string Script { get; private set; }
        public Project Project { get; private set; }
        public Connection Output { get; private set; }

        public Condition(string id, Project project, string script, Connection output)
        {
            Id = id;
            Project = project;
            Script = script;
            Output = output;
        }

        public bool Evaluate()
        {
            if (string.IsNullOrEmpty(Script))
            {
                return true;
            }

            AwInterpreter interpreter = new AwInterpreter(Project);
            var output = interpreter.RunScript("<pre><code>"+Script+"</code></pre>");
            var value = output.Result;
            if (value is bool b) { return b; }
            if (value is string s) { return s.Length > 0; }
            if (value is int i) { return i > 0; }
            if (value is float f) { return f > 0; }
            return (bool)output.Result;
        }

        public IPath ResolvePath(IPath path)
        {
            return Output != null && !string.IsNullOrEmpty(Output.Id) ? Output.ResolvePath(path) : IPath.Invalid;
        }
    }
}
