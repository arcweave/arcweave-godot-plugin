using System.Collections.Generic;
using Antlr4.Runtime;
using System.IO;
using System.Linq;
using Arcweave.Interpreter.INodes;

namespace Arcweave.Interpreter
{
    public class ArcscriptParserBase : Parser
    {
        private class FunctionArgs
        {
            public int? MinArgs { get; set; }
            public int? MaxArgs { get; set; }
        }

        private Dictionary<string, FunctionArgs> ArcscriptFunctions;
        private protected IProject Project { get; private set; }

        internal int currentLine;
        internal int openTagEndPos;
        public ArcscriptParserBase(ITokenStream input) : base(input) {
            var functions = new Dictionary<string, FunctionArgs>()
        {
            { "abs", new FunctionArgs { MinArgs=1, MaxArgs=1 } },
            { "max", new FunctionArgs { MinArgs=2 } },
            { "min", new FunctionArgs { MinArgs=2 } },
            { "random", new FunctionArgs { MinArgs=0, MaxArgs=0 } },
            { "roll", new FunctionArgs { MinArgs=1, MaxArgs=2 } },
            { "round", new FunctionArgs { MinArgs=1, MaxArgs=1 } },
            { "sqr", new FunctionArgs { MinArgs=1, MaxArgs=1 } },
            { "sqrt", new FunctionArgs { MinArgs=1, MaxArgs=1 } },
            { "visits", new FunctionArgs { MinArgs=0, MaxArgs=1 } },
            { "show", new FunctionArgs { MinArgs=1 } },
            { "reset", new FunctionArgs { MinArgs=1 } },
            { "resetAll", new FunctionArgs { MinArgs=0 } },
        };

            this.ArcscriptFunctions = functions;
        }

        public ArcscriptParserBase(ITokenStream input, TextWriter output, TextWriter errorOutput) : base(input, output, errorOutput) {
            var functions = new Dictionary<string, FunctionArgs>()
        {
            { "abs", new FunctionArgs { MinArgs=1, MaxArgs=1 } },
            { "max", new FunctionArgs { MinArgs=2 } },
            { "min", new FunctionArgs { MinArgs=2 } },
            { "random", new FunctionArgs { MinArgs=0, MaxArgs=0 } },
            { "roll", new FunctionArgs { MinArgs=1, MaxArgs=2 } },
            { "round", new FunctionArgs { MinArgs=1, MaxArgs=1 } },
            { "sqr", new FunctionArgs { MinArgs=1, MaxArgs=1 } },
            { "sqrt", new FunctionArgs { MinArgs=1, MaxArgs=1 } },
            { "visits", new FunctionArgs { MinArgs=0, MaxArgs=1 } },
            { "show", new FunctionArgs { MinArgs=1 } },
            { "reset", new FunctionArgs { MinArgs=1 } },
            { "resetAll", new FunctionArgs { MinArgs=0 } },
        };

            this.ArcscriptFunctions = functions;
        }

        public void SetProject(IProject project) {
            this.Project = project;
        }

        public override string[] RuleNames => throw new System.NotImplementedException();

        public override IVocabulary Vocabulary => throw new System.NotImplementedException();

        public override string GrammarFileName => throw new System.NotImplementedException();

        public bool assertVariable(IToken variable) {
            var variableName = variable.Text;
            var found = this.Project.Variables.First(x => x.Name == variableName);
            if ( found != null ) {
                return false;
            }
            return true;
        }

        public bool assertMention(IList<ArcscriptParser.Mention_attributesContext> attrCtxList) {
            Dictionary<string, string> attrs = new Dictionary<string, string>();
            foreach ( var attrCtx in attrCtxList ) {
                string attrName = attrCtx.GetToken(ArcscriptParser.ATTR_NAME, 0)?.GetText();
                string attrValue = attrCtx.GetToken(ArcscriptParser.ATTR_VALUE, 0)?.GetText() ?? "";
                if ( attrValue.StartsWith("\"") && attrValue.EndsWith("\"") ) {
                    attrValue = attrValue.Substring(1, attrValue.Length - 2);
                } else if ( attrValue.StartsWith("'") && attrValue.EndsWith("'") ) {
                    attrValue = attrValue.Substring(1, attrValue.Length - 2);
                }
                attrs.Add(attrName, attrValue);
            }
            string[] classList = attrs["class"].Split(" ");
            if ( !classList.Contains("mention") ) {
                return false;
            }
            if ( attrs["data-type"] != "element" ) {
                return false;
            }
            if ( this.Project.ElementWithId(attrs["data-id"]) == null ) {
                return false;
            }
            return true;
        }

        public bool assertFunctionArguments(IToken fname, ArcscriptParser.Argument_listContext argumentList) {
            int argListLength = 0;
            if ( argumentList != null && argumentList.argument() != null ) {
                argListLength = argumentList.argument().Length;
            }
            var min = this.ArcscriptFunctions[fname.Text].MinArgs;
            var max = this.ArcscriptFunctions[fname.Text].MaxArgs;
            if ( ( min != null && argListLength < min ) || ( max != null && argListLength > max ) ) {
                return false;
            }
            return true;
        }

        public bool assertFunctionArguments(IToken fname, ArcscriptParser.Variable_listContext variable_List) {
            int varListLength = variable_List.VARIABLE().Length;
            var min = this.ArcscriptFunctions[fname.Text].MinArgs;
            var max = this.ArcscriptFunctions[fname.Text].MaxArgs;
            if ( ( min != null && varListLength < min ) || ( max != null && varListLength > max ) ) {
                return false;
            }
            return true;
        }

        internal void setLineStart(IToken token)
        {
            openTagEndPos = token.StartIndex + token.Text.Length;
        }
    }

}