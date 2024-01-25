using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Variable : GodotObject, IVariable
    {
        public string Name { get; set; }
        public object Value { get; set; }


        public string _typeName { get; private set; }
        public System.Type Type => System.Type.GetType(_typeName);
        public object _defaultValue { get; private set; }

        public Variable(string name, object value)
        {
            Name = name;
            Value = value;
            _defaultValue = value;
            this._typeName = value.GetType().FullName;
        }

        public void ResetToDefaultValue()
        {
            Value = _defaultValue;
        }
    }
}
