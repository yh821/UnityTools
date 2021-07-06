using System.IO;
using UnityEngine;

public class ProjectExtensionSettings
{
    public static string editorCustomPath = "ProjectSettings/ProjectExtensionSettings.EditorCustom.json";

    private static EditorCustom m_EditorCustom;

    public static EditorCustom editorCustom
    {
        get
        {
            if (m_EditorCustom == null)
            {
                m_EditorCustom = EditorCustom.LoadOrCreateIfNoExit(editorCustomPath);
            }
            return m_EditorCustom;
        }
    }
}

public class EditorCustom
{
    public string writablePath = "LocalFile";
    public string logPath = "LocalFile";
    public string cachePath = "Cache";

    public static EditorCustom LoadOrCreateIfNoExit(string path)
    {
        EditorCustom editorCustom;

        if (File.Exists(path))
        {
            using (var jsonReader = File.OpenText(path))
            {
                editorCustom = JsonUtility.FromJson<EditorCustom>(jsonReader.ReadToEnd());
            }
        }
        else
        {
            editorCustom = new EditorCustom();
            Save(path, editorCustom);
        }
        return editorCustom;
    }

    public static void Save(string path, EditorCustom editorCustom)
    {
        using (var jsonWritter = File.CreateText(path))
        {
            jsonWritter.Write(JsonUtility.ToJson(editorCustom, true));
        }
    }
}
