#nullable enable
using System;
using System.Collections.Generic;
using Antlr4.Runtime.Misc;
using System.Globalization;
using System.Linq;
using Antlr4.Runtime.Tree;
using Arcweave.Interpreter.INodes;
using Arcweave.Project;

namespace Arcweave.Interpreter
{
    public class ArcscriptVisitor : ArcscriptParserBaseVisitor<object?>
    {
        public IProject project;
        public readonly ArcscriptState state;
        public string elementId;
        private readonly Functions _functions;
        private System.Action<string> _emit;
        public ArcscriptVisitor(string elementId, IProject project, System.Action<string>? emit = null) {
            this.elementId = elementId;
            this.project = project;
            this.state = new ArcscriptState(elementId, project, emit);
            this._functions = new Functions(elementId, project, this.state);
            if (emit != null)
            {
                _emit = emit;
            }
            else
            {
                _emit = (string eventName) => {  };
            }
        }

        public override object VisitInput([NotNull] ArcscriptParser.InputContext context) {
            if ( context.script() != null ) {
                return this.VisitScript(context.script());
            }

            Expression condition = (Expression)this.VisitCondition(context.condition());
            return Expression.GetBoolValue(condition.Value);
        }

        public override object VisitCondition(ArcscriptParser.ConditionContext context)
        {
            return Visit(context.expression())!;
        }

        public override object VisitScript_section([NotNull] ArcscriptParser.Script_sectionContext context) {
            if ( context == null ) {
                return null!;
            }

            var blockquoteContexts = context.blockquote();
            if (blockquoteContexts != null && blockquoteContexts.Length > 0)
            {
                object[] result = new object[blockquoteContexts.Length];
                int index = 0;
                foreach (var blockquoteContext in blockquoteContexts)
                {
                    result[index++] = this.VisitBlockquote(blockquoteContext);
                }
                return result;
            }

            var paragraphContexts = context.paragraph();
            if (paragraphContexts != null && paragraphContexts.Length > 0)
            {
                object[] result = new object[paragraphContexts.Length];
                int index = 0;
                foreach (var paragraphContext in paragraphContexts)
                {
                    result[index++] = this.VisitParagraph(paragraphContext);
                }
                return result;
            }

            return this.VisitChildren(context);
        }

        public override object VisitParagraph(ArcscriptParser.ParagraphContext context)
        {
            var paragraphEnd = context.PARAGRAPHEND().GetText();
            var paragraphContent = paragraphEnd.Substring(0, paragraphEnd.Length - "</p>".Length);
            this.state.Outputs.AddParagraph(paragraphContent);
            return context.GetText();
        }

        public override object VisitBlockquote(ArcscriptParser.BlockquoteContext context)
        {
            this.state.Outputs.AddBlockquote();
            this.VisitChildren(context);
            this.state.Outputs.ExitBlockquote();
            return context.GetText();
        }

        public override object VisitAssignment_segment([NotNull] ArcscriptParser.Assignment_segmentContext context) {
            this.state.Outputs.AddScriptOutput(null);
            return this.VisitStatement_assignment(context.statement_assignment());
        }

        public override object VisitFunction_call_segment([NotNull] ArcscriptParser.Function_call_segmentContext context) {
            this.state.Outputs.AddScriptOutput(null);
            return this.VisitFunction_call(context.function_call());
        }

        public override object VisitConditional_section([NotNull] ArcscriptParser.Conditional_sectionContext context) {
            this.state.Outputs.AddScriptOutput(null);
            ConditionalSection if_section = (ConditionalSection)this.VisitIf_section(context.if_section());
            if ( if_section.Clause ) {
                this.state.Outputs.AddScriptOutput(null);
                return if_section.Script;
            }
            foreach(ArcscriptParser.Else_if_sectionContext else_if_context in context.else_if_section())
            {
                ConditionalSection elif_section = (ConditionalSection)this.VisitElse_if_section(else_if_context);
                if (elif_section.Clause )
                {
                    return elif_section.Script;
                }
            }

            if ( context.else_section() != null ) {
                ConditionalSection elseSection = (ConditionalSection)this.VisitElse_section(context.else_section());
                this.state.Outputs.AddScriptOutput(null);
                return elseSection.Script;
            }
            this.state.Outputs.AddScriptOutput(null);
            return null!;
        }

