#if GODOT
using Godot.Collections;
#else
using System.Collections.Generic;
#endif
namespace Arcweave.Interpreter.INodes
{
    public interface IElement : INode, IHasAttributes
    {
        public int Visits { get; set; }

        public string Title { get; }
        public string RawContent { get; }

#if GODOT
        public Array<Arcweave.Project.Connection> Outputs { get; }
#else
        public List<Arcweave.Project.Connection> Outputs { get; }
#endif
        public void RunContentScript();

        public Arcweave.Project.Options GetOptions();
    }
}
