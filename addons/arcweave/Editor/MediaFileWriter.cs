#if TOOLS
using System;
using System.IO;
using Godot;

namespace Arcweave.Editor;

/// <summary>
/// Writes the media downloaded by ArcweaveMediaDownloader without ever losing the previous file.
/// </summary>
[Tool]
public partial class MediaFileWriter : RefCounted
{
	/// <summary>
	/// Writes the file through a temporary file, so a failed write never leaves it truncated or deleted.
	/// Temporary files start with a dot, so the editor never imports them.
	/// </summary>
	/// <param name="path">The res:// path of the file</param>
	/// <param name="bytes">The file contents</param>
	/// <returns>An empty string on success, otherwise the error message</returns>
	public string Write(string path, byte[] bytes)
	{
		var target = ProjectSettings.GlobalizePath(path);
		var directory = System.IO.Path.GetDirectoryName(target) ?? "";
		var id = Guid.NewGuid().ToString("N");
		var temporary = System.IO.Path.Combine(directory, $".arcweave-{id}.tmp");
		var backup = System.IO.Path.Combine(directory, $".arcweave-{id}.bak");
		try
		{
			File.WriteAllBytes(temporary, bytes);
			if (File.Exists(target))
			{
				// Replaces the file in one step, keeping the previous file in the backup until it succeeds
				File.Replace(temporary, target, backup);
			}
			else
			{
				File.Move(temporary, target);
			}
			return "";
		}
		catch (Exception e)
		{
			// Eg. Windows renamed the previous file to the backup but could not move the new one in its place
			if (!File.Exists(target) && File.Exists(backup))
			{
				TryMove(backup, target);
			}
			return e.Message;
		}
		finally
		{
			TryDelete(temporary);
			if (File.Exists(target))
			{
				TryDelete(backup);
			}
		}
	}

	private static void TryMove(string source, string destination)
	{
		try
		{
			File.Move(source, destination);
		}
		catch (Exception e)
		{
			GD.PushWarning($"[Arcweave] Could not restore {destination} from {source}: {e.Message}");
		}
	}

	private static void TryDelete(string path)
	{
		try
		{
			if (File.Exists(path)) File.Delete(path);
		}
		catch (Exception e)
		{
			GD.PushWarning($"[Arcweave] Could not delete {path}: {e.Message}");
		}
	}
}
#endif
