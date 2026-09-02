using System.Collections.Generic;
using Antlr4.Runtime;
using System.IO;
using System.Linq;
using Arcweave.Interpreter.INodes;
using Arcweave.Project;

namespace Arcweave.Interpreter
{
    public class ArcscriptParserBase : Parser
    {
        private protected IProject Project { get; private set; }
        private protected List<string> VariableNames = new List<string>();
        internal int currentLine;
        internal int openTagEndPos;



        public ArcscriptParserBase(ITokenStream input) : base(input)
        {
        }

        public ArcscriptParserBase(ITokenStream input, TextWriter output, TextWriter errorOutput) : base(input, output, errorOutput)
        {
        }

        public void SetProject(IProject project) {
            this.Project = project;
            foreach (var projectVariable in project.GetAllVariables())
            {
                VariableNames.Add(projectVariable.Parent == null
                    ? projectVariable.Name
                    : projectVariable.Parent.CustomId + "." + projectVariable.Name);
            }
        }

        public override string[] RuleNames => throw new System.NotImplementedException();

        public override IVocabulary Vocabulary => throw new System.NotImplementedException();

        public override string GrammarFileName => throw new System.NotImplementedException();

        public bool assertVariable(ArcscriptParser.IdentifierContext identifierContext) {
            var variableName = identifierContext.GetText();

            var found = VariableNames.First(name => name == variableName);
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
            string[] classList = attrs["class"].Split(' ');
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

        public bool assertFunctionArguments(IToken fname, ArcscriptParser.Argument_listContext argumentListContext) {
            int argListLength = 0;
            if ( argumentListContext != null && argumentListContext.argument() != null ) {
                argListLength = argumentListContext.argument().Length;
            }
            var min = Functions.FunctionDefinitions[fname.Text].MinArgs;
            var max = Functions.FunctionDefinitions[fname.Text].MaxArgs;
            var argType = Functions.FunctionDefinitions[fname.Text].ArgumentsType;
            if ( ( min != null && argListLength < min ) || ( max != null && argListLength > max ) ) {
                throw new RecognitionException("Incorrect number of arguments for function " + fname.Text, this, this.InputStream, this.Context);
            }

            if (argType != null && argumentListContext != null && argumentListContext.argument() != null)
            {
                if (argType == typeof(Variable))
                {
                    throw new RecognitionException("Function " + fname.Text + " accepts variables as arguments", this, this.InputStream, this.Context);
                }
            }

            return true;
        }

        public bool assertFunctionArguments(IToken fname, ArcscriptParser.Identifier_listContext identifierListContext) {
            int identifierListLength = 0;
            if (identifierListContext != null && identifierListContext.identifier() != null)
            {
                identifierListLength = identifierListContext.identifier().Length;
            }

            var min = Functions.FunctionDefinitions[fname.Text].MinArgs;
            var max = Functions.FunctionDefinitions[fname.Text].MaxArgs;
            if ( ( min != null && identifierListLength < min ) || ( max != null && identifierListLength > max ) ) {
                throw new RecognitionException("Incorrect number of arguments for function " + fname.Text, this, this.InputStream, this.Context);
            }

            return true;
        }

        internal void setLineStart(IToken token)
        {
            openTagEndPos = token.StartIndex + token.Text.Length;
        }
    }

}
