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

        /// <summary>
        /// Adds an attribute to the component
        /// </summary>
        /// <param name="attribute">The attribute</param>
        public void AddAttribute(Attribute attribute)
        {
            Attributes.Add(attribute);
        }

        /// <summary>
        /// Returns the attribute based on the attribute name
        /// </summary>
        /// <param name="attributeName">The attribute name</param>
        /// <returns>The attribute or null if not found</returns>
        public Attribute GetAttribute(string attributeName)
        {
            try
            {
                return Attributes.First(attribute => attribute.Name == attributeName);
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }
        }
    }
}
