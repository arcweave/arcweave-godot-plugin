using System;
using Godot;

namespace Arcweave.Project;

public partial class Asset : GodotObject
{
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public string Path { get; set; }
    
    public enum AssetType
    {
        Undefined,
        Audio,
        Image,
        Icon,
    }
    public AssetType Type { get; }

    [Export] public string AssetTypeName
    {
        get => Type.ToString();
        set {}
    }
    
    public Asset(string id, string name, string path, AssetType type)
    {
        Id = id;
        Name = name;
        Path = path;
        Type = type;
    }
}