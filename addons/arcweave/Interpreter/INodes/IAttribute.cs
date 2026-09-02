namespace Arcweave.Interpreter.INodes
{
    public interface IAttribute
    {
        public enum DataType
        {
            Undefined = 0,
            StringPlainText = 1,
            StringRichText = 2,
            ComponentList = 3,
            AssetList = 4,
            Boolean = 5,
            Integer = 6,
            Float = 7,
        }

        public enum ContainerType
        {
            Undefined = 0,
            Component = 1,
            Element = 2,
            Board = 3,
        }

        public string Name { get; }

        public DataType Type { get; }

        public ContainerType containerType { get; }

        public string containerId { get; }

    }
}
