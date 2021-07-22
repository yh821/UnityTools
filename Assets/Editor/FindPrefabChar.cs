using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditorInternal;
using UnityEngine.UI;

public class FindPrefabChar : EditorWindow
{
    private class Data
    {
        public string text;
        public string cn_text;
        public List<string> assetPaths;
        public bool foldout;

        public Data(string text)
        {
            this.text = text;
            this.cn_text = ChineseToUnicode(text);
            assetPaths = new List<string>();
            foldout = false;
        }

        public Data(string text, string chn_text)
        {
            this.text = text;
            this.cn_text = chn_text;
            assetPaths = new List<string>();
            foldout = false;
        }
    }

    [MenuItem("Tools/查找预设文字")]
    public static void AddWindow()
    {
        var win = GetWindow<FindPrefabChar>("查找预设文字");
        win.Init();
    }

    private static readonly Regex TextRegex = new Regex(@"m_Text: ""(\S+)""", RegexOptions.Singleline);
    private static readonly Regex CodeRegex = new Regex(@"((\\u\w{4})+)", RegexOptions.Singleline);
    private static readonly Regex ChnRegex = new Regex(@"([\u4E00-\u9FA5]+)", RegexOptions.Singleline);

    public const string PathListKey = "FindPrefabChar.PathList";
    public const string DefaultPath = "Assets/Game/UIs";
    public List<string> PathList = null;
    private SerializedObject mPathSerializedObject;
    private SerializedProperty mPathSerializedProperty;
    private ReorderableList mPathReorderableList;