        public override object VisitIf_section([NotNull] ArcscriptParser.If_sectionContext context) {
            Expression result = (Expression)this.VisitIf_clause(context.if_clause());
            ConditionalSection ifSection = new ConditionalSection(false, null);
            if ( result ) {
                ifSection.Clause = true;
                ifSection.Script = this.VisitScript(context.script());
            }
            return ifSection;
        }

        public override object VisitElse_if_section([NotNull] ArcscriptParser.Else_if_sectionContext context) {
            Expression result = (Expression)this.VisitElse_if_clause(context.else_if_clause());
            ConditionalSection elseIfSection = new ConditionalSection(false, null);
            if ( result ) {
                elseIfSection.Clause = true;
                elseIfSection.Script = this.VisitScript(context.script());
            }
            return elseIfSection;
        }

        public override object VisitElse_section([NotNull] ArcscriptParser.Else_sectionContext context) {
            return new ConditionalSection(true, this.VisitScript(context.script()));
        }

        public override object VisitIf_clause([NotNull] ArcscriptParser.If_clauseContext context) {
            return Visit(context.expression())!;
        }

        public override object VisitElse_if_clause([NotNull] ArcscriptParser.Else_if_clauseContext context) {
            return Visit(context.expression())!;
        }

        public override object VisitStatement_assignment([NotNull] ArcscriptParser.Statement_assignmentContext context)
        {
            var identifier = (IdentifierDef) this.VisitAssignable(context.assignable());

            var identifierValue = new Expression(this.state.GetVarValue(identifier.Name, identifier.Scope));

            var re = (Expression)Visit(context.expression())!;

            if ( context.ASSIGN() != null ) {
                this.state.SetVarValue(identifier, re.Value);
                return null!;
            }

            if ( context.ASSIGNADD() != null ) {
                identifierValue += re;
            } else if ( context.ASSIGNSUB() != null ) {
                identifierValue -= re;
            } else if ( context.ASSIGNMUL() != null ) {
                identifierValue *= re;
            } else if ( context.ASSIGNDIV() != null ) {
                identifierValue /= re;
            } else if (context.ASSIGNMOD() != null) {
                identifierValue %= re;
            }

            this.state.SetVarValue(identifier, identifierValue.Value);
            return null!;
        }

        public override object VisitAssignable(ArcscriptParser.AssignableContext context)
        {
            return this.VisitIdentifier(context.identifier());
        }

        public override object VisitComparisonExpression(ArcscriptParser.ComparisonExpressionContext context)
        {
            var left = (Expression)Visit(context.expression(0))!;
            if (context.AND() != null || context.ANDKEYWORD() != null)
            {
                if (!Expression.GetBoolValue(left.Value))
                {
                    return new Expression(false);
                }
                var rightComp = (Expression)Visit(context.expression(1))!;
                return new Expression(Expression.GetBoolValue(rightComp.Value));
            }

            if (context.OR() != null || context.ORKEYWORD() != null)
            {
                if (Expression.GetBoolValue(left.Value))
                {
                    return left;
                }
                var rightComp = (Expression)Visit(context.expression(1))!;
                return rightComp;
            }

            var right = (Expression)Visit(context.expression(1))!;

            if (context.EQ() != null || (context.ISKEYWORD() != null && context.NOTKEYWORD() == null))
            {
                return new Expression(left == right);
            }

            if (context.NE() != null || (context.ISKEYWORD() != null && context.NOTKEYWORD() != null))
            {
                return new Expression(left != right);
            }

            if (context.LT() != null)
            {
                return new Expression(left < right);
            }

            if (context.GT() != null)
            {
                return new Expression(left > right);
            }

            if (context.LE() != null)
            {
                return new Expression(left <= right);
            }

            if (context.GE() != null)
            {
                return new Expression(left >= right);
            }

            throw new Exception("Unknown comparison operator");
        }

        public override object VisitUnaryExpression(ArcscriptParser.UnaryExpressionContext context)
        {
            if (context.NOTKEYWORD() != null || context.NEG() != null)
            {
                return !(Expression)Visit(context.expression())!;
            }

            if (context.ADD() != null)
            {
                return Visit(context.expression())!;
            }

            if (context.SUB() != null)
            {
                return -(Expression)Visit(context.expression())!;
            }
            throw new Exception("Unknown unary operator");
        }

        public override object VisitMultiplicativeExpression(ArcscriptParser.MultiplicativeExpressionContext context)
        {
            var left = (Expression)Visit(context.expression(0))!;
            var right = (Expression)Visit(context.expression(1))!;
            if (context.MUL() != null)
            {
                return left * right;
            }

            if (context.DIV() != null)
            {
                return left / right;
            }

            if (context.MOD() != null)
            {
                return left % right;
            }
            throw new Exception("Unknown multiplicative operator");
        }

        public override object VisitAdditiveExpression(ArcscriptParser.AdditiveExpressionContext context)
        {
            var left = (Expression)Visit(context.expression(0))!;
            var right = (Expression)Visit(context.expression(1))!;
            if (context.ADD() != null)
            {
                return left + right;
            }

            if (context.SUB() != null)
            {
                return left - right;
            }
            throw new Exception("Unknown additive operator");
        }

        public override object VisitParenthesizedExpression(ArcscriptParser.ParenthesizedExpressionContext context)
        {
            return Visit(context.expression())!;
        }

        public override object VisitIdentifierExpression(ArcscriptParser.IdentifierExpressionContext context)
        {
            var identifier = (IdentifierDef)VisitIdentifier(context.identifier());
            return new Expression(this.state.GetVarValue(identifier.Name, identifier.Scope));
        }

        public override object VisitLiteralExpression(ArcscriptParser.LiteralExpressionContext context)
        {
            return VisitLiteral(context.literal());
        }

        public override object VisitFunctionCallExpression(ArcscriptParser.FunctionCallExpressionContext context)
        {
            return new Expression(VisitFunction_call(context.function_call()));
        }

        public override object VisitLiteral(ArcscriptParser.LiteralContext context)
        {
            if (context.BOOLEAN() != null)
            {
                if (context.BOOLEAN().GetText() == "true")
                {
                    return new Expression(true);
                }
                return new Expression(false);
            }

            if (context.STRING() != null)
            {
                string result = context.STRING().GetText();
                result = result.Substring(1, result.Length - 2);
                return new Expression(result);
            }
            return VisitNumeric_literal(context.numeric_literal());
        }

        public override object VisitNumeric_literal(ArcscriptParser.Numeric_literalContext context)
        {
            if (context.FLOAT() != null)
            {
                return new Expression(double.Parse(context.FLOAT().GetText(), CultureInfo.InvariantCulture));
            }
            return new Expression(int.Parse(context.INTEGER().GetText()));
        }

