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

public partial class PrefabTools : EditorWindow
{

	[MenuItem("Assets/查找预制图片", true)]
	static bool FindSelectImageValidate()
	{
		return Selection.activeObject is Texture2D;
	}
	[MenuItem("Assets/查找预制图片", false)]
	static void FindSelectImage()
	{
		var texture = Selection.activeObject as Texture2D;
		if (texture)
		{
			var win = OpenWindow();
			win.FindImageReference(texture);
		}
	}

	[Serializable]
    private class SpriteData
    {
        public string path;
        public GameObject prefab;
        public List<string> children;
        public bool foldout;

        public SpriteData(string path, GameObject prefab)
        {
            this.path = path;
            this.prefab = prefab;
            children = new List<string>();
            foldout = false;
        }
    }

    //private string mFindPath = string.Empty;
    private bool mIsSprite = false;

    private Sprite mInputSprite = null;
    private Sprite mReplaceSprite = null;

    private Texture2D mInputTexture = null;
    private Texture2D mLastInputTexture = null;
    private Texture2D mReplaceTexture = null;
    private Texture2D mLastReplaceTexture = null;

    private Dictionary<string, SpriteData> mSpriteDataMap = new Dictionary<string, SpriteData>();
    private List<GameObject> mSpriteReference = new List<GameObject>();

    private Vector2 mImageScroller = Vector2.zero;

    private string mGuid;
    private string mLastGuid;
    private Object mObj;
    private Object mLastObj;
    private string mPath;

    public const string PathListKey = "FindPrefabImage.CommitPathList";
    public const string DefaultPath = "Assets/Game/UIs";
    public List<string> CommitPathList = null;
    private SerializedObject mPathSerializedObject;
    private SerializedProperty mPathSerializedProperty;
    private ReorderableList mPathReorderableList;

    private Transform mTempParent = null;
    public Transform TempParent
    {
        get
        {
            if (mTempParent == null)
            {
                var go = GameObject.Find("GameRoot/BaseView/Root");
                if (go != null)
                    mTempParent = go.transform;
            }
            return mTempParent;
        }
    }

    public void InitFindImage(Sprite sprite = null)
    {
        mInputSprite = sprite;
        //mFindPath = Path.Combine(Application.dataPath, "Game/UIs/View");
    }

