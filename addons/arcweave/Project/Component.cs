using System.Linq;
using Godot;
using Godot.Collections;

namespace Arcweave.Project
{
    public partial class Component
    {
        [Export] public string Id { get; private set; }
        [Export] public string Name { get; private set; }
        [Export] public Asset Cover { get; private set; }

        [Export] public Array<Attribute> Attributes { get; private set; }

        public Component(string id, string name)
        {
            Id = id;
            Name = name;
            Attributes = new Array<Attribute>();
        }

        public Component(string id, string name, Asset cover)
        {
            Id = id;
            Name = name;
            Cover = cover;
            Attributes = new Array<Attribute>();
        }

        public void AddAttribute(Attribute attribute)
        {
            Attributes.Add(attribute);
        }

        public Attribute GetAttribute(string attributeName)
        {
            return Attributes.First(attribute => attribute.Name == attributeName);
        }
    }
}
