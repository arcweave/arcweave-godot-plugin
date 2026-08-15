using System.Linq;
using Godot;
using Godot.Collections;

namespace Arcweave.Project
{
    public partial class Component
    {
        [Export] public string Id { get; private set; }
        [Export] public string CustomId { get; private set; }
        [Export] public string Name { get; private set; }
        [Export] public Asset Cover { get; private set; }

        [Export] public Array<Attribute> Attributes { get; private set; }
        [Export] public Array<Variable> Variables { get; private set; }

        public Component(string id, string name) : this(id, null, name) { }

        public Component(string id, string customId, string name)
        {
            Id = id;
            CustomId = customId;
            Name = name;
            Attributes = new Array<Attribute>();
            Variables = new Array<Variable>();
        }

        public Component(string id, string name, Asset cover) : this(id, null, name, cover) { }

        public Component(string id, string customId, string name, Asset cover)
        {
            Id = id;
            CustomId = customId;
            Name = name;
            Cover = cover;
            Attributes = new Array<Attribute>();
            Variables = new Array<Variable>();
        }

        /// <summary>
        /// Adds an attribute to the component
        /// </summary>
        /// <param name="attribute">The attribute</param>
        public void AddAttribute(Attribute attribute)
        {
            Attributes.Add(attribute);
        }

        public void AddVariable(Variable variable)
        {
            variable.Parent = this;
            Variables.Add(variable);
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