    public static string StringToUnicode(string value)
    {
        var bytes = Encoding.Unicode.GetBytes(value);
        var sb = new StringBuilder();
        for (int i = 0; i < bytes.Length; i += 2)
        {
            sb.AppendFormat("\\u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'), bytes[i].ToString("x").PadLeft(2, '0'));
        }
        return sb.ToString();
    }

    public static string UnicodeToString(string value)
    {
        StringBuilder sb = new StringBuilder();
        value = value.Replace("\\", "");
        var unicodes = value.Split('u');
        for (int i = 1; i < unicodes.Length; i++)
        {
            sb.Append((char)int.Parse(unicodes[i], System.Globalization.NumberStyles.HexNumber));
        }
        return sb.ToString();
    }

    public static string ChineseToUnicode(string value)
    {
        var chn_matchs = ChnRegex.Matches(value);
        foreach (Match cm in chn_matchs)
        {
            var str = cm.Groups[1].Value;
            var chn = StringToUnicode(str);
            value = value.Replace(str, chn);
        }
        return value;
    }

    public static string UnicodeToChinese(string value)
    {
        var code_matchs = CodeRegex.Matches(value);
        foreach (Match cm in code_matchs)
        {
            var code = cm.Groups[1].Value;
            var chn = UnicodeToString(code);
            value = value.Replace(code, chn);
        }
        return value;
    }

    private string projectPath = "";
    //private string findPath = "";
    private Dictionary<string, Data> dict = new Dictionary<string, Data>();
    private List<Data> searchData = new List<Data>();
    private string searchKey = "";
    private string lastSearchKey = "";

    private GameObject selectPrefab;
    private bool isHaveCanvas = false;

    private Vector2 scroller = Vector2.zero;

    private Transform TempParent
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

    private void Init()
    {
        projectPath = Application.dataPath.Replace("Assets", "");
        //findPath = Path.Combine(Application.dataPath, "Prefabs");

        InitPathGui();
    }

    private void InitPathGui()
    {
        var pathStr = EditorPrefs.GetString(PathListKey, DefaultPath);
        PathList = new List<string>(pathStr.Split('|'));
        if (string.IsNullOrEmpty(PathList[0]))
            PathList[0] = DefaultPath;
        mPathSerializedObject = new SerializedObject(this);
        mPathSerializedProperty = mPathSerializedObject.FindProperty("PathList");
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

    private void SavePath()
    {
        mPathSerializedObject.ApplyModifiedProperties();
        for (int i = 0; i < PathList.Count; i++)
        {
            if (string.IsNullOrEmpty(PathList[i]))
                PathList.RemoveAt(i);
        }
        EditorPrefs.SetString(PathListKey, string.Join("|", PathList));
    }

    void OnLostFocus()
    {
        SavePath();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        EditorGUIUtility.labelWidth = 64;

        mPathReorderableList?.DoLayoutList();

        EditorGUILayout.BeginHorizontal();
        #region 导入预设文字
        if (GUILayout.Button("导入预设文字"))
        {
            FromCSV(out dict);
            RefreshSearchText();
        }
        #endregion
        #region 导出预设文字
        if (GUILayout.Button("导出预设文字"))
        {
            if (dict.Count > 0)
            {
                ToCSV(dict);
            }
        }
        #endregion
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        searchKey = EditorGUILayout.DelayedTextField("搜索文本", searchKey, GUILayout.MaxWidth(250));
        if (lastSearchKey != searchKey)
        {
            lastSearchKey = searchKey;
            if (dict.Count == 0)
            {
                FindTextReference();
            }
            RefreshSearchText();
        }

        #region 遍历预设文字
        if (GUILayout.Button("遍历预设文字"))
        {
            FindTextReference();
            RefreshSearchText();
        }
        #endregion
        EditorGUILayout.EndHorizontal();

        #region ShowTextDict
        if (dict.Count > 0)
        {
            scroller = EditorGUILayout.BeginScrollView(scroller);
            Data data;
            for (int i = 0; i < searchData.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                data = searchData[i];
                EditorGUILayout.LabelField(i.ToString(), data.cn_text);
                data.foldout = EditorGUILayout.Foldout(data.foldout, "使用该文本的预设", true);
                EditorGUILayout.EndHorizontal();
                if (data.foldout)
                {
                    foreach (var prefab in data.assetPaths)
                    {
                        EditorGUILayout.BeginHorizontal();
                        #region 显示/移除
                        if (GUILayout.Button("显示/移除", GUILayout.MaxWidth(64)))
                        {
                            Object asset = AssetDatabase.LoadMainAssetAtPath(prefab);
                            if (asset != null)
                            {
                                string name = Path.GetFileNameWithoutExtension(prefab);
                                var target = GetSceneObject(name, out isHaveCanvas);
                                if (target == null)
                                {
                                    selectPrefab = PrefabUtility.InstantiatePrefab(asset) as GameObject;
                                    isHaveCanvas = selectPrefab.GetComponentInChildren<CanvasScaler>() != null;
                                    if (!isHaveCanvas && TempParent != null)
                                        selectPrefab.transform.SetParent(TempParent, false);
                                    selectPrefab.name = name;
                                }
                                else
                                {
                                    DestroyImmediate(target);
                                    selectPrefab = null;
                                }
                            }
                        }
                        #endregion
                        EditorGUILayout.LabelField(prefab);
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
        #endregion

        EditorGUILayout.EndVertical();
    }

    private void RefreshSearchText()
    {
        searchData.Clear();
        if (string.IsNullOrEmpty(searchKey))
        {
            foreach (var chn in dict.Values)
            {
                searchData.Add(chn);
            }
        }
        else
        {
            foreach (var chn in dict.Values)
            {
                if (chn.cn_text.IndexOf(searchKey) != -1)
                    searchData.Add(chn);
            }
        }
    }

    private void FindTextReference()
    {
        dict.Clear();
        SavePath();
        //string[] dirs = Directory.GetFiles(findPath, "*.prefab", SearchOption.AllDirectories);
        var paths = PathList.ToArray();
        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
        {
            ShowNotification(new GUIContent("请设置路径"));
            return;
        }
        var guids = AssetDatabase.FindAssets("t:prefab", paths);
        for (int i = 0, len = guids.Length; i < len; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            //path = "Assets" + path.Replace(Application.dataPath, "");
            //path = path.Replace("\\", "/");
            if (path.Contains("Prefabs/Ch") || path.Contains("Prefabs/Effect"))
                continue;
            string text = File.ReadAllText(path);
            var textMatchs = TextRegex.Matches(text);
            foreach (Match tm in textMatchs)
            {
                var txt = tm.Groups[1].Value;
                if (dict.TryGetValue(txt, out var data))
                {
                    if (!data.assetPaths.Contains(path))
                        data.assetPaths.Add(path);
                }
                else
                {
                    var chnTxt = UnicodeToChinese(txt);
                    data = new Data(txt, chnTxt);
                    data.assetPaths.Add(path);
                    dict.Add(txt, data);
                }
            }
            EditorUtility.DisplayProgressBar("查找图片索引...", path, (float) i / len);
        }
        EditorUtility.ClearProgressBar();
    }

    private void ToCSV(Dictionary<string, Data> dict)
    {
        var filename = Path.Combine(projectPath, "预设文字.csv");
        using var w = new StreamWriter(filename, false, Encoding.UTF8);
        foreach (var item in dict)
        {
            w.WriteLine($"{item.Key},{item.Value.cn_text}");
        }
        w.Close();
    }

    private void FromCSV(out Dictionary<string, Data> dict)
    {
        dict = new Dictionary<string, Data>();
        var filename = Path.Combine(projectPath, "预设文字.csv");
        //var needLoadAbs = new Dictionary<string, int>();
        using var sr = new StreamReader(filename);
        string line;
        while ((line = sr.ReadLine()) != null)
        {
            line = line.Replace("\n", "");
            if (line.StartsWith("#"))
                continue;
            var lines = line.Split(',');
            var data = new Data(lines[0], lines[1]);
            dict.Add(lines[0], data);
        }
    }

    private GameObject GetSceneObject(string name, out bool hasCanvas)
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

}