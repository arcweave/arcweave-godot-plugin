namespace Arcweave.Interpreter.INodes
{
    public interface IAttribute
    {
        public enum DataType
        {
            Undefined,
            StringPlainText,
            StringRichText,
            ComponentList,
        }

        public enum ContainerType
        {
            Undefined,
            Component,
            Element,
        }

        public string Name { get; }

        public DataType Type { get; }

        public ContainerType containerType { get; }

        public string containerId { get; }

    }
}
