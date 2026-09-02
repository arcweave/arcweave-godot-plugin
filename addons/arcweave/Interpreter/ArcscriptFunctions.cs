using System;
using System.Collections.Generic;
using System.Linq;
using Arcweave.Interpreter.INodes;
using Arcweave.Project;

namespace Arcweave.Interpreter
{
    public class Functions
    {
        private static readonly Random _getrandom = new Random();
        private IProject _project;
        private string elementId;
        private ArcscriptState state;
        public Dictionary<string, Func<IList<object>, object>> functions { get; private set; } = new Dictionary<string, Func<IList<object>, object>>();

        public class FunctionDefinition
        {
            public int? MinArgs { get; set; }
            public int? MaxArgs { get; set; }
            public Type ArgumentsType { get; set; }
            public Type ReturnType { get; set; }
        }

        public static Dictionary<string, FunctionDefinition> FunctionDefinitions =
        new() {
            { "abs", new FunctionDefinition { MinArgs=1, MaxArgs=1, ReturnType = typeof(int)} },
            { "max", new FunctionDefinition { MinArgs=2, ReturnType =  typeof(double)} },
            { "min", new FunctionDefinition { MinArgs=2, ReturnType = typeof(double)} },
            { "random", new FunctionDefinition { MinArgs=0, MaxArgs=0, ReturnType = typeof(double)} },
            { "roll", new FunctionDefinition { MinArgs=1, MaxArgs=2, ReturnType = typeof(int)} },
            { "round", new FunctionDefinition { MinArgs=1, MaxArgs=1, ReturnType = typeof(int)} },
            { "sqr", new FunctionDefinition { MinArgs=1, MaxArgs=1, ReturnType = typeof(double)} },
            { "sqrt", new FunctionDefinition { MinArgs=1, MaxArgs=1, ReturnType = typeof(double)} },
            { "visits", new FunctionDefinition { MinArgs=0, MaxArgs=1, ReturnType = typeof(int)} },
            { "show", new FunctionDefinition { MinArgs=1, ReturnType = typeof(string)} },
            { "reset", new FunctionDefinition { MinArgs=1, ArgumentsType = typeof(Variable), ReturnType = typeof(void)} },
            { "resetAll", new FunctionDefinition { MinArgs=0, ArgumentsType = typeof(Variable), ReturnType = typeof(void)} },
            { "resetVisits", new FunctionDefinition { MinArgs=0, MaxArgs = 0, ReturnType = typeof(void)} },
        };

        public Functions(string elementId, IProject project, ArcscriptState state) {
            this._project = project;
            this.elementId = elementId;
            this.state = state;

            this.functions["sqrt"] = this.Sqrt;
            this.functions["sqr"] = this.Sqr;
            this.functions["abs"] = this.Abs;
            this.functions["random"] = this.Random;
            this.functions["roll"] = this.Roll;
            this.functions["show"] = this.Show;
            this.functions["reset"] = this.Reset;
            this.functions["resetAll"] = this.ResetAll;
            this.functions["round"] = this.Round;
            this.functions["min"] = this.Min;
            this.functions["max"] = this.Max;
            this.functions["visits"] = this.Visits;
            this.functions["resetVisits"] = this.ResetVisits;
        }

        public object Sqrt(IList<object> args) {
            Expression e = args[0] as Expression;
            double n;
            if (e.Value is int i)
            {
                n = i;
            } else if (e.Value is double d)
            {
                n = d;
            }
            else
            {
                n = (double)e.Value;
            }
            var result = Math.Sqrt(n);
            if (double.IsNaN(result))
            {
                throw new InvalidOperationException("Cannot compute square root of a negative number.");
            }
            return result;
        }

        public object Sqr(IList<object> args) {
            Expression e = args[0] as Expression;
            double n;
            if (e.Value is int i)
            {
                n = i;
            } else if (e.Value is double d)
            {
                n = d;
            }
            else
            {
                n = (double)e.Value;
            }
            return n * n;
        }

        public object Abs(IList<object> args) {
            Expression e = args[0] as Expression;
            double n;
            if (e.Value is int i)
            {
                n = i;
            } else if (e.Value is double d)
            {
                n = d;
            }
            else
            {
                n = (double)e.Value;
            }
            return Math.Abs(n);
        }

        public object Random(IList<object> args) {
            lock ( _getrandom ) {
                return _getrandom.NextDouble();
            }
        }

        public object Roll(IList<object> args) {
            Expression e = args[0] as Expression;
            int maxRoll = (int)e.Value;
            int numRolls = 1;
            if ( args.Count == 2 ) {
                Expression e2 = args[1] as Expression;
                numRolls = (int)e2.Value;
            }
            int sum = 0;
            for ( int i = 0; i < numRolls; i++ ) {
                int oneRoll = _getrandom.Next(1, maxRoll + 1);
                sum += oneRoll;
            }
            return sum;
        }

        public object Show(IList<object> args) {
            List<string> results = new List<string>();
            foreach (Expression arg in args ) {
                results.Add(arg.ToString());
            }
            string result = String.Join("", results.ToArray());
            // Replace escaped sequences with their actual characters
            result = result
                .Replace("\\a", "\a")
                .Replace("\\b", "\b")
                .Replace("\\f", "\f")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\v", "\v")
                .Replace("\\'", "'")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
            this.state.Outputs.AddScriptOutput(result);
            return null;
        }

        public object Reset(IList<object> args) {
            foreach (IVariable argument in args ) {
                state.SetVarValue(argument, argument.DefaultValue);
            }
            return null;
        }

        public object ResetAll(IList<object> args) {
            HashSet<string> variableIds = new HashSet<string>();
            foreach ( IVariable argument in args ) {
                variableIds.Add(argument.Id);
            }
            foreach ( IVariable variable in this._project.GetAllVariables() ) {
                if ( !variableIds.Contains(variable.Id) ) {
                    state.SetVarValue(variable, variable.DefaultValue);
                }
            }
            return null;
        }

        public object Round(IList<object> args) {
            Expression e = args[0] as Expression;
            double n = (double)e.Value;
            return (int)Math.Round(n);
        }

        public object Min(IList<object> args)
        {
            IList<Expression> arguments = args.Cast<Expression>().ToList();

            return arguments.Min().Value;
        }

        public object Max(IList<object> args) {
            IList<Expression> arguments = args.Cast<Expression>().ToList();
            return arguments.Max().Value;
        }

        public object Visits(IList<object> args) {
            string elementId = this.elementId;
            if ( args != null && args.Count == 1 ) {
                ArcscriptVisitor.Mention mention = (ArcscriptVisitor.Mention)args[0];
                elementId = mention.attrs["data-id"];
            }
            IElement element = this._project.ElementWithId(elementId);
            return element.Visits;
        }

        public object ResetVisits(IList<object> args) {
            this.state.ResetVisits();
            return null;
        }
    }
}
