#nullable enable
using System;
using System.Collections.Generic;
using Antlr4.Runtime.Misc;
using System.Globalization;
using Antlr4.Runtime.Tree;
using Arcweave.Interpreter.INodes;

namespace Arcweave.Interpreter
{
    public class ArcscriptVisitor : ArcscriptParserBaseVisitor<object>
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

            Expression comp_cond = (Expression)this.VisitCompound_condition_or(context.compound_condition_or());
            return Expression.GetBoolValue(comp_cond.Value);
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
            // if ( context.normal_text() != null && context.normal_text().Length > 0 ) {
            //     this.state.outputs.Add(context.GetText());
            //     return context.GetText();
            // }

            return this.VisitChildren(context);
        }

        public override object VisitParagraph(ArcscriptParser.ParagraphContext context)
        {
            // this.state.outputs.Add(context.GetText());
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
            return this.VisitStatement_function_call(context.statement_function_call());
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
            return this.VisitCompound_condition_or(context.compound_condition_or());
        }

        public override object VisitElse_if_clause([NotNull] ArcscriptParser.Else_if_clauseContext context) {
            return this.VisitCompound_condition_or(context.compound_condition_or());
        }

        public override object VisitStatement_assignment([NotNull] ArcscriptParser.Statement_assignmentContext context) {
            string variableName = context.VARIABLE().GetText();

            Expression compound_condition_or = (Expression)this.VisitCompound_condition_or(context.compound_condition_or());
            if ( context.ASSIGN() != null ) {
                this.state.SetVarValue(variableName, compound_condition_or.Value);
                return null!;
            }

            Expression variableValue = new Expression(this.state.GetVarValue(variableName));

            if ( context.ASSIGNADD() != null ) {
                variableValue += compound_condition_or;
            } else if ( context.ASSIGNSUB() != null ) {
                variableValue -= compound_condition_or;
            } else if ( context.ASSIGNMUL() != null ) {
                variableValue *= compound_condition_or;
            } else if ( context.ASSIGNDIV() != null ) {
                variableValue /= compound_condition_or;
            } else if (context.ASSIGNMOD() != null) {
                variableValue %= compound_condition_or;
            }

            this.state.SetVarValue(variableName, variableValue.Value);
            return null!;
        }

        public override object VisitCompound_condition_or([NotNull] ArcscriptParser.Compound_condition_orContext context) {
            Expression compound_condition_and = (Expression)this.VisitCompound_condition_and(context.compound_condition_and());
            if ( context.compound_condition_or() != null ) {
                Expression compound_condition_or = (Expression)this.VisitCompound_condition_or(context.compound_condition_or());
                Expression result = compound_condition_and || compound_condition_or;
                return result;
            }
            return compound_condition_and;
        }

        public override object VisitCompound_condition_and([NotNull] ArcscriptParser.Compound_condition_andContext context) {
            Expression negated_unary_condition = (Expression)this.VisitNegated_unary_condition(context.negated_unary_condition());
            if ( context.compound_condition_and() != null ) {
                Expression compound_condition_and = (Expression)this.VisitCompound_condition_and(context.compound_condition_and());
                Expression result = negated_unary_condition && compound_condition_and;
                return result;
            }

            return negated_unary_condition;
        }

        public override object VisitNegated_unary_condition([NotNull] ArcscriptParser.Negated_unary_conditionContext context) {
            Expression unary_condition = (Expression)this.VisitUnary_condition(context.unary_condition());

            if ( context.NEG() != null || context.NOTKEYWORD() != null ) {
                return !unary_condition;
            }

            return unary_condition;
        }

        public override object VisitUnary_condition([NotNull] ArcscriptParser.Unary_conditionContext context) {
            return this.VisitCondition(context.condition());
        }

        public override object VisitCondition([NotNull] ArcscriptParser.ConditionContext context) {
            if ( context.expression().Length == 1 ) {
                return this.VisitExpression(context.expression()[0]);
            }
            ArcscriptParser.Conditional_operatorContext conditional_operator_context = context.conditional_operator();
            Expression exp0 = (Expression)this.VisitExpression(context.expression()[0]);
            Expression exp1 = (Expression)this.VisitExpression(context.expression()[1]);

            bool result = false;
            if ( conditional_operator_context.GT() != null ) {
                result = exp0 > exp1;

            }
            if ( conditional_operator_context.GE() != null ) {
                result = exp0 >= exp1;
            }
            if ( conditional_operator_context.LT() != null ) {
                result = exp0 < exp1;
            }
            if ( conditional_operator_context.LE() != null ) {
                result = exp0 <= exp1;
            }
            if ( conditional_operator_context.EQ() != null ) {
                result = exp0 == exp1;
            }
            if ( conditional_operator_context.NE() != null ) {
                result = exp0 != exp1;
            }
            if ( conditional_operator_context.ISKEYWORD() != null ) {
                if ( conditional_operator_context.NOTKEYWORD() != null ) {
                    result = exp0 != exp1;
                } else
                {
                    result = exp0 == exp1;
                }
            }

            return new Expression(result);
        }

        public override object VisitExpression([NotNull] ArcscriptParser.ExpressionContext context) {
            if ( context.STRING() != null ) {
                string result = context.STRING().GetText();
                result = result.Substring(1, result.Length - 2);
                return new Expression(result);
            }
            if ( context.BOOLEAN() != null ) {
                return new Expression(context.BOOLEAN().GetText() == "true");
            }
            return this.VisitAdditive_numeric_expression(context.additive_numeric_expression());
        }

        public override object VisitAdditive_numeric_expression([NotNull] ArcscriptParser.Additive_numeric_expressionContext context) {
            if ( context.additive_numeric_expression() != null ) {
                Expression result = (Expression)this.VisitAdditive_numeric_expression(context.additive_numeric_expression());
                Expression mult_num_expression = (Expression)this.VisitMultiplicative_numeric_expression(context.multiplicative_numeric_expression());
                if ( context.ADD() != null ) {
                    return result + mult_num_expression;
                }
                // Else MINUS
                return result - mult_num_expression;
            }

            return (Expression)this.VisitMultiplicative_numeric_expression(context.multiplicative_numeric_expression());
        }

        public override object VisitMultiplicative_numeric_expression([NotNull] ArcscriptParser.Multiplicative_numeric_expressionContext context) {
            if ( context.multiplicative_numeric_expression() != null ) {
                Expression result = (Expression)this.VisitMultiplicative_numeric_expression(context.multiplicative_numeric_expression());
                Expression signed_unary_num_expr = (Expression)this.VisitSigned_unary_numeric_expression(context.signed_unary_numeric_expression());
                if ( context.MUL() != null ) {
                    return result * signed_unary_num_expr;
                }

                if (context.DIV() != null)
                {
                    return result / signed_unary_num_expr;
                }
                // Else MOD
                return result % signed_unary_num_expr;
            }

            return (Expression)this.VisitSigned_unary_numeric_expression(context.signed_unary_numeric_expression());
        }

        public override object VisitSigned_unary_numeric_expression([NotNull] ArcscriptParser.Signed_unary_numeric_expressionContext context) {
            Expression unary_num_expr = (Expression)this.VisitUnary_numeric_expression(context.unary_numeric_expression());
            ArcscriptParser.SignContext sign = context.sign();

            if ( sign != null ) {
                if ( sign.ADD() != null ) {
                    return unary_num_expr;
                }
                // Else MINUS
                return -unary_num_expr;
            }
            return unary_num_expr;
        }

        public override object VisitUnary_numeric_expression([NotNull] ArcscriptParser.Unary_numeric_expressionContext context) {
            if ( context.FLOAT() != null ) {
                return new Expression(double.Parse(context.FLOAT().GetText(), CultureInfo.InvariantCulture));
            }
            if ( context.INTEGER() != null ) {
                return new Expression(int.Parse(context.INTEGER().GetText()));
            }

            if (context.STRING() != null)
            {
                string result = context.STRING().GetText();
                result = result.Substring(1, result.Length - 2);
                return new Expression(result);
            }

            if (context.BOOLEAN() != null)
            {
                return new Expression(context.BOOLEAN().GetText() == "true");
            }
            if ( context.VARIABLE() != null ) {
                string variableName = context.VARIABLE().GetText();
                return new Expression(this.state.GetVarValue(variableName));
            }

            if ( context.function_call() != null )
            {
                object functionResult = this.VisitFunction_call(context.function_call());
                if (functionResult.GetType() == typeof(Expression))
                {
                    return functionResult;
                }
                return new Expression(functionResult);
            }
            return this.VisitCompound_condition_or(context.compound_condition_or());
        }
        public override object VisitVoid_function_call([NotNull] ArcscriptParser.Void_function_callContext context)
        {
            string fname = "";
            IList<object>? argument_list_result = null;
            if (context.VFNAME() != null)
            {
                fname = context.VFNAME().GetText();
                if (context.argument_list() != null)
                {
                    argument_list_result = (IList<object>)this.VisitArgument_list(context.argument_list());
                }
            }
            if (context.VFNAMEVARS() != null)
            {
                fname = context.VFNAMEVARS().GetText();
                if (context.variable_list() != null)
                {
                    argument_list_result = (IList<object>)this.VisitVariable_list(context.variable_list());
                }
            }

            object returnValue = this._functions.functions[fname](argument_list_result);

            return returnValue;
        }

        public override object VisitFunction_call([NotNull] ArcscriptParser.Function_callContext context) {
            IList<object>? argument_list_result = null;
            if ( context.argument_list() != null ) {
                argument_list_result = (IList<object>)this.VisitArgument_list(context.argument_list());
            }

            string fname = context.FNAME().GetText();

            Type resultType = this._functions.returnTypes[fname];
            object returnValue = this._functions.functions[fname](argument_list_result);

            return returnValue;
        }

        public override object VisitVariable_list([NotNull] ArcscriptParser.Variable_listContext context)
        {
            List<object> variables = new List<object>();
            foreach (ITerminalNode variable in context.VARIABLE())
            {
                var varObject = this.state.GetVariable(variable.GetText());
                if (varObject != null)
                {
                    variables.Add(varObject);
                }
            }
            return variables;
        }

        public override object VisitArgument_list([NotNull] ArcscriptParser.Argument_listContext context) {
            List<object> argumentList = new List<object>();
            foreach ( ArcscriptParser.ArgumentContext argument in context.argument() ) {
                argumentList.Add(this.VisitArgument(argument));
            }
            return argumentList;
        }

        public override object VisitArgument([NotNull] ArcscriptParser.ArgumentContext context) {
            if ( context.STRING() != null ) {
                string result = context.STRING().GetText();
                result = result.Substring(1, result.Length - 2);
                return new Expression(result);
            }
            if ( context.mention() != null ) {
                Mention mention_result = (Mention)this.VisitMention(context.mention());
                return mention_result;
            }
            return this.VisitAdditive_numeric_expression(context.additive_numeric_expression());
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
    }
}