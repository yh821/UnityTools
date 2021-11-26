using System.Collections.Generic;
using Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityToolbarExtender;

[InitializeOnLoad]
public class UnityTopBar
{
    public const string ProjName = "S1:uc03_cn";

    private static GUIStyle _style;
    private static bool _needCheckScene = false;

    static UnityTopBar()
    {
        ToolbarExtender.LeftToolbarGUI.Add(OnLeftToolbarGUI);
        ToolbarExtender.RightToolbarGUI.Add(OnRightToolbarGUI);
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.newSceneCreated += OnSceneCreated;
    }

    static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        _needCheckScene = scene.path.StartsWith("Assets/Game/Scenes/Map/");
        if (_needCheckScene)
        {
            var roots = scene.GetRootGameObjects();
            foreach(var go in roots)
            {
                if(go.name.ToLower() == "main")
                {
                    return;
                }
            }
            _needCheckScene = false;
        }
    }

    static void OnSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
    {
        _needCheckScene = false;
    }

    static void OnLeftToolbarGUI()
    {
        if (_style == null)
        {
            _style = new GUIStyle("BoldLabel") { fontSize = 16 };
        }

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

        GUILayout.Label(ProjName, _style);
    }

    static void OnRightToolbarGUI()
    {
        if (EditorApplication.isPlaying)
            return;

        if (GUILayout.Button(new GUIContent("�ճ���", "�½��ճ���"), GUILayout.Height(22)))
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var camera = new GameObject("Camera", typeof(Camera));
            camera.transform.position = new Vector3(0, 1, -10);
        }

        GUI.color = Color.green;
        if (GUILayout.Button(new GUIContent("������", "������Ϸ����"), GUILayout.Height(22)))
        {
            EditorSceneManager.OpenScene("Assets/Scenes/main.unity");
        }

        if (_needCheckScene)
        {
            GUI.color = Color.red;
            if (GUILayout.Button(new GUIContent("��鳡��", "��鳡��"), GUILayout.Height(22)))
            {
                SceneCheckList();
            }
        }

            GUI.color = Color.white;
        GUILayout.FlexibleSpace();
    }

    private static void SceneCheckList()
    {
        var content = new System.Text.StringBuilder();
        var scene = SceneManager.GetActiveScene();

        var missingPrefabList = new List<GameObject>();
        var roots = scene.GetRootGameObjects();
        foreach(var go in roots)
        {
            //ɾ����һ��սڵ�
            if (go.transform.childCount == 0)
            {
                var com = go.GetComponentsInChildren<Component>();
                if (com.Length <= 1)
                {
                    Debug.Log($"ɾ���սڵ�: <color=yellow>{go.name}</color>");
                    Object.DestroyImmediate(go);
                    continue;
                }
            }

            //�ռ�MissingPrefab��ʵ��
            WalkNode(go.transform, missingPrefabList);
        }

        //���MissingPrefab��ʵ��
        foreach(var go in missingPrefabList)
        {
            Debug.Log($"ɾ��MissingPrefab: <color=yellow>{FileHelper.GetSceneGameObjectPath(go)}</color>");
            Object.DestroyImmediate(go);
        }

        //�Ƿ����
        if(Lightmapping.lightingDataAsset == null)
        {
            content.Append("ȱ�ٹ�����ͼ, ����Ƿ��Ѻ���!");
        }

        //Ѱ·����
        var triangulation = NavMesh.CalculateTriangulation();
        if(triangulation.vertices.Length <= 0)
        {
            content.Append("ȱ��Ѱ·����, ����Ƿ��Ѻ���!");
        }

        //������
        var lightColor = RenderSettings.ambientLight;
        var grayLevel = lightColor.r * 0.299f + lightColor.g * 0.587f + lightColor.b * 0.114f;
        if (grayLevel < 0.75f)
        {
            RenderSettings.ambientLight = Color.white;
            content.Append("�ѽ��������Ϊ��ɫ!");
        }

        if (content.Length > 0)
        {
            Debug.LogError(content.ToString());
            EditorUtility.DisplayDialog("����", content.ToString(), "ȷ��");
        }
        else
        {
            Debug.Log("����������");
        }

        EditorSceneManager.SaveScene(scene);
    }

    private static void WalkNode(Transform parent, List<GameObject> missingList)
    {
        if(PrefabUtility.IsPrefabAssetMissing(parent.gameObject))
        {
            missingList.Add(parent.gameObject);
            return;
        }
        if (parent.childCount == 0)
            return;
        for(int i = 0, len = parent.childCount; i < len; i++)
        {
            WalkNode(parent.GetChild(i), missingList);
        }
    }

}
