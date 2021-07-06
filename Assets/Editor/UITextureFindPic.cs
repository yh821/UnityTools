using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
public class UITextureFindPic : EditorWindow
{
    HashSet<Sprite> usingSprites = new HashSet<Sprite>();
    Vector2 scroller = Vector2.zero;
    Dictionary<string, string> Ignore = new Dictionary<string, string>();
    bool showIgnore = false;
    string findPath;
    string showPath;

    public virtual string JsonFileName
    {
        get
        {
            return "ArtWorks/EXPORTERS/Editor_Check_UI_Ignore.json";
        }

    }
    [MenuItem("检查工具/资源[3]/图片找出处")]
    static void AddWindow()
    {
        //创建窗口
        GetWindow<UITextureFindPic>("图片找出处");
    }

    public void Init(Sprite sprite)
    {
        inputSprite = sprite;
    }

    public void FindSpriteReference(Sprite inputSp)
    {
        inputSprite = inputSp;
        FindTextureReference();
    }

    void Awake()
    {
        var abspath = Path.Combine(Application.dataPath, JsonFileName);
        if (Common.FileHelper.IsFileExists(abspath))
        {
            var content = Common.FileHelper.ReadFileText(abspath);
            Ignore = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
        }
    }

    void OnEnable()
    {
        findPath = Application.dataPath + "/Prefabs";
        showPath = "Assets/Prefabs";
    }

    void WriteIgnore()
    {
        var abspath = Path.Combine(Application.dataPath, JsonFileName);
        Common.FileHelper.WriteAllText(abspath, JsonConvert.SerializeObject(Ignore));
    }

    void Copy<T>(List<T> dst, T[] src)
    {
        for (int i = 0; i < src.Length; i++)
        {
            dst.Add(src[i]);
        }
    }

    List<GameObject> mReference = new List<GameObject>();

    bool GetImageSpritePath(GameObject mainAsset, out List<string> paths)
    {
        mReference.Clear();

        var imageList = mainAsset.GetComponentsInChildren<Image>(true);
        for (int kk = 0; kk < imageList.Length; kk++)
        {
            var o = imageList[kk];
            if (o.sprite)
            {
                AddSprite(o.sprite, o.gameObject);
            }
        }

        paths = new List<string>();
        foreach (var go in mReference)
            paths.Add(GetPath(go));

        return paths.Count > 0;
    }

