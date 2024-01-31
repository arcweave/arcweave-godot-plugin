using System.Collections.Generic;

namespace Arcweave.Interpreter.INodes
{
    public interface IHasAttributes
    {
        List<Arcweave.Project.Attribute> Attributes { get; }

        public void AddAttribute(Arcweave.Project.Attribute attribute);
    }
}