        public override object VisitFunction_call([NotNull] ArcscriptParser.Function_callContext context) {
            IList<object>? argument_list_result = null;
            string fname = context.FNAME().GetText();

            if ( context.argument_list() != null ) {
                argument_list_result = (IList<object>)this.VisitArgument_list(context.argument_list());
            }

            if (context.identifier_list() != null)
            {
                argument_list_result = (IList<object>)this.VisitIdentifier_list(context.identifier_list());
                if (Functions.FunctionDefinitions.ContainsKey(fname))
                {
                    if (Functions.FunctionDefinitions[fname].ArgumentsType == typeof(Variable))
                    {
                        for (int i = 0; i < argument_list_result.Count; i++)
                        {
                            IdentifierDef identifierDef = (IdentifierDef)argument_list_result[i];
                            argument_list_result[i] = this.state.GetVariable(identifierDef.Name, identifierDef.Scope);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < argument_list_result.Count; i++)
                        {
                            IdentifierDef identifierDef = (IdentifierDef)argument_list_result[i];
                            argument_list_result[i] = new Expression(this.state.GetVarValue(identifierDef.Name, identifierDef.Scope));
                        }
                    }
                }
            }

            object returnValue = this._functions.functions[fname](argument_list_result);

            return returnValue;
        }

        public override object VisitArgument_list([NotNull] ArcscriptParser.Argument_listContext context)
        {
            return context.argument().Select(this.VisitArgument).ToList();
        }

        public override object VisitArgument([NotNull] ArcscriptParser.ArgumentContext context) {
            if (context.expression() != null)
            {
                return Visit(context.expression())!;
            }
            if ( context.mention() != null ) {
                Mention mention_result = (Mention)this.VisitMention(context.mention());
                return mention_result;
            }
            throw new Exception("Unknown argument");
        }

        public override object VisitIdentifier_list(ArcscriptParser.Identifier_listContext context)
        {
            return context.identifier().Select(this.VisitIdentifier).ToList();
        }

        public override object VisitIdentifier(ArcscriptParser.IdentifierContext context)
        {
            string name = "";
            string? scope = null;
            if (context.IDENTIFIER().Length == 1)
            {
                name = context.IDENTIFIER(0).GetText();
            }
            else
            {
                scope = context.IDENTIFIER(0).GetText();
                name = context.IDENTIFIER(1).GetText();
            }
            return new IdentifierDef(name, scope);
        }

        public override object VisitMention([NotNull] ArcscriptParser.MentionContext context) {
            Dictionary<string, string> attrs = new Dictionary<string, string>();

            foreach ( ArcscriptParser.Mention_attributesContext attr in context.mention_attributes() ) {
                Dictionary<string, object> res = (Dictionary<string, object>)this.VisitMention_attributes(attr);
                attrs.Add((string)res["name"], (string)res["value"]);
            }
            string label = "";
            if ( context.MENTION_LABEL() != null ) {
                label = context.MENTION_LABEL().GetText();
            }
            return new Mention(label, attrs);
        }

        public override object VisitMention_attributes([NotNull] ArcscriptParser.Mention_attributesContext context) {
            string name = context.ATTR_NAME().GetText();
            ITerminalNode ctxvalue = context.ATTR_VALUE();
            object value = true;
            if ( ctxvalue != null ) {
                string strvalue = ctxvalue.GetText();
                if ( ( strvalue.StartsWith("\"") && strvalue.EndsWith("\"") ) ||
                    ( strvalue.StartsWith("'") && strvalue.EndsWith("'") ) ) {
                    strvalue = strvalue.Substring(1, strvalue.Length - 2);
                }
                value = strvalue;
            }
            return new Dictionary<string, object> { { "name", name }, { "value", value } };
        }

        public class Argument
        {
            public Type type { get; private set; }
            public object value { get; private set; }
            public Argument(Type type, object value) {
                this.type = type;
                this.value = value;
            }
        }

        public class Mention
        {
            public string label { get; private set; }
            public Dictionary<string, string> attrs { get; private set; }

            public Mention(string label, Dictionary<string, string> attrs) {
                this.label = label;
                this.attrs = attrs;
            }
        }

        public struct ConditionalSection
        {
            public bool Clause;
            public object Script;

            public ConditionalSection(bool clause, object script) { Clause = clause; Script = script; }
        }

        public struct IdentifierDef
        {
            public readonly string Name;
            public readonly string? Scope;
            public IdentifierDef(string name, string? scope = null) { Name = name; Scope = scope; }
        }
    }
}
