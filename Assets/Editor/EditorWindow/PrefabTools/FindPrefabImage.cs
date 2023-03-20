using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using Object = UnityEngine.Object;

public class FindPrefabImage : FindPrefabBase
{
	public FindPrefabImage(PrefabTools win) : base(win)
	{
	}

	//private string mFindPath = string.Empty;
	private bool mIsSprite = false;

	private Sprite mInputSprite = null;
	private Sprite mReplaceSprite = null;

	private Texture2D mInputTexture = null;
	private Texture2D mLastInputTexture = null;
	private Texture2D mReplaceTexture = null;
	private Texture2D mLastReplaceTexture = null;

	private Vector2 mImageScroller = Vector2.zero;

	private string mGuid;
	private string mLastGuid;
	private Object mObj;
	private Object mLastObj;
	private string mPath;

	public void InitFindImage(Sprite sprite = null)
	{
		mInputSprite = sprite;
		//mFindPath = Path.Combine(Application.dataPath, "Game/UIs/View");
	}

	public override void OnGUI()
	{
		EditorGUILayout.BeginHorizontal();
		{
			mGuid = EditorGUILayout.TextField("GUID:", mGuid, GUILayout.MaxWidth(300));
			if (mGuid != mLastGuid)
			{
				mLastGuid = mGuid;
				mPath = AssetDatabase.GUIDToAssetPath(mGuid);
				mObj = AssetDatabase.LoadAssetAtPath<Object>(mPath);
				mLastObj = mObj;
			}

			if (mObj != mLastObj)
			{
				mLastObj = mObj;
				mPath = AssetDatabase.GetAssetPath(mObj);
				mGuid = AssetDatabase.AssetPathToGUID(mPath);
				mLastGuid = mGuid;
			}

			mObj = EditorGUILayout.ObjectField(mObj, typeof(Object), false, GUILayout.MaxWidth(150));
			EditorGUILayout.TextField(mPath);

			//if (GUILayout.Button("遍历引用"))
			//{
			// var paths = mWin.GetPrefabPaths();
			// if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
			// {
			//  mWin.ShowNotification(new GUIContent("请设置路径"));
			//  return;
			// }
			// var guids = AssetDatabase.FindAssets("t:prefab", paths);
			// for (int i = 0, len = guids.Length; i < len; i++)
			// {
			//  var path = AssetDatabase.GUIDToAssetPath(guids[i]);
			//  var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			//  if (prefab)
			//  {
			//   if (GetAllGUIDPath(mGuid, path))
			//    Debug.Log(path);
			//  }
			//  EditorUtility.DisplayProgressBar("查找索引...", path, (float) i / len);
			// }
			// EditorUtility.ClearProgressBar();
			//}
		}
		EditorGUILayout.EndHorizontal();

		//EditorGUILayout.BeginHorizontal();
		//{
		//    if (GUILayout.Button("选择路径", GUILayout.MaxWidth(100)))
		//    {
		//        mFindPath = EditorUtility.OpenFolderPanel("选择遍历路径", mFindPath, "");
		//    }
		//}
		//EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		{
			mInputTexture =
				EditorGUILayout.ObjectField("查找的图片", mInputTexture, typeof(Texture2D), false, GUILayout.MaxWidth(128))
					as Texture2D;
			if (mLastInputTexture != mInputTexture)
			{
				mLastInputTexture = mInputTexture;
				var path = AssetDatabase.GetAssetPath(mInputTexture);
				var importer = AssetImporter.GetAtPath(path) as TextureImporter;
				if (importer != null) mIsSprite = importer.textureType == TextureImporterType.Sprite;
				if (mIsSprite)
					mInputSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
			}

			GUILayout.Space(30);

			mReplaceTexture =
				EditorGUILayout.ObjectField("替换的图片", mReplaceTexture, typeof(Texture2D), false,
					GUILayout.MaxWidth(128)) as Texture2D;
			if (mLastReplaceTexture != mReplaceTexture)
			{
				mLastReplaceTexture = mReplaceTexture;
				mReplaceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(mReplaceTexture));
			}
		}
		EditorGUILayout.EndHorizontal();

		//if (mIsPreviewMode)
		// EditorGUILayout.HelpBox("预制里路径相同的节点会选中错误，仅供预览，替换建议使用非预览模式。", MessageType.Warning);
		//else
		// EditorGUILayout.HelpBox("当前使用非预览模式。", MessageType.Info);
		EditorGUILayout.BeginHorizontal();
		{
			//mIsPreviewMode = GUILayout.Toggle(mIsPreviewMode, "预览模式", GUILayout.Width(70));
			if (GUILayout.Button("开始查找"))
			{
				if ((mIsSprite && mInputSprite != null) || (!mIsSprite && mInputTexture != null))
					StartFindPrefab();
				else
					mWin.ShowNotification(new GUIContent("请设置查找的图片"));
			}
		}
		EditorGUILayout.EndHorizontal();

		if (mPrefabDatas.Count > 0)
		{
			mImageScroller = EditorGUILayout.BeginScrollView(mImageScroller);
			DrawPrefabScroll(mPrefabDatas);
			EditorGUILayout.EndScrollView();

			if (mReplaceSprite != null || mReplaceTexture != null)
			{
				GUILayout.Space(PrefabTools.SPACE);
				if (GUILayout.Button("全部替换"))
				{
					if (EditorUtility.DisplayDialog("提示", "全部替换(二次确认)", "确定", "取消"))
						ReplaceAllImage();
				}
			}
		}
	}

	protected override void DrawPrefabScroll(Dictionary<string, PrefabData> prefabDatas)
	{
		foreach (var data in mPrefabDatas.Values)
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
#endif
				if (GUILayout.Button("克隆/删除", GUILayout.MaxWidth(64)))
				{
					var prefab = AssetDatabase.LoadMainAssetAtPath(data.path);
					if (prefab != null)
					{
						var fileName = Path.GetFileNameWithoutExtension(data.path);
						var target = mWin.GetSceneObject(fileName);
						if (target == null)
						{
							var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
							go.transform.SetParent(mWin.TempParent, false);
							go.name = fileName;
						}
						else
						{
							Object.DestroyImmediate(target);
						}
					}
				}

				EditorGUILayout.ObjectField(data.prefab, data.prefab.GetType(), false, GUILayout.MaxWidth(200));

				data.foldout = EditorGUILayout.Foldout(data.foldout, "Children:" + data.children.Count, true);

				if (mReplaceSprite != null)
				{
					if (GUILayout.Button("SetNative", GUILayout.MaxWidth(70)))
					{
						var stage = PrefabTools.GetPrefabStage(data.prefab, data.path);
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

				if (mReplaceSprite != null || mReplaceTexture != null)
				{
					if (GUILayout.Button("替换", GUILayout.MaxWidth(64)))
					{
#if UNITY_2018_3_OR_NEWER
						var stage = PrefabTools.GetPrefabStage(data.prefab, data.path);
						ReplaceImage(stage.prefabContentsRoot, data.children);
						EditorSceneManager.MarkSceneDirty(stage.scene);
#else
                        var go = CreatePrefabInstance(spriteData.prefab);
                        for (int i = 0; i < spriteData.children.Count; i++)
                        {
                            string path = spriteData.children[i];
                            var image = go.transform.Find(path).GetComponent<Image>();
                            if (image != null)
                                image.sprite = mReplaceSprite;
                        }
                        PrefabUtility.ReplacePrefab(go, spriteData.prefab, ReplacePrefabOptions.ConnectToPrefab);
                        AssetDatabase.SaveAssets();
#endif
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
#if UNITY_2018_3_OR_NEWER
							var stage = PrefabTools.GetPrefabStage(data.prefab, data.path);
							var go = stage.prefabContentsRoot;
							var trans = path == go.name ? go.transform : go.transform.Find(path);
							if (trans != null)
								Selection.activeObject = trans.gameObject;
#else
                            var fileName = Path.GetFileNameWithoutExtension(spriteData.path);
                            var target = mWin.GetSceneObject(fileName);
                            if (target != null && PrefabUtility.GetPrefabType(target) == PrefabType.PrefabInstance)
                            {
                                var select = target.transform.Find(path);
                                if (select != null)
                                    Selection.activeObject = select.gameObject;
                            }
#endif
						}

						if (GUILayout.Button("Native", GUILayout.MaxWidth(50)))
						{
							var stage = PrefabTools.GetPrefabStage(data.prefab, data.path);
							var go = stage.prefabContentsRoot;
							var trans = path == go.name ? go.transform : go.transform.Find(path);
							if (trans != null)
							{
								Selection.activeObject = trans.gameObject;
								var image = trans.GetComponent<Image>();
								if (image != null)
									image.SetNativeSize();
							}

							EditorSceneManager.MarkSceneDirty(stage.scene);
						}

						EditorGUILayout.LabelField(path);
					}
					EditorGUILayout.EndHorizontal();
				}
			}
		}
	}

	protected override bool FindChildrenPath(GameObject prefab, ref List<string> paths)
	{
		mPrefabChildren.Clear();
		var rawImages = prefab.GetComponentsInChildren<RawImage>(true);
		foreach (var rawImage in rawImages)
		{
			if (rawImage.texture != null && rawImage.texture == mInputTexture &&
			    !mPrefabChildren.Contains(rawImage.gameObject))
				mPrefabChildren.Add(rawImage.gameObject);
		}

		foreach (var rawImage in mPrefabChildren)
			paths.Add(PrefabTools.GetPath(rawImage));

		if (mIsSprite)
		{
			var images = prefab.GetComponentsInChildren<Image>(true);
			foreach (var image in images)
			{
				if (image.sprite != null && image.sprite == mInputSprite && !mPrefabChildren.Contains(image.gameObject))
					mPrefabChildren.Add(image.gameObject);
			}

			foreach (var image in mPrefabChildren)
				paths.Add(PrefabTools.GetPath(image));
		}

		return paths.Count > 0;
	}

	private static readonly Regex TextureRegex =
		new Regex(@"m_Texture: \{fileID: \d+, guid: (\w+), type: \d+\}", RegexOptions.Singleline);

	private static readonly Regex SpriteRegex =
		new Regex(@"m_Sprite: \{fileID: \d+, guid: (\w+), type: \d+\}", RegexOptions.Singleline);

	bool GetAllGUIDPath(string guid, string path)
	{
		var content = File.ReadAllText(path);
		var texMatchs = TextureRegex.Matches(content);
		var sprMatchs = SpriteRegex.Matches(content);
		foreach (Match mt in texMatchs)
		{
			var id = mt.Groups[1].Value;
			if (id == guid)
				return true;
		}

		foreach (Match mt in sprMatchs)
		{
			var id = mt.Groups[1].Value;
			if (id == guid)
				return true;
		}

		return false;
	}

	private void ReplaceAllImage()
	{
		var i = 0;
		var count = mPrefabDatas.Count;
		foreach (var data in mPrefabDatas.Values)
		{
			//var go = PrefabUtility.InstantiatePrefab(data.prefab) as GameObject;
			//ReplaceImage(go, data.children);
			//PrefabUtility.SaveAsPrefabAsset(go, data.path);
			//DestroyImmediate(go);
			i++;
			var prefab = PrefabUtility.LoadPrefabContents(data.path);
			ReplaceImage(prefab, data.children);
			PrefabUtility.SaveAsPrefabAsset(prefab, data.path);
			PrefabUtility.UnloadPrefabContents(prefab);
			var cancel = EditorUtility.DisplayCancelableProgressBar("正在替换", data.path, (float) i / count);
			if (cancel) break;
		}

		AssetDatabase.SaveAssets();
		EditorUtility.ClearProgressBar();
	}

	private void ReplaceImage(GameObject go, List<string> children)
	{
		foreach (var path in children)
		{
			var trans = path == go.name ? go.transform : go.transform.Find(path);
			if (trans == null)
			{
				Debug.LogError("找不到节点:" + path);
				continue;
			}

			var image = trans.GetComponent<Image>();
			if (image != null)
			{
				if (mReplaceSprite != null)
					image.sprite = mReplaceSprite;
				continue;
			}

			var rawImage = trans.GetComponent<RawImage>();
			if (rawImage != null)
			{
				if (mReplaceTexture != null)
					rawImage.texture = mReplaceTexture;
			}
		}
	}

	public void FindImageReference(Sprite sprite)
	{
		mIsSprite = true;
		mInputSprite = sprite;
		var path = AssetDatabase.GetAssetPath(mInputSprite);
		mInputTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		mLastInputTexture = mInputTexture;
		StartFindPrefab();
	}

	public void FindImageReference(Texture2D texture)
	{
		mIsSprite = false;
		mInputSprite = null;
		mInputTexture = texture;
		mLastInputTexture = mInputTexture;
		StartFindPrefab();
	}
}