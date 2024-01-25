using System.Collections.Generic;
using Arcweave.Interpreter.INodes;
using Godot;
using Godot.Collections;

namespace Arcweave.Project
{
	public partial class Board : GodotObject, IBoard
	{
		public string Id { get; private set; }
		public string CustomId { get; private set; }
		public string Name { get; private set; }
		public List<IElement> Elements { get; private set; }
		public List<Connection> Connections { get; private set; }
		public List<Note> Notes { get; private set; }
		public List<Jumper> Jumpers { get; private set; }
		public List<Branch> Branches { get; private set; }

		public Board(string id, string name, List<IElement> elements, List<Connection> connections, List<Jumper> jumpers, List<Branch> branches)
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
