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
#else
        public object Value { get; set; }
        object _defaultValue { get; }
#endif
        System.Type Type { get; }

        string _typeName { get; }

        public void ResetToDefaultValue();
    }
}
