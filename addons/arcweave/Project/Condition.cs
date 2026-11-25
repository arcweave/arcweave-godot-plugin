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

        /// <summary>
        /// Evaluates the condition.
        /// </summary>
        /// <returns>The result of the evaluation</returns>
        public bool Evaluate()
        {
            if (string.IsNullOrEmpty(Script))
            {
                return true;
            }

            AwInterpreter interpreter = new AwInterpreter(Project);
            var output = interpreter.RunScript("<pre><code>"+Script+"</code></pre>");
            return IsTruthy(output.Result);
        }

        /// <summary>
        /// Returns whether the result of a condition script is true, like the C# and C++ Arcscript interpreters:
        /// true, a non-empty string, or a number greater than 0. Anything else, including negative numbers and null,
        /// is false.
        /// </summary>
        internal static bool IsTruthy(object result)
        {
            if (result is Variant variant)
            {
                return variant.VariantType switch
                {
                    Variant.Type.Bool => variant.AsBool(),
                    Variant.Type.String => variant.AsString().Length > 0,
                    Variant.Type.Int => variant.AsInt64() > 0,
                    Variant.Type.Float => variant.AsDouble() > 0,
                    _ => false
                };
            }
            return result switch
            {
                float f => f > 0,
                long l => l > 0,
                _ => Interpreter.Expression.GetBoolValue(result)
            };
        }

        public Path ResolvePath(Path path)
        {
            return Output != null && !string.IsNullOrEmpty(Output.Id) ? Output.ResolvePath(path) : Path.Invalid;
        }
    }
}
