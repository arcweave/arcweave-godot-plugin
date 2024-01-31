using System.Collections.Generic;
using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Attribute 
    {
        public string Id { get; set; }
        public string Name { get; private set; }

        public string containerId { get; private set; }

        private string _dataString;
        private List<Component> _dataComponentList;

        public IAttribute.ContainerType containerType { get; private set; }

        public IAttribute.DataType Type { get; private set; }

        public object data
        {
            get
            {
                if ( Type == IAttribute.DataType.StringPlainText || Type == IAttribute.DataType.StringRichText ) { return _dataString;  }
                if ( Type == IAttribute.DataType.ComponentList) { return _dataComponentList; }
                return null;
            }
        }

        internal void Set(string id, string name, IAttribute.DataType type, object dataObject, IAttribute.ContainerType containerType, string containerId)
        {
            Id = id; Name = name; Type = type;
            if ( type == IAttribute.DataType.StringPlainText || type == IAttribute.DataType.StringRichText ) { _dataString = (string) dataObject; }
            if ( type == IAttribute.DataType.ComponentList) {  _dataComponentList = (List<Component>) dataObject; }
            this.containerType = containerType;
            this.containerId = containerId;
        }

    }
}
