using Arcweave.Interpreter.INodes;
using Godot;
using Godot.Collections;

namespace Arcweave.Project
{
    public partial class Attribute 
    {
        [Export] public string Id { get; set; }
        [Export] public string Name { get; private set; }
        [Export] public string containerId { get; private set; }

        [Export] private string _dataString;
        [Export] private Array<Component> _dataComponentList;

        [Export] public IAttribute.ContainerType containerType { get; private set; }

        [Export] public IAttribute.DataType Type { get; private set; }

        [Export] public Variant data
        {
            get
            {
                if ( Type == IAttribute.DataType.StringPlainText || Type == IAttribute.DataType.StringRichText ) { return _dataString;  }
                if ( Type == IAttribute.DataType.ComponentList) { return _dataComponentList; }
                return default(Variant);
            }
            private set { }
        }

        internal void Set(string id, string name, IAttribute.DataType type, object dataObject, IAttribute.ContainerType containerType, string containerId)
        {
            Id = id; Name = name; Type = type;
            if ( type == IAttribute.DataType.StringPlainText || type == IAttribute.DataType.StringRichText ) { _dataString = (string) dataObject; }
            if ( type == IAttribute.DataType.ComponentList) {  _dataComponentList = (Array<Component>) dataObject; }
            this.containerType = containerType;
            this.containerId = containerId;
        }

    }
}
