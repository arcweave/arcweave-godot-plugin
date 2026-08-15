using Arcweave.Interpreter;
using Arcweave.Interpreter.INodes;
using Godot;
using Godot.Collections;

namespace Arcweave.Project
{
    public partial class Attribute 
    {
        [Export] public string Id { get; set; }
        [Export] public string CustomId { get; private set; }
        [Export] public string Name { get; private set; }
        [Export] public string containerId { get; private set; }

        [Export] private string _dataString;
        [Export] private string _dataStringRichText;
        [Export] private string _dataStringRichTextRaw;
        [Export] private Array<Component> _dataComponentList;
        [Export] private Array<Asset> _dataAssetList;
        [Export] private bool _dataBoolean;
        [Export] private int _dataInteger;
        [Export] private double _dataFloat;

        private Project _project;

        [Export] public IAttribute.ContainerType containerType { get; private set; }

        [Export] public IAttribute.DataType Type { get; private set; }

        [Export] public Variant data
        {
            get
            {
                if ( Type == IAttribute.DataType.StringPlainText) { return _dataString;  }

                if (Type == IAttribute.DataType.StringRichText)
                {
                    if (string.IsNullOrEmpty(_dataStringRichText))
                    {
                        var i = new AwInterpreter(_project);
                        var output = i.RunScript(_dataStringRichTextRaw);
                        _dataStringRichText = Utils.CleanString(output.Output);
                        return _dataStringRichText;
                    }

                    return _dataStringRichText;
                }
                if ( Type == IAttribute.DataType.ComponentList) { return _dataComponentList; }
                if ( Type == IAttribute.DataType.AssetList) { return _dataAssetList; }
                if ( Type == IAttribute.DataType.Boolean) { return _dataBoolean; }
                if ( Type == IAttribute.DataType.Integer) { return _dataInteger; }
                if ( Type == IAttribute.DataType.Float) { return _dataFloat; }
                return default(Variant);
            }
            private set { }
        }

        public Attribute(Project project)
        {
            _project = project;
        }

        internal void Set(string id, string customId, string name, IAttribute.DataType type, object dataObject, IAttribute.ContainerType containerType, string containerId)
        {
            Id = id; CustomId = customId; Name = name; Type = type;
            if ( type == IAttribute.DataType.StringPlainText) { _dataString = (string) dataObject; }
            if ( type == IAttribute.DataType.StringRichText) { _dataStringRichTextRaw = (string) dataObject; }
            if ( type == IAttribute.DataType.ComponentList) {  _dataComponentList = (Array<Component>) dataObject; }
            if ( type == IAttribute.DataType.AssetList) { _dataAssetList = (Array<Asset>) dataObject; }
            if ( type == IAttribute.DataType.Boolean) { _dataBoolean = (bool) dataObject; }
            if ( type == IAttribute.DataType.Integer) { _dataInteger = (int) dataObject; }
            if ( type == IAttribute.DataType.Float) { _dataFloat = (double) dataObject; }
            this.containerType = containerType;
            this.containerId = containerId;
        }

    }
}
