#if GODOT
using Godot;
#endif

namespace Arcweave.Interpreter.INodes
{
    public interface IVariable
    {
        public string Name { get; set; }
#if GODOT
        
        Variant Value { get; }
        Variant _defaultValue { get; }
#else
        object Value { get; }
        object _defaultValue { get; }
#endif
        public object ObjectValue { get; }

        System.Type Type { get; }
        string _typeName { get; }

        public void ResetToDefaultValue();
    }
}
