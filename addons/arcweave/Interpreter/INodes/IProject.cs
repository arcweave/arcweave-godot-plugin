#if GODOT
using Godot.Collections;
#else
using System.Collections.Generic;
#endif
using Arcweave.Project;

namespace Arcweave.Interpreter.INodes
{
    public interface IProject
    {
#if GODOT
        public Array<Arcweave.Project.Variable> Variables { get; }
        public Dictionary<string, Arcweave.Project.Board> Boards { get; }
#else
        public List<Arcweave.Project.Variable> Variables { get; }
        public List<Arcweave.Project.Board> Boards { get; }
#endif
        public Arcweave.Project.Element ElementWithId(string id);

        public Arcweave.Project.Variable GetVariable(string name);
    }
}
