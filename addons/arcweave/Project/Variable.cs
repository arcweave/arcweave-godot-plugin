using Godot;

namespace Arcweave.Project
{
    public partial class Variable
    {
        [Signal] public delegate void VariableUpdatedEventHandler(Variant oldValue, Variant newValue);
        public string Name { get; set; }
        public bool Changed;
        private Variant _value;

        public Variant Value
        {
            get => _value;
            internal set
            {
                _value = value;
                Changed = true;
            }
        }

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

        private string _typeName { get; set; }
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
        private Variant _defaultValue { get; set; }

        public Variable(string name, Variant value)
        {
            Name = name;
            Value = value;
            _defaultValue = value;
            this._typeName = value.GetType().FullName;
            Changed = false;
        }

        /// <summary>
        /// Resets the variable to the default value.
        /// </summary>
        public void ResetToDefaultValue()
        {
            Value = _defaultValue;
        }
    }
}
