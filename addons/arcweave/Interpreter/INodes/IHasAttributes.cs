using System.Collections.Generic;

namespace Arcweave.Interpreter.INodes
{
    public interface IHasAttributes
    {
        List<IAttribute> Attributes { get; }

        public void AddAttribute(IAttribute attribute);
    }
}
