using System;
using Godot;

namespace Arcweave.Project;

public partial class Asset : GodotObject
{
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public string Path { get; set; }
    /// <summary>
    /// The res:// path of the file downloaded from Arcweave's web API, or an empty string if it was not downloaded.
    /// </summary>
    [Export] public string LocalPath { get; set; } = "";
    
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

    /// <summary>
    /// Loads the file downloaded from Arcweave's web API, eg. a Texture2D for images or an AudioStream for audio.
    /// </summary>
    /// <returns>The loaded resource, or null if the asset was not downloaded</returns>
    public Resource LoadResource()
    {
        if (string.IsNullOrEmpty(LocalPath) || !ResourceLoader.Exists(LocalPath))
        {
            return null;
        }
        return ResourceLoader.Load(LocalPath);
    }
}
