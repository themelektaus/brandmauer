namespace Brandmauer;

public static class FileUtils
{
	public static void CreateFolder(string folder)
	{
		Directory.CreateDirectory(folder);
	}

	public static bool MoveFolder(string source, string target)
	{
		DeleteFolder(target);

		if (Directory.Exists(source))
		{
			Directory.Move(source, target);
			return true;
		}

		return false;
	}

	public static void CopyFolder(string source, string target)
	{
		if (!Directory.Exists(source))
			return;

		CreateFolder(target);

		foreach (var folder in GetAllFolders(source))
			CreateFolder(folder.Replace(source, target));

		foreach (var file in GetAllFiles(source))
			CopyFile(file, file.Replace(source, target));
	}

	public static void DeleteFolder(string folder)
	{
		if (Directory.Exists(folder))
			Directory.Delete(folder, true);
	}

	public static string[] GetAllFolders(string folder)
	{
		return Directory.GetDirectories(folder, "*", SearchOption.AllDirectories);
	}

	public static string[] GetAllFiles(string folder)
	{
		return Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
	}

	public static void CreateFile(string path)
	{
		if (!File.Exists(path))
			File.Create(path).Close();
	}

	public static void MoveFile(string source, string target)
	{
		if (File.Exists(source))
			File.Move(source, target, true);
	}

	public static void CopyFile(string source, string target)
	{
		File.Copy(source, target, true);
	}

	public static void DeleteFile(string path)
	{
		if (File.Exists(path))
			File.Delete(path);
	}
}
