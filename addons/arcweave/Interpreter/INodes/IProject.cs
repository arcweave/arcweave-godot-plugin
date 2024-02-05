#if GODOT
using Godot;
using Godot.Collections;
#else
using System.Collections.Generic;
#endif

namespace Arcweave.Interpreter.INodes
{
    public interface IProject
    {
        public Dictionary<string, Arcweave.Project.Variable> Variables { get; }

        public Arcweave.Project.Element ElementWithId(string id);

        public Arcweave.Project.Variable GetVariable(string name);
    }
}
