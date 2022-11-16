using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public partial class PrefabTools : EditorWindow
{
    private Font mInputFont = null;
    private Font mReplaceFont = null;

    private Dictionary<string, SpriteData> mFontDataMap = new Dictionary<string, SpriteData>();
    private List<GameObject> mFontReference = new List<GameObject>();

    private Vector2 mFontScroller = Vector2.zero;

	void OnGUI_FindFont()
    {
        EditorGUIUtility.labelWidth = 64;

        mPathReorderableList?.DoLayoutList();

        EditorGUILayout.BeginHorizontal();
        {
	        mInputFont = EditorGUILayout.ObjectField("查找的字体", mInputFont, typeof(Font), false) as Font;
			GUILayout.Space(30);
	        mReplaceFont = EditorGUILayout.ObjectField("替换的字体", mReplaceFont, typeof(Font), false) as Font;
        }
        EditorGUILayout.EndHorizontal();

		GUILayout.Space(SPACE);
		if (GUILayout.Button("开始查找"))
		{
			if (mInputFont != null)
				StartFindFontReference();
			else
				ShowNotification(new GUIContent("请设置查找的字体"));
		}

		if (mFontDataMap.Count > 0)
		{
			mFontScroller = EditorGUILayout.BeginScrollView(mFontScroller);
            DrawFontScrollView();
            EditorGUILayout.EndScrollView();

			if (mReplaceFont != null)
			{
				EditorGUILayout.BeginHorizontal();
				{
					if (GUILayout.Button("全部替换"))
					{
						if (EditorUtility.DisplayDialog("提示", "全部替换(二次确认)", "确定", "取消"))
							ReplaceAllFont();
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			else
			{
				EditorGUILayout.LabelField($"总数:{mFontDataMap.Count}", GUILayout.Width(64));
            }
        }
    }

	private void DrawFontScrollView()
	{
        foreach (var data in mFontDataMap.Values)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("编辑/退出", GUILayout.MaxWidth(64)))
                {
                    var stage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (stage == null || stage.prefabAssetPath != data.path)
                        AssetDatabase.OpenAsset(AssetDatabase.LoadMainAssetAtPath(data.path));
                    else
                        StageUtility.GoToMainStage();
                }
                if (GUILayout.Button("克隆/删除", GUILayout.MaxWidth(64)))
                {
                    var prefab = AssetDatabase.LoadMainAssetAtPath(data.path);
                    if (prefab != null)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(data.path);
                        var target = GetSceneObject(fileName);
                        if (target == null)
                        {
                            var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                            go.transform.SetParent(TempParent, false);
                            go.name = fileName;
                        }
                        else
                        {
                            DestroyImmediate(target);
                        }
                    }
                }

                EditorGUILayout.ObjectField(data.prefab, data.prefab.GetType(), false, GUILayout.MaxWidth(200));

                data.foldout = EditorGUILayout.Foldout(data.foldout, "Children:" + data.children.Count, true);

                if (mReplaceFont != null)
                {
                    if (GUILayout.Button("SetNative", GUILayout.MaxWidth(70)))
                    {
                        var stage = GetPrefabStage(data);
                        var go = stage.prefabContentsRoot;
                        foreach (var path in data.children)
                        {
                            var trans = path == go.name ? go.transform : go.transform.Find(path);
                            var image = trans.GetComponent<Image>();
                            if (image != null)
                                image.SetNativeSize();
                        }
                        EditorSceneManager.MarkSceneDirty(stage.scene);
                    }
                }

                if (mReplaceFont != null)
                {
                    if (GUILayout.Button("替换", GUILayout.MaxWidth(64)))
                    {
                        var stage = GetPrefabStage(data);
                        ReplaceFont(stage.prefabContentsRoot, data.children);
                        EditorSceneManager.MarkSceneDirty(stage.scene);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (data.foldout)
            {
                foreach (var path in data.children)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("", GUILayout.MaxWidth(21)); //空白
                        if (GUILayout.Button("选中", GUILayout.MaxWidth(40)))
                        {
                            var stage = GetPrefabStage(data);
                            var go = stage.prefabContentsRoot;
                            var trans = path == go.name ? go.transform : go.transform.Find(path);
                            if (trans != null)
                                Selection.activeObject = trans.gameObject;
                        }
                        EditorGUILayout.LabelField(path);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }

    private void StartFindFontReference()
	{
		mFontDataMap.Clear();
		SaveCommitPath();
		//var dirs = Directory.GetFiles(mFindPath, "*.prefab", SearchOption.AllDirectories);
		//for (int i = 0; i < dirs.Length; i++)
		var paths = CommitPathList.ToArray();
		if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
		{
			ShowNotification(new GUIContent("请设置路径"));
			return;
		}
		var guids = AssetDatabase.FindAssets("t:prefab", paths);
		for (int i = 0, len = guids.Length; i < len; i++)
		{
			//var path = "Assets" + dirs[i].Replace(Application.dataPath, "").Replace('\\','/');
			var path = AssetDatabase.GUIDToAssetPath(guids[i]);
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (prefab != null)
			{
				var data = new SpriteData(path, prefab);
				if (!mFontDataMap.ContainsKey(path) && GetAllFontPath(prefab, ref data.children))
					mFontDataMap.Add(path, data);
			}
			EditorUtility.DisplayProgressBar("查找索引...", path, (float)i / len);
		}
		EditorUtility.ClearProgressBar();
    }

	bool GetAllFontPath(GameObject mainAsset, ref List<string> paths)
	{
		mFontReference.Clear();

		var texts = mainAsset.GetComponentsInChildren<Text>(true);
		foreach (var text in texts)
		{
			if (text.font != null && text.font == mInputFont && !mFontReference.Contains(text.gameObject))
				mFontReference.Add(text.gameObject);
		}

		foreach (var go in mFontReference)
			paths.Add(GetPath(go));

		return mFontReference.Count > 0;
	}

	private void ReplaceAllFont()
	{
		if (mReplaceFont == null)
			return;
		foreach (var data in mFontDataMap.Values)
		{
			var go = PrefabUtility.InstantiatePrefab(data.prefab) as GameObject;
			ReplaceFont(go, data.children);
			PrefabUtility.SaveAsPrefabAsset(go, data.path);
			DestroyImmediate(go);
		}
		AssetDatabase.SaveAssets();
	}

	private void ReplaceFont(GameObject go, List<string> children)
	{
		if (mReplaceFont == null)
			return;
		foreach (var path in children)
		{
			var trans = path == go.name ? go.transform : go.transform.Find(path);
			var text = trans.GetComponent<Text>();
			if (text != null)
			{
				text.font = mReplaceFont;
			}
		}
	}

}