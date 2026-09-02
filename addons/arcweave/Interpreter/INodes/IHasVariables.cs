#if GODOT
using Godot.Collections;
#else
using System.Collections.Generic;
#endif
using System;

namespace Arcweave.Interpreter.INodes
{
    public interface IHasVariables
    {
#if GODOT
        Array<Arcweave.Project.Variable> Variables { get; }
#else
        List<Arcweave.Project.Variable> Variables { get; }
#endif
        public string CustomId { get; }
        public void AddVariable(Arcweave.Project.Variable variable);
    }
}
