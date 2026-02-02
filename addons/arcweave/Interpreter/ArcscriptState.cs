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

        private System.Action<string> _emit;
        public ArcscriptState(string elementId, IProject project, System.Action<string>? emit = null)
        {
            Outputs = new ArcscriptOutputs();
            this.currentElement = elementId;
            this.project = project;
            if (emit != null)
            {
                _emit = emit;
            }
            else
            {
                _emit = (string eventName) => { };
            }
        }

        public IVariable? GetVariable(string name) {
            try
            {
                return this.project.Variables.First(variable => variable.Name == name);
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }
        }

        public object GetVarValue(string name) {
            if ( this.VariableChanges.ContainsKey(name) ) {
                return VariableChanges[name];
            }
            return this.project.GetVariable(name).ObjectValue;
        }

        public void SetVarValue(string name, object value) { VariableChanges[name] = value; }

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