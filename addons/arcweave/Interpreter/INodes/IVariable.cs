using System;
#if GODOT
using Godot;
#endif

namespace Arcweave.Interpreter.INodes
{
    public interface IVariable
    {
        public string Name { get; set; }
        public string Id { get; }
#if GODOT
        Variant Value { get; }
        Variant DefaultValue { get; }
#else
        object Value { get; }
        object DefaultValue { get; }
#endif
        IHasVariables Parent { get; }
        public object ObjectValue { get; }

        System.Type Type { get; }

        public void ResetToDefaultValue();
    }
}
