#if GODOT
using Godot;
using Godot.Collections;
#else
using System.Collections.Generic;
#endif

namespace Arcweave.Interpreter.INodes
{
#if GODOT
    using VariableType = Variant;
#else
    using VariableType = object;
#endif

    public interface IProject
    {
        public Dictionary<string, Arcweave.Project.Variable> Variables { get; }

        public Arcweave.Project.Element ElementWithId(string id);

        public VariableType GetVariable(string name);
    }
}