    private void InitPathGui()
    {
        var pathStr = EditorPrefs.GetString(PathListKey, DefaultPath);
        CommitPathList = new List<string>(pathStr.Split('|'));
        if (string.IsNullOrEmpty(CommitPathList[0]))
            CommitPathList[0] = DefaultPath;
        mPathSerializedObject = new SerializedObject(this);
        mPathSerializedProperty = mPathSerializedObject.FindProperty("CommitPathList");
        mPathReorderableList = new ReorderableList(mPathSerializedObject, mPathSerializedProperty)
        {
            drawHeaderCallback = rect => GUI.Label(rect, "路径列表:"),
            elementHeight = EditorGUIUtility.singleLineHeight,
            drawElementCallback = (rect, index, selected, focused) =>
            {
                var element = mPathSerializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, element, GUIContent.none);
            }
        };
    }

    private void SaveCommitPath()
    {
        mPathSerializedObject.ApplyModifiedProperties();
        for (int i = 0; i < CommitPathList.Count; i++)
        {
            if(string.IsNullOrEmpty(CommitPathList[i]))
                CommitPathList.RemoveAt(i);
        }
        EditorPrefs.SetString(PathListKey, string.Join("|", CommitPathList));
    }

    void OnGUI_FindImage()
    {
        EditorGUIUtility.labelWidth = 64;

        mPathReorderableList?.DoLayoutList();

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

	        if (GUILayout.Button("遍历引用"))
	        {
		        var paths = CommitPathList.ToArray();
		        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
		        {
			        ShowNotification(new GUIContent("请设置路径"));
			        return;
		        }
		        var guids = AssetDatabase.FindAssets("t:prefab", paths);
		        for (int i = 0, len = guids.Length; i < len; i++)
		        {
			        var path = AssetDatabase.GUIDToAssetPath(guids[i]);
			        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			        if (prefab)
			        {
				        if (GetAllGUIDPath(mGuid, path))
					        Debug.Log(path);
			        }
			        EditorUtility.DisplayProgressBar("查找索引...", path, (float) i / len);
		        }
		        EditorUtility.ClearProgressBar();
	        }
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
            mInputTexture = EditorGUILayout.ObjectField("查找的图片", mInputTexture, typeof(Texture2D), false, GUILayout.MaxWidth(128)) as Texture2D;
            if (mLastInputTexture != mInputTexture)
            {
	            mLastInputTexture = mInputTexture;
	            var path = AssetDatabase.GetAssetPath(mInputTexture);
	            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
	            mIsSprite = importer.textureType == TextureImporterType.Sprite;
	            if (mIsSprite)
		            mInputSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            GUILayout.Space(30);

            mReplaceTexture = EditorGUILayout.ObjectField("替换的图片", mReplaceTexture, typeof(Texture2D), false, GUILayout.MaxWidth(128)) as Texture2D;
            if (mLastReplaceTexture != mReplaceTexture)
            {
                mLastReplaceTexture = mReplaceTexture;
                mReplaceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(mReplaceTexture));
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(SPACE);
        if (GUILayout.Button("开始查找"))
        {
            if ((mIsSprite && mInputSprite != null) || (!mIsSprite && mInputTexture != null))
                StartFindImageReference();
            else
                ShowNotification(new GUIContent("请设置查找的图片"));
        }

        if (mSpriteDataMap.Count>0)
        {
            mImageScroller = EditorGUILayout.BeginScrollView(mImageScroller);
            DrawSpriteScrollView();
            EditorGUILayout.EndScrollView();

            if (mReplaceSprite != null || mReplaceTexture != null)
            {
                GUILayout.Space(SPACE);
                if (GUILayout.Button("全部替换"))
                {
                    if (EditorUtility.DisplayDialog("提示", "全部替换(二次确认)", "确定", "取消"))
                        ReplaceAllImage();
                }
            }
        }
    }

    private void DrawSpriteScrollView()
    {
        foreach (var data in mSpriteDataMap.Values)
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

                if (mReplaceSprite != null)
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

                if (mReplaceSprite != null || mReplaceTexture != null)
                {
                    if (GUILayout.Button("替换", GUILayout.MaxWidth(64)))
                    {
#if UNITY_2018_3_OR_NEWER
                        var stage = GetPrefabStage(data);
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
                            var stage = GetPrefabStage(data);
                            var go = stage.prefabContentsRoot;
                            var trans = path == go.name ? go.transform : go.transform.Find(path);
                            if (trans != null)
                                Selection.activeObject = trans.gameObject;
#else
                            var fileName = Path.GetFileNameWithoutExtension(spriteData.path);
                            var target = GetSceneObject(fileName);
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
                            var stage = GetPrefabStage(data);
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

    PrefabStage GetPrefabStage(SpriteData spriteData)
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage == null || stage.prefabAssetPath != spriteData.path)
        {
            AssetDatabase.OpenAsset(spriteData.prefab);
            stage = PrefabStageUtility.GetCurrentPrefabStage();
        }

        return stage;
    }

    GameObject GetSceneObject(string name)
    {
        if (TempParent != null)
        {
            var t = TempParent.Find(name);
            if (t != null)
                return t.gameObject;
        }

        return GameObject.Find(name);
    }

    private void StartFindImageReference()
    {
        mSpriteDataMap.Clear();
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
                if (!mSpriteDataMap.ContainsKey(path) && GetAllSpritePath(prefab, ref data.children))
                    mSpriteDataMap.Add(path, data);
            }
            EditorUtility.DisplayProgressBar("查找索引...", path, (float) i / len);
        }
        EditorUtility.ClearProgressBar();
    }

    bool GetAllSpritePath(GameObject mainAsset, ref List<string> paths)
    {
        mSpriteReference.Clear();

        var rawImageList = mainAsset.GetComponentsInChildren<RawImage>(true);
        foreach (var rawImage in rawImageList)
        {
            if (rawImage.texture != null && rawImage.texture == mInputTexture && !mSpriteReference.Contains(rawImage.gameObject))
                mSpriteReference.Add(rawImage.gameObject);
        }

        if (mIsSprite)
        {
            var imageList = mainAsset.GetComponentsInChildren<Image>(true);
            foreach (var image in imageList)
            {
                if (image.sprite != null && image.sprite == mInputSprite && !mSpriteReference.Contains(image.gameObject))
                    mSpriteReference.Add(image.gameObject);
            }
        }

        foreach (var go in mSpriteReference)
            paths.Add(GetPath(go));

        return mSpriteReference.Count > 0;
    }

	private static readonly Regex TextureRegex = new Regex(@"m_Texture: \{fileID: \d+, guid: (\w+), type: \d+\}", RegexOptions.Singleline);
	private static readonly Regex SpriteRegex = new Regex(@"m_Sprite: \{fileID: \d+, guid: (\w+), type: \d+\}", RegexOptions.Singleline);

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

    string GetPath(GameObject g)
    {
        Transform t = g.transform;
        string path = t.gameObject.name;
        while (t.parent != null && t.parent.parent != null)
        {
            path = t.parent.name + "/" + path;
            t = t.parent;
        }
        return path;
    }

    private void ReplaceAllImage()
    {
        foreach (var data in mSpriteDataMap.Values)
        {
            var go = PrefabUtility.InstantiatePrefab(data.prefab) as GameObject;
            ReplaceImage(go, data.children);
            PrefabUtility.SaveAsPrefabAsset(go, data.path);
            DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
    }

    private void ReplaceImage(GameObject go, List<string> children)
    {
        foreach (var path in children)
        {
            var trans = path == go.name ? go.transform : go.transform.Find(path);
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
        StartFindImageReference();
    }

    public void FindImageReference(Texture2D texture)
    {
	    mIsSprite = false;
	    mInputSprite = null;
	    mInputTexture = texture;
        mLastInputTexture = mInputTexture;
	    StartFindImageReference();
    }

}
