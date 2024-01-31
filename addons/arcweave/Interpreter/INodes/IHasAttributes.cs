#if GODOT
using Godot.Collections;
#else
using System.Collections.Generic;
#endif

namespace Arcweave.Interpreter.INodes
{
    public interface IHasAttributes
    {
#if GODOT
        Array<Arcweave.Project.Attribute> Attributes { get; }
#else
        List<Arcweave.Project.Attribute> Attributes { get; }
#endif

        public void AddAttribute(Arcweave.Project.Attribute attribute);
    }
}
