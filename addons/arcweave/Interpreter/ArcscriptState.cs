#nullable enable
using System.Collections.Generic;
using System.Linq;
using Arcweave.Interpreter.INodes;
using Arcweave.Project;

namespace Arcweave.Interpreter
{
    public class ArcscriptState
    {
        public Dictionary<string, object> VariableChanges = new Dictionary<string, object>();
        // public List<string> outputs = new List<string>();
        public ArcscriptOutputs Outputs;
        public string currentElement { get; set; }
        public IProject project { get; set; }

        public Dictionary<string, Variable> Variables { get; } = new Dictionary<string, Variable>();

        private System.Action<string> _emit;
        public ArcscriptState(string elementId, IProject project, System.Action<string>? emit = null)
        {
            Outputs = new ArcscriptOutputs();
            this.currentElement = elementId;
            this.project = project;

            foreach (var variable in project.GetAllVariables())
            {
                Variables.TryAdd(variable.Id, variable);
            }

            if (emit != null)
            {
                _emit = emit;
            }
            else
            {
                _emit = (string eventName) => { };
            }
        }

        public IVariable? GetVariable(string name, string? scope = null) {
            try
            {
                return Variables.Values.FirstOrDefault(variable =>
                {
                    if (scope != null)
                    {
                        return variable.Name == name &&
                            variable.Parent != null &&
                            scope == variable.Parent.CustomId;
                    }

                    return variable.Name == name && variable.Parent == null;
                });
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }
        }

        public object GetVarValue(string name, string? scope = null)
        {
            var v = GetVariable(name, scope);
            if (v == null)
            {
                throw new System.InvalidOperationException($"Variable {name} not found");
            }

            return this.VariableChanges.ContainsKey(v.Id) ? VariableChanges[v.Id] : v.ObjectValue;
        }

        public void SetVarValue(IVariable v, object value)
        {
            VariableChanges[v.Id] = value;
        }

        public void SetVarValue(ArcscriptVisitor.IdentifierDef identifierDef, object value) {
            var v = GetVariable(identifierDef.Name, identifierDef.Scope);
            if (v == null)
            {
                throw new System.InvalidOperationException($"Variable {identifierDef.Name} not found");
            }
            VariableChanges[v.Id] = value;
        }

        public void SetVarValues(string[] names, string[] values) {
            for ( int i = 0; i < names.Length; i++ ) {
                this.VariableChanges[names[i]] = values[i];
            }
        }

        public void ResetVisits()
        {
            foreach (var board in project.Boards)
            {
#if GODOT
                foreach (var element in board.Value.Elements)
#else
                foreach (var element in board.Nodes.OfType<Element>())
#endif
                {
                    element.Visits = 0;
                }
            }
            _emit("resetVisits");
        }
    }
}
