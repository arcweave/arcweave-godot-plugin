using System.Collections.Generic;

namespace Arcweave.Interpreter.INodes
{
    public interface IElement : INode, IHasAttributes
    {
        public int Visits { get; set; }

        public string Title { get; }
        public string Content { get; }

        public List<IConnection> Outputs { get; }

        public string GetRuntimeContent();

        public IOptions GetOptions();
    }
}
