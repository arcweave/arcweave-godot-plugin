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

		public Board(string id, string name, Array<Element> elements, Array<Connection> connections, Array<Jumper> jumpers, Array<Branch> branches)
		{
			Id = id;
			Name = name;
			Elements = elements;
			Connections = connections;
			Jumpers = jumpers;
			Branches = branches;
		}

		public Array<Element> GetElements()
		{
			var elements = new Array<Element>();
			foreach (var element in Elements)
			{
				elements.Add(element as Element);
			}
			return elements;
		}
		
		public Array<Connection> GetConnections()
		{
			var connections = new Array<Connection>();
			foreach (var connection in Connections)
			{
				connections.Add(connection);
			}
			return connections;
		}

		public Array<Jumper> GetJumpers()
		{
			var jumpers = new Array<Jumper>();
			foreach (var jumper in Jumpers)
			{
				jumpers.Add(jumper);
			}
			return jumpers;
		}

		public Array<Branch> GetBranches()
		{
			var branches = new Array<Branch>();
			foreach (var branch in Branches)
			{
				branches.Add(branch);
			}

			return branches;
		}
	}
}
