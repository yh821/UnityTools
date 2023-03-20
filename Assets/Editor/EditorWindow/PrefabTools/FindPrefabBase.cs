using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

public abstract class FindPrefabBase
{
	protected PrefabTools mWin;

	protected FindPrefabBase(PrefabTools win)
	{
		mWin = win;
	}

	protected class PrefabData
	{
		public string path;
		public GameObject prefab;
		public List<string> children;
		public bool foldout;

		public PrefabData(string path, GameObject prefab)
		{
			this.path = path;
			this.prefab = prefab;
			children = new List<string>();
			foldout = false;
		}
	}

	protected Dictionary<string, PrefabData> mPrefabDatas = new Dictionary<string, PrefabData>();
	protected List<GameObject> mPrefabChildren = new List<GameObject>();

	protected Vector2 mPrefabScroller = Vector2.zero;

	public virtual void OnGUI()
	{
		DrawPrefabScrollBefore();

		if (GUILayout.Button("开始查询"))
		{
			StartFindPrefab();
		}

		if (mPrefabDatas.Count > 0)
		{
			DrawPrefabScroll(mPrefabDatas);
			DrawPrefabScrollAfter();
		}
	}

	protected virtual void StartFindPrefab()
	{
		mPrefabDatas.Clear();
		mWin.SavePrefabPaths();
		//var dirs = Directory.GetFiles(mFindPath, "*.prefab", SearchOption.AllDirectories);
		//for (int i = 0; i < dirs.Length; i++)
		var paths = mWin.GetPrefabPaths();
		if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
		{
			mWin.ShowNotification(new GUIContent("请设置路径"));
			return;
		}

		var guids = AssetDatabase.FindAssets("t:prefab", paths);
		for (int i = 0, len = guids.Length; i < len; i++)
		{
			//var path = "Assets" + dirs[i].Replace(Application.dataPath, "").Replace('\\','/');
			//var path = FileUtil.GetProjectRelativePath(path);//绝对路径=>相对路径
			var path = AssetDatabase.GUIDToAssetPath(guids[i]);
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (prefab != null)
			{
				var data = new PrefabData(path, prefab);
				if (!mPrefabDatas.ContainsKey(path) && FindChildrenPath(prefab, ref data.children))
					mPrefabDatas.Add(path, data);
			}

			EditorUtility.DisplayProgressBar("查找索引...", path, (float) i / len);
		}

		EditorUtility.ClearProgressBar();
	}

	protected virtual void DrawPrefabScrollBefore()
	{
	}

	protected virtual void DrawPrefabScroll(Dictionary<string, PrefabData> prefabDatas)
	{
		mPrefabScroller = EditorGUILayout.BeginScrollView(mPrefabScroller);
		foreach (var data in prefabDatas.Values)
		{
			EditorGUILayout.BeginHorizontal();
			{
				data.foldout = EditorGUILayout.Foldout(data.foldout, "Children:" + data.children.Count, true);
#if UNITY_2018_3_OR_NEWER
				if (GUILayout.Button("编辑/退出", GUILayout.MaxWidth(64)))
				{
					var stage = PrefabStageUtility.GetCurrentPrefabStage();
					if (stage == null || stage.prefabAssetPath != data.path)
						AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(data.path));
					else
						StageUtility.GoToMainStage();
				}
#else
                if (GUILayout.Button("克隆/删除", GUILayout.MaxWidth(64)))
                {
                    var prefab = AssetDatabase.LoadMainAssetAtPath(data.path);
                    if (prefab != null)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(data.path);
                        var target = mWin.GetSceneObject(fileName);
                        if (target == null)
                        {
                            var obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                            obj.transform.SetParent(mWin.TempParent, false);
                            obj.name = fileName;
                        }
                        else
                        {
                            Object.DestroyImmediate(target);
                        }
                    }
                }
#endif
				EditorGUILayout.ObjectField(data.prefab, data.prefab.GetType(), false, GUILayout.MaxWidth(200));
			}
			EditorGUILayout.EndHorizontal();
		}

		EditorGUILayout.EndScrollView();
	}

	protected virtual void DrawPrefabScrollAfter()
	{
	}

	protected virtual bool FindChildrenPath(GameObject prefab, ref List<string> paths)
	{
		return true;
	}
}