﻿using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Jumper : GodotObject, INode
    {
        [Export] public string Id { get; private set; }

        [Export] public Project Project { get; private set; }

        [Export] private Element Target { get; set; }

        public Jumper(string id, Project project, Element target)
        {
            Id = id;
            Project = project;
            Target = target;
        }

        Path INode.ResolvePath(Path path)
        {
            return (Target as INode)?.ResolvePath(path) ?? Path.Invalid;
        }
    }
}
