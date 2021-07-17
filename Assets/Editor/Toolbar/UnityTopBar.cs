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
        if (GUILayout.Button(new GUIContent("提交", "提交unity工程svn"), GUILayout.Height(22)))
        {
            SVNHelper.CommitToSVN();
        }

        GUI.color = new Color(0.33f,0.75f,1f);
        if (GUILayout.Button(new GUIContent("更新", "更新unity工程svn"), GUILayout.Height(22)))
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

        if (GUILayout.Button(new GUIContent("空场景", "新建空场景"), GUILayout.Height(22)))
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

        GUI.color = Color.green;
        if (GUILayout.Button(new GUIContent("主场景", "启动游戏场景"), GUILayout.Height(22)))
        {
            EditorSceneManager.OpenScene("Assets/Scenes/main.unity");
        }
        GUI.color = Color.white;

        GUILayout.FlexibleSpace();
    }
}
