using Godot;
using Godot.Collections;

namespace Arcweave.Project
{
    public partial class Component
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public Variant Cover { get; private set; }

        public Array<Attribute> Attributes { get; private set; }

        public Component(string id, string name)
        {
            Id = id;
            Name = name;
            Attributes = new Array<Attribute>();
        }

        public Component(string id, string name, Variant cover)
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
    }
}
