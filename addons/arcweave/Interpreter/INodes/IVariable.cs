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
        Variant DefaultValue { get; }
#else
        object Value { get; }
        object DefaultValue { get; }
#endif
        public object ObjectValue { get; }

        System.Type Type { get; }

        public void ResetToDefaultValue();
    }
}
