using Godot;

namespace Arcweave.Project
{
    public partial class Variable
    {
        public string Name { get; set; }

        public Variant Value { get; set; }
        
        public object ObjectValue {
            get
            {
                if (Value.VariantType == Variant.Type.String) return Value.AsString();
                if (Value.VariantType == Variant.Type.Bool) return Value.AsBool();
                if (Value.VariantType == Variant.Type.Int) return Value.AsInt32();
                if (Value.VariantType == Variant.Type.Float) return Value.AsDouble();
                return null;
            }
        }


        public string _typeName { get; private set; }
        public System.Type Type {
            get
            {
                if (Value.VariantType == Variant.Type.String) return typeof(string);
                if (Value.VariantType == Variant.Type.Int) return typeof(int);
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
