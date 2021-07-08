using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

public class FindPrefabImage : EditorWindow
{
    private class Data
    {
        public string path;
        public GameObject prefab;
        public List<string> children;
        public bool foldout;

        public Data(string path, GameObject prefab)
        {
            this.path = path;
            this.prefab = prefab;
            children = new List<string>();
            foldout = false;
        }
    }

    [MenuItem("Tools/查找预制图片")]
    static void AddWindow()
    {
        var win = GetWindow<FindPrefabImage>("查找预制图片");
        win.Init();
    }

    private string mFindPath = string.Empty;
    private bool mIsSprite = false;

    private Sprite mInputSprite = null;
    private Sprite mReplaceSprite = null;

    private Texture2D mInputTexture = null;
    private Texture2D mLastInputTexture = null;
    private Texture2D mReplaceTexture = null;
    private Texture2D mLastReplaceTexture = null;

    private Dictionary<string, Data> mDataDict = new Dictionary<string, Data>();
    private List<GameObject> mReference = new List<GameObject>();

    private Vector2 mScroller = Vector2.zero;
    private const int SpacePixels = 10;

    private string mGuid;
    private string mLastGuid;
    private Object mObj;
    private Object mLastObj;
    private string mPath;

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

    public void Init(Sprite sprite = null)
    {
        mInputSprite = sprite;
        mFindPath = Path.Combine(Application.dataPath, "Game/UIs/View");
    }

    void OnGUI()
    {
        EditorGUIUtility.labelWidth = 64;
        GUILayout.Space(SpacePixels);
        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("选择路径", GUILayout.MaxWidth(100)))
            {
                mFindPath = EditorUtility.OpenFolderPanel("选择遍历路径", mFindPath, "");
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            mInputTexture = EditorGUILayout.ObjectField("查找的图片", mInputSprite, typeof(Texture2D), false, GUILayout.MaxWidth(128)) as Texture2D;
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

        GUILayout.Space(SpacePixels);
        if (GUILayout.Button("开始查找"))
        {
            if ((mIsSprite && mInputSprite != null) || (!mIsSprite && mInputTexture != null))
                FindImageReference();
            else
                ShowNotification(new GUIContent("请设置查找的图片"));
        }

        if (mDataDict.Count>0)
        {
            mScroller = EditorGUILayout.BeginScrollView(mScroller);
            DrawScrollView();
            EditorGUILayout.EndScrollView();

            if (mReplaceSprite != null || mReplaceTexture != null)
            {
                GUILayout.Space(SpacePixels);
                if (GUILayout.Button("全部替换"))
                {
                    if (EditorUtility.DisplayDialog("提示", "全部替换(二次确认)", "确定", "取消"))
                        ReplaceAllImage();
                }
            }
        }
    }

    private void DrawScrollView()
    {
        foreach (var data in mDataDict.Values)
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
                        var go = CreatePrefabInstance(data.prefab);
                        for (int i = 0; i < data.children.Count; i++)
                        {
                            string path = data.children[i];
                            var image = go.transform.Find(path).GetComponent<Image>();
                            if (image != null)
                                image.sprite = mReplaceSprite;
                        }
                        PrefabUtility.ReplacePrefab(go, data.prefab, ReplacePrefabOptions.ConnectToPrefab);
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
                            var fileName = Path.GetFileNameWithoutExtension(data.path);
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

    PrefabStage GetPrefabStage(Data data)
    {
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage == null || stage.prefabAssetPath != data.path)
        {
            AssetDatabase.OpenAsset(data.prefab);
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

    public void FindImageReference()
    {
        mDataDict.Clear();
        var dirs = Directory.GetFiles(mFindPath, "*.prefab", SearchOption.AllDirectories);
        for (int i = 0; i < dirs.Length; i++)
        {
            var path = "Assets" + dirs[i].Replace(Application.dataPath, "").Replace('\\','/');
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                var data = new Data(path, prefab);
                if (!mDataDict.ContainsKey(path) && GetAllSpritePath(prefab, ref data.children))
                    mDataDict.Add(path, data);
            }
            EditorUtility.DisplayProgressBar("查找索引...", path, (float)i / dirs.Length);
        }
        EditorUtility.ClearProgressBar();
    }

    bool GetAllSpritePath(GameObject mainAsset, ref List<string> paths)
    {
        mReference.Clear();

        var rawImageList = mainAsset.GetComponentsInChildren<RawImage>(true);
        foreach (var rawImage in rawImageList)
        {
            if (rawImage.texture != null && rawImage.texture == mInputTexture && !mReference.Contains(rawImage.gameObject))
                mReference.Add(rawImage.gameObject);
        }

        if (mIsSprite)
        {
            var imageList = mainAsset.GetComponentsInChildren<Image>(true);
            foreach (var image in imageList)
            {
                if (image.sprite != null && image.sprite == mInputSprite && !mReference.Contains(image.gameObject))
                    mReference.Add(image.gameObject);
            }
        }

        foreach (var go in mReference)
            paths.Add(GetPath(go));

        return mReference.Count > 0;
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
        foreach (var data in mDataDict.Values)
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

}
