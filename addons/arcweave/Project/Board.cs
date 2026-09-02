using Godot;
using Godot.Collections;

namespace Arcweave.Project
{
	public partial class Board
	{
		[Export] public string Id { get; private set; }
		[Export] public string CustomId { get; private set; }
		[Export] public string Name { get; private set; }
		[Export] public Array<Element> Elements { get; private set; }
		[Export] public Array<Connection> Connections { get; private set; }
		[Export] public Array<Note> Notes { get; private set; }
		[Export] public Array<Jumper> Jumpers { get; private set; }
		[Export] public Array<Branch> Branches { get; private set; }
		[Export] public Array<Attribute> Attributes { get; private set; }
		[Export] public Array<Variable> Variables { get; private set; }

		public Board(string id, string name, string customId, Array<Element> elements, Array<Connection> connections, Array<Jumper> jumpers, Array<Branch> branches, Array<Note> notes)
		{
			Id = id;
			Name = name;
			CustomId = customId;
			Elements = elements;
			Connections = connections;
			Jumpers = jumpers;
			Branches = branches;
			Notes = notes;
			Attributes = new Array<Attribute>();
			Variables = new Array<Variable>();
		}

		public void AddAttribute(Attribute attribute)
		{
			Attributes.Add(attribute);
		}

		public void AddVariable(Variable variable)
		{
			variable.Parent = this;
			Variables.Add(variable);
		}
	}
}
