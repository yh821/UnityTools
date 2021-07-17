using Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;

[InitializeOnLoad]
public class UnityTopBar
{
    static UnityTopBar()
    {
        ToolbarExtender.LeftToolbarGUI.Add(OnLeftToolbarGUI);
        ToolbarExtender.RightToolbarGUI.Add(OnRightToolbarGUI);
    }

    static void OnLeftToolbarGUI()
    {
        if (GUILayout.Button(new GUIContent("�ύ", "�ύunity����svn"), GUILayout.Height(22)))
        {
            SVNHelper.CommitToSVN();
        }

        GUI.color = new Color(0.33f,0.75f,1f);
        if (GUILayout.Button(new GUIContent("����", "����unity����svn"), GUILayout.Height(22)))
        {
            if (EditorApplication.isPlaying)
                EditorApplication.isPaused = true;
            SVNHelper.UpdateFromSVN();
        }
        GUI.color = Color.white;

        GUILayout.FlexibleSpace();
    }

    static void OnRightToolbarGUI()
    {
        if (EditorApplication.isPlaying)
            return;

        if (GUILayout.Button(new GUIContent("�ճ���", "�½��ճ���"), GUILayout.Height(22)))
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        GUI.color = Color.green;
        if (GUILayout.Button(new GUIContent("������", "������Ϸ����"), GUILayout.Height(22)))
        {
            EditorSceneManager.OpenScene("Assets/Scenes/main.unity");
        }
        GUI.color = Color.white;

        GUILayout.FlexibleSpace();
    }
}
