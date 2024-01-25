using System.Collections.Generic;

namespace Arcweave.Interpreter.INodes
{
    public interface IProject
    {
        public Dictionary<string, IVariable> Variables { get; }

        public IElement ElementWithId(string id);

        public object GetVariable(string name);
    }
}
