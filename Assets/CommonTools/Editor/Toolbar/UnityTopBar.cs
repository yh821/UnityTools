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
	public const string ProjName = "UnityTools";

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
			foreach (var go in roots)
			{
				if (go.name.ToLower() == "main")
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
			_style = new GUIStyle("BoldLabel") {fontSize = 16};
		}

		if (GUILayout.Button(EditorGUIUtility.IconContent("SettingsIcon", "|svn路径配置"), GUILayout.Height(22)))
		{
			SVNHelper.OpenPathOption();
		}

		if (GUILayout.Button(new GUIContent("提交", "提交unity工程svn"), GUILayout.Height(22)))
		{
			SVNHelper.ExecuteCommand(SVNHelper.Command.Commit, SVNHelper.GetCommitPaths());
		}

		GUI.color = new Color(0.33f, 0.75f, 1f);
		if (GUILayout.Button(new GUIContent("更新", "更新unity工程svn"), GUILayout.Height(22)))
		{
			if (EditorApplication.isPlaying)
				EditorApplication.isPaused = true;
			SVNHelper.ExecuteCommand(SVNHelper.Command.Update, SVNHelper.GetUpdatePaths());
		}

		GUI.color = Color.white;

		GUILayout.FlexibleSpace();

		GUILayout.Label(ProjName, _style);
	}

	static void OnRightToolbarGUI()
	{
		if (EditorApplication.isPlaying)
			return;

		if (GUILayout.Button(new GUIContent("空场景", "新建空场景"), GUILayout.Height(22)))
		{
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
			var camera = new GameObject("Camera", typeof(Camera));
			camera.transform.position = new Vector3(0, 1, -10);
		}

		GUI.color = Color.green;
		if (GUILayout.Button(new GUIContent("主场景", "启动游戏场景"), GUILayout.Height(22)))
		{
			EditorSceneManager.OpenScene("Assets/Game/Scenes/main.unity");
		}

		if (_needCheckScene)
		{
			GUI.color = Color.red;
			if (GUILayout.Button(new GUIContent("检查场景", "检查场景"), GUILayout.Height(22)))
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
		foreach (var go in roots)
		{
			//删除第一层空节点
			if (go.transform.childCount == 0)
			{
				var com = go.GetComponentsInChildren<Component>();
				if (com.Length <= 1)
				{
					Debug.Log($"删除空节点: <color=yellow>{go.name}</color>");
					Object.DestroyImmediate(go);
					continue;
				}
			}

			//收集MissingPrefab的实例
			WalkNode(go.transform, missingPrefabList);
		}

		//清除MissingPrefab的实例
		foreach (var go in missingPrefabList)
		{
			Debug.Log($"删除MissingPrefab: <color=yellow>{go.ScenePath()}</color>");
			Object.DestroyImmediate(go);
		}

		//是否烘培
		if (Lightmapping.lightingDataAsset == null)
		{
			content.Append("缺少光照贴图, 检查是否已烘培!");
		}

		//寻路网格
		var triangulation = NavMesh.CalculateTriangulation();
		if (triangulation.vertices.Length <= 0)
		{
			content.Append("缺少寻路网格, 检查是否已烘培!");
		}

		//环境光
		var lightColor = RenderSettings.ambientLight;
		var grayLevel = lightColor.r * 0.299f + lightColor.g * 0.587f + lightColor.b * 0.114f;
		if (grayLevel < 0.75f)
		{
			RenderSettings.ambientLight = Color.white;
			content.Append("已将环境光改为白色!");
		}

		if (content.Length > 0)
		{
			Debug.LogError(content.ToString());
			EditorUtility.DisplayDialog("错误", content.ToString(), "确定");
		}
		else
		{
			Debug.Log("场景检查完成");
		}

		EditorSceneManager.SaveScene(scene);
	}

	private static void WalkNode(Transform parent, List<GameObject> missingList)
	{
		if (PrefabUtility.IsPrefabAssetMissing(parent.gameObject))
		{
			missingList.Add(parent.gameObject);
			return;
		}

		if (parent.childCount == 0)
			return;
		for (int i = 0, len = parent.childCount; i < len; i++)
		{
			WalkNode(parent.GetChild(i), missingList);
		}
	}
}