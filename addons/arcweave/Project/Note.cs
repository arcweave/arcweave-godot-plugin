using Godot;

namespace Arcweave.Project
{
    public partial class Note : GodotObject
    {
        [Export] public string Content;
        [Export] public string Theme;

        public Note(string content, string theme)
        {
            Content = content;
            Theme = theme;
        }
    }
}
