using Arcweave.Interpreter;
using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Condition : GodotObject, INode
    {
        [Export] public string Id { get; set; }
        // If null or empty, it's an "else" condition
        [Export] public string Script { get; private set; }
        [Export] public Project Project { get; private set; }
        [Export] public Connection Output { get; private set; }

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
            if (output.Result is Variant result)
            {
                if (result.VariantType == Variant.Type.Bool) { return result.AsBool(); }
                if (result.VariantType == Variant.Type.String) { return result.AsString().Length > 0; }
                if (result.VariantType == Variant.Type.Int) { return result.AsInt32() > 0; }
                if (result.VariantType == Variant.Type.Float) { return result.AsDouble() > 0; }    
            }
            else
            {
                var value = output.Result;
                switch (value)
                {
                    case bool b:
                        return b;
                    case string s:
                        return s.Length > 0;
                    case int i:
                        return i > 0;
                    case float f:
                        return f > 0;
                }
            }
            return (bool)output.Result;
        }

        public Path ResolvePath(Path path)
        {
            return Output != null && !string.IsNullOrEmpty(Output.Id) ? Output.ResolvePath(path) : Path.Invalid;
        }
    }
}
