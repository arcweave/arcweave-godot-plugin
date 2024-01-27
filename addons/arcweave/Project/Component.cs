﻿using System.Collections.Generic;
using Arcweave.Interpreter.INodes;
using Godot;

namespace Arcweave.Project
{
    public partial class Component : GodotObject, IComponent
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public Variant Cover { get; private set; }

        public List<IAttribute> Attributes { get; private set; }

        public Component(string id, string name)
        {
            Id = id;
            Name = name;
            Attributes = new List<IAttribute>();
        }

        public Component(string id, string name, Variant cover)
        {
            Id = id;
            Name = name;
            Cover = cover;
            Attributes = new List<IAttribute>();
        }

        public void AddAttribute(IAttribute attribute)
        {
            Attributes.Add(attribute);
        }
    }
}