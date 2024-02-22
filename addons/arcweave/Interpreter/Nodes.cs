#if GODOT
using Godot;
#endif

using Arcweave.Interpreter.INodes;

namespace Arcweave.Project
{
#if GODOT
        public partial class Attribute : GodotObject, IAttribute { }
        public partial class Board : GodotObject, IBoard { }
        public partial class Component : GodotObject, IComponent { }
        public partial class Connection : GodotObject, IConnection { }
        public partial class Element : GodotObject, IElement { }
        public partial class Project : GodotObject, IProject { }
        public partial class Variable : GodotObject, IVariable { }
        public partial class Options : GodotObject, IOptions { }
        public partial class Path : GodotObject, IPath { }
#else
        public partial class Attribute : IAttribute { }
        public partial class Board : IBoard { }
        public partial class Component : IComponent { }
        public partial class Connection : IConnection { }
        public partial class Element : IElement { }
        public partial class Project : IProject { }
        public partial class Variable : IVariable { }
        public partial class Options : IOptions { }
        public partial class Path : IPath { }
#endif
}

