#if GODOT
using Godot;
#endif

namespace Arcweave.Interpreter.INodes
{
    public interface IVariable
    {
        public string Name { get; set; }
#if GODOT
        
        public Variant Value { get; set; }
        Variant _defaultValue { get; }
        Variant.Type Type { get; }
#else
        public object Value { get; set; }
        object _defaultValue { get; }
        System.Type Type { get; }
#endif

        string _typeName { get; }

        public void ResetToDefaultValue();
    }
}
