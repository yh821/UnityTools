using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class FindPrefabFont : FindPrefabBase
{
	public FindPrefabFont(PrefabTools win) : base(win)
	{
	}

	private class FontFilter
	{
		public Font font;
		public Font last_font;
		public string guid;
		public bool size_toggle;
		public int size;
		public bool line_toggle;
		public float lineSpring;
		public bool color_toggle;
		public Color color;
	}

	private FontFilter mInputFont = new FontFilter();
	private FontFilter mReplaceFont = new FontFilter();

	public override void OnGUI()
	{
		EditorGUILayout.BeginHorizontal();
		{
			DrawFontFilter("查找的字体", mInputFont);
			GUILayout.Space(5);
			DrawFontFilter("替换的字体", mReplaceFont);
		}
		EditorGUILayout.EndHorizontal();

		//if(mIsPreviewMode)
		//	EditorGUILayout.HelpBox("预制里路径相同的节点会选中错误，仅供预览，全部替换请使用非预览模式。", MessageType.Warning);
		//else
		//	EditorGUILayout.HelpBox("当前使用非预览模式。", MessageType.Info);
		EditorGUILayout.BeginHorizontal();
		{
			//mIsPreviewMode = GUILayout.Toggle(mIsPreviewMode, "预览模式", GUILayout.Width(70));
			if (GUILayout.Button("开始查找"))
			{
				if (mInputFont.font != null)
					StartFindFontReference();
				else
					mWin.ShowNotification(new GUIContent("请设置查找的字体"));
			}
		}
		EditorGUILayout.EndHorizontal();

		if (mPrefabDatas.Count > 0)
		{
			DrawPrefabScroll(mPrefabDatas);

			if (mReplaceFont != null)
			{
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.LabelField($"总数:{mPrefabDatas.Count}", GUILayout.Width(70));
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
				EditorGUILayout.LabelField($"总数:{mPrefabDatas.Count}", GUILayout.Width(64));
			}
		}
	}

	void DrawFontFilter(string title, FontFilter filter)
	{
		EditorGUILayout.BeginVertical("Box");
		{
			EditorGUILayout.LabelField(title);
			filter.font = EditorGUILayout.ObjectField(filter.font, typeof(Font), false) as Font;
			EditorGUILayout.BeginHorizontal();
			filter.line_toggle = EditorGUILayout.Toggle(filter.line_toggle, GUILayout.Width(PrefabTools.TGO_WIDTH));
			GUI.enabled = filter.line_toggle;
			filter.lineSpring = EditorGUILayout.FloatField("字体行距", filter.lineSpring);
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			filter.size_toggle = EditorGUILayout.Toggle(filter.size_toggle, GUILayout.Width(PrefabTools.TGO_WIDTH));
			GUI.enabled = filter.size_toggle;
			filter.size = EditorGUILayout.IntField("字体大小", filter.size);
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			filter.color_toggle = EditorGUILayout.Toggle(filter.color_toggle, GUILayout.Width(PrefabTools.TGO_WIDTH));
			GUI.enabled = filter.color_toggle;
			filter.color = EditorGUILayout.ColorField("字体颜色", filter.color);
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
			if (filter.last_font != filter.font)
			{
				filter.last_font = filter.font;
				var path = AssetDatabase.GetAssetPath(filter.font);
				filter.guid = AssetDatabase.AssetPathToGUID(path);
			}
		}
		EditorGUILayout.EndVertical();
	}

	private void StartFindFontReference()
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
				if (!mPrefabDatas.ContainsKey(path) && GetAllFontPath(prefab, ref data.children))
					mPrefabDatas.Add(path, data);
			}

			EditorUtility.DisplayProgressBar("查找索引...", path, (float) i / len);
		}

		EditorUtility.ClearProgressBar();
	}

	protected override void DrawPrefabScroll(Dictionary<string, PrefabData> prefabDatas)
	{
		mPrefabScroller = EditorGUILayout.BeginScrollView(mPrefabScroller);
		foreach (var data in prefabDatas.Values)
		{
			EditorGUILayout.BeginHorizontal();
			{
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
                        var target = GetSceneObject(fileName);
                        if (target == null)
                        {
                            var prefab = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                            prefab.transform.SetParent(TempParent, false);
                            prefab.name = fileName;
                        }
                        else
                        {
                            DestroyImmediate(target);
                        }
                    }
                }
#endif
				EditorGUILayout.ObjectField(data.prefab, data.prefab.GetType(), false, GUILayout.MaxWidth(200));

				data.foldout = EditorGUILayout.Foldout(data.foldout, "Children:" + data.children.Count, true);

				if (mReplaceFont != null)
				{
					if (GUILayout.Button("替换", GUILayout.MaxWidth(64)))
					{
						var stage = PrefabTools.GetPrefabStage(data.prefab, data.path);
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
							var stage = PrefabTools.GetPrefabStage(data.prefab, data.path);
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

		EditorGUILayout.EndScrollView();
	}

	bool GetAllFontPath(GameObject prefab, ref List<string> paths)
	{
		mPrefabChildren.Clear();
		var texts = prefab.GetComponentsInChildren<Text>(true);
		foreach (var text in texts)
		{
			if (!mPrefabChildren.Contains(text.gameObject) && FilterFont(text, mInputFont))
				mPrefabChildren.Add(text.gameObject);
		}

		foreach (var go in mPrefabChildren)
			paths.Add(PrefabTools.GetPath(go.gameObject));

		return paths.Count > 0;
	}

	bool FilterFont(Text text, FontFilter filter)
	{
		return text.font == filter.font
		       && (!filter.size_toggle || filter.size == text.fontSize)
		       && (!filter.line_toggle || filter.lineSpring == text.lineSpacing)
		       && (!filter.color_toggle || filter.color == text.color);
	}

	private void ReplaceAllFont()
	{
		if (mReplaceFont == null)
			return;
		//var i = 0;
		//var count = prefabDatas.Count;
		foreach (var data in mPrefabDatas.Values)
		{
			//i++;
			ReplaceFont(data.path);
			//var cancel = EditorUtility.DisplayCancelableProgressBar("正在替换", data.path, (float) i / count);
			//if (cancel) break;
		}

		//EditorUtility.ClearProgressBar();
		AssetDatabase.SaveAssets();
		EditorUtility.ClearProgressBar();
	}

	private void ReplaceFont(GameObject prefab, List<string> children)
	{
		if (mInputFont.font == null || mReplaceFont.font == null)
			return;
		foreach (var path in children)
		{
			var trans = path == prefab.name ? prefab.transform : prefab.transform.Find(path);
			if (trans == null)
			{
				Debug.LogErrorFormat("找不到节点, prefab:{0}, path:{1}", prefab.name, path);
				continue;
			}

			var text = trans.GetComponent<Text>();
			if (text != null)
				OnReplaceFont(text, mReplaceFont);
		}
	}

	private void ReplaceFont(string path)
	{
		if (mInputFont.font == null || mReplaceFont.font == null)
			return;
		var contents = PrefabUtility.LoadPrefabContents(path);
		var texts = contents.GetComponentsInChildren<Text>(true);
		foreach (var text in texts)
		{
			if (text.font == mInputFont.font)
				OnReplaceFont(text, mReplaceFont);
		}

		PrefabUtility.SaveAsPrefabAsset(contents, path);
		PrefabUtility.UnloadPrefabContents(contents);
	}

	void OnReplaceFont(Text text, FontFilter filter)
	{
		text.font = filter.font;
		if (filter.line_toggle)
			text.lineSpacing = filter.lineSpring;
		if (filter.size_toggle)
			text.fontSize = filter.size;
		if (filter.color_toggle)
			text.color = filter.color;
	}
}