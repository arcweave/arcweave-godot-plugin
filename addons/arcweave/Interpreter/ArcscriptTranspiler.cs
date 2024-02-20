using Antlr4.Runtime;
using System.Collections.Generic;
using Arcweave.Interpreter.INodes;

namespace Arcweave.Interpreter
{

    public class AwInterpreter
    {
        private IProject Project { get; set; }
        private string ElementId { get; set; }

        public AwInterpreter(IProject project, string elementId = "")
        {
            this.Project = project;
            this.ElementId = elementId;
        }

        private ArcscriptParser.InputContext GetParseTree(string code)
        {
            ICharStream stream = CharStreams.fromString(code);
            ITokenSource lexer = new ArcscriptLexer(stream);
            ITokenStream tokens = new CommonTokenStream(lexer);
            ArcscriptParser parser = new ArcscriptParser(tokens);
            parser.SetProject(Project);

            ArcscriptParser.InputContext tree = parser.input();
            return tree;
        }

        public TranspilerOutput RunScript(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return new TranspilerOutput();
            }
            ArcscriptParser.InputContext tree = this.GetParseTree(code);
            ArcscriptVisitor visitor = new ArcscriptVisitor(this.ElementId, this.Project);
            object result = tree.Accept(visitor);

            // List<string> outputs = visitor.state.outputs;
            var outputResult = visitor.state.Outputs.GetText();

            var isCondition = tree.script() != null;

            return new TranspilerOutput(outputResult, visitor.state.VariableChanges, result, isCondition);
        }

        public class TranspilerOutput
        {
            public string Output { get; private set; }
            public Dictionary<string, object> Changes { get; private set; }
            public object Result { get; private set; }
            public bool IsCondition { get; private set; }

            public TranspilerOutput()
            {
                Result = false;
                Output = "";
                Changes = new Dictionary<string, object>();
                IsCondition = false;
            }
            public TranspilerOutput(string output, Dictionary<string, object> changes, object result,
                bool isCondition = false)
            {
                this.Result = result;
                this.Output = output;
                this.Changes = changes;
                IsCondition = isCondition;
            }
        }
    }
}