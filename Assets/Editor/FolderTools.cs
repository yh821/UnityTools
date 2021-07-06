using System.IO;

public static class FolderTools
{
    public static readonly int RED_LIMIT = (int)UnityEngine.Mathf.Pow(2, 21);
    public static readonly int ORANGE_LIMIT = (int)UnityEngine.Mathf.Pow(2, 20);
    public static readonly int YELLOW_LIMIT = (int)UnityEngine.Mathf.Pow(2, 19);

    static public void OpenFolder(string fullPath)
    {
        string path = fullPath.Replace("/", "\\");
        if (Directory.Exists(path))
            System.Diagnostics.Process.Start("explorer.exe", path);
    }

    static public void DeleteFolder(string fullPath)
    {
        string path = fullPath.Replace("/", "\\");
        if (Directory.Exists(path))
            Directory.Delete(path);
    }

    static public void OpenFile(string fullPath)
    {
        string path = fullPath.Replace("/", "\\");
        if (File.Exists(path))
            System.Diagnostics.Process.Start(path);
    }

    static public void SelectFile(string fullPath)
    {
        string path = fullPath.Replace("/", "\\");
        if (File.Exists(path))
            System.Diagnostics.Process.Start("explorer.exe", "/select," + path);
    }

    static public void DeleteFile(string fullPath)
    {
        string path = fullPath.Replace("/", "\\");
        if (File.Exists(path))
            File.Delete(path);
    }
}