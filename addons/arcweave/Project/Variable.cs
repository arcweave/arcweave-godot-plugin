using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Variable
    {
        public string Name { get; set; }
        public Variant Value { get; set; }


        public string _typeName { get; private set; }
        public System.Type Type {
            get
            {
                if (Value.VariantType == Variant.Type.String) return typeof(string);
                if (Value.VariantType == Variant.Type.Int) return typeof(long);
                if (Value.VariantType == Variant.Type.Bool) return typeof(bool);
                if (Value.VariantType == Variant.Type.Float) return typeof(double);
                return null;
            }
        }
        public Variant _defaultValue { get; private set; }

        public Variable(string name, Variant value)
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
