namespace Arcweave.Interpreter.INodes
{
    public interface IVariable
    {
        public string Name { get; set; }
        public object Value { get; set; }
        System.Type Type { get; }

        string _typeName { get; }

        object _defaultValue { get; }
        public void ResetToDefaultValue();
    }
}