    bool GetAllSpritePath(GameObject mainAsset, out List<string> paths)
    {
        mReference.Clear();

        #region Button
        var btns = mainAsset.GetComponentsInChildren<Button>(true);
        for (int kk = 0; kk < btns.Length; kk++)
        {
            var o = btns[kk].spriteState;
            if (o.disabledSprite)
            {
                AddSprite(o.disabledSprite, btns[kk].gameObject);
            }
            if (o.highlightedSprite)
            {
                AddSprite(o.highlightedSprite, btns[kk].gameObject);
            }
            if (o.pressedSprite)
            {
                AddSprite(o.pressedSprite, btns[kk].gameObject);
            }
        }
        #endregion

        #region Image
        var imageList = mainAsset.GetComponentsInChildren<Image>(true);
        for (int kk = 0; kk < imageList.Length; kk++)
        {
            var o = imageList[kk];
            if (o.sprite)
            {
                AddSprite(o.sprite, o.gameObject);
            }
        }
        #endregion

        paths = new List<string>();
        foreach (var go in mReference)
            paths.Add(GetPath(go));

        return paths.Count > 0;
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

    bool checkPicSame(Sprite checkSp)
    {
        if (inputSprite != null && inputSprite == checkSp)
            return true;
        else
            return false;
    }

    void AddSprite(Sprite s, GameObject t)
    {
        if (checkPicSame(s) && !mReference.Contains(t))
            mReference.Add(t);
    }

    Dictionary<GameObject, List<string>> outSpirte = new Dictionary<GameObject, List<string>>();
    Sprite inputSprite;
    Sprite newSprite;
    GameObject selectPrefab;
    bool isHaveCanvas = false;
    List<bool> IgnoreToggle = new List<bool>();
    List<bool> ShowToggle = new List<bool>();
    int index = 0;
    Transform TempParent
    {
        get
        {
            if (mTempParent == null)
            {
                var go = GameObject.Find("GUI_ROOT/TopCanvas");
                if (go != null)
                    mTempParent = go.transform;
            }
            return mTempParent;
        }
    }
    Transform mTempParent = null;

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        EditorGUIUtility.labelWidth = 64;

        EditorGUILayout.BeginHorizontal();
        inputSprite = EditorGUILayout.ObjectField("查找的图片", inputSprite, typeof(Sprite), false, GUILayout.MaxWidth(128)) as Sprite;
        newSprite = EditorGUILayout.ObjectField("替换的图片", newSprite, typeof(Sprite), false, GUILayout.MaxWidth(128)) as Sprite;
        EditorGUILayout.LabelField("查找的路径:", showPath);
        #region 选择路径
        if (GUILayout.Button("选择路径", GUILayout.MaxWidth(100)))
        {
            findPath = EditorUtility.OpenFolderPanel("选择遍历路径", findPath, "");
            showPath = "Assets" + findPath.Replace(Application.dataPath, "").Replace("\\", "/");
        }
        #endregion
        EditorGUILayout.EndHorizontal();

        #region 查找引用
        if (GUILayout.Button("查找引用"))
        {
            FindTextureReference();
        }
        #endregion

        if (outSpirte != null)
        {
            scroller = EditorGUILayout.BeginScrollView(scroller);
            index = 0;
            SetShowCount(outSpirte.Count);
            foreach (var item in outSpirte)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(index.ToString(), GUILayout.MaxWidth(24));
                #region 显示/移除
                if (GUILayout.Button("显示/移除", GUILayout.MaxWidth(64)))
                {
                    var target = GetSceneObject(item.Key.name, out isHaveCanvas);
                    if (target == null)
                    {
                        selectPrefab = PrefabUtility.InstantiatePrefab(item.Key) as GameObject;
                        isHaveCanvas = selectPrefab.GetComponentInChildren<CanvasScaler>() != null;
                        if (!isHaveCanvas && TempParent != null)
                            selectPrefab.transform.SetParent(TempParent, false);
                        selectPrefab.name = item.Key.name;
                    }
                    else
                    {
                        DestroyImmediate(target);
                        selectPrefab = null;
                    }
                }
                #endregion
                #region 替换
                if (newSprite != null)
                {
                    if (GUILayout.Button("替换", GUILayout.MaxWidth(64)))
                    {
                        GameObject go = CreatePrefabInstance(item.Key);
                        for (int i = 0; i < item.Value.Count; i++)
                        {
                            if (!IgnoreToggle[i])
                                continue;
                            string path = item.Value[i];
                            var image = go.transform.Find(path).GetComponent<Image>();
                            if (image != null)
                                image.sprite = newSprite;
                        }
                        var prefab = PrefabUtility.ReplacePrefab(go, item.Key, ReplacePrefabOptions.ConnectToPrefab);
                        AssetDatabase.SaveAssets();
                    }
                }
                #endregion
                EditorGUILayout.ObjectField(item.Key, item.Key.GetType(), false, GUILayout.MaxWidth(200));
                #region Child
                ShowToggle[index] = EditorGUILayout.Foldout(ShowToggle[index], "Child:" + item.Value.Count, true);
                EditorGUILayout.EndHorizontal();

                if (ShowToggle[index])
                {
                    SetChildCount(item.Value.Count);
                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.MaxWidth(24));//空白
                        if (newSprite != null)
                            IgnoreToggle[i] = EditorGUILayout.Toggle(IgnoreToggle[i], GUILayout.MaxWidth(10));
                        #region 选中
                        if (GUILayout.Button("选中", GUILayout.MaxWidth(40)))
                        {
                            var target = GetSceneObject(item.Key.name, out isHaveCanvas);
                            if (target != null && PrefabUtility.GetPrefabType(target) == PrefabType.PrefabInstance)
                            {
                                var select = target.transform.Find(item.Value[i]);
                                if (select != null)
                                    Selection.activeObject = select.gameObject;
                            }
                        }
                        #endregion
                        EditorGUILayout.LabelField(item.Key.name + "/" + item.Value[i]);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                #endregion
                index++;
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }

    public void FindTextureReference()
    {
        outSpirte.Clear();
        if (inputSprite == null)
            return;

        string[] dirs = Directory.GetFiles(findPath, "*.prefab", SearchOption.AllDirectories);
        for (int i = 0; i < dirs.Length; i++)
        {
            string path = dirs[i];
            path = path.Replace("\\", "/");
            if (path.Contains("Prefabs/Ch")
                || path.Contains("Prefabs/Effect"))
            {
                continue;
            }
            path = "Assets" + path.Replace(Application.dataPath, "");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                List<string> paths;
                if (!outSpirte.ContainsKey(prefab) && GetAllSpritePath(prefab, out paths))
                    outSpirte.Add(prefab, paths);
            }

            EditorUtility.DisplayProgressBar("精灵引用搜索", path, (float)i / dirs.Length);
        }

        EditorUtility.ClearProgressBar();
    }

    GameObject GetSceneObject(string name, out bool hasCanvas)
    {
        hasCanvas = false;
        if (TempParent != null)
        {
            var t = TempParent.Find(name);
            if (t != null)
                return t.gameObject;
        }

        var target = GameObject.Find(name);
        if (target != null)
            hasCanvas = true;
        return target;
    }

    void SetChildCount(int length)
    {
        if (IgnoreToggle.Count < length)
        {
            for (int i = IgnoreToggle.Count; i < length; i++)
                IgnoreToggle.Add(true);
        }
    }

    void SetShowCount(int length)
    {
        if (ShowToggle.Count < length)
        {
            for (int i = ShowToggle.Count; i < length; i++)
                ShowToggle.Add(false);
        }
    }

    GameObject CreatePrefabInstance(GameObject prefab)
    {
        var target = GetSceneObject(prefab.name, out isHaveCanvas);
        if (target == null || PrefabUtility.GetPrefabType(target) != PrefabType.PrefabInstance)
        {
            target = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (!isHaveCanvas && TempParent != null)
                target.transform.SetParent(TempParent, false);
            target.name = prefab.name;
        }
        return target;
    }
}
