using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class FindPrefabChar : EditorWindow
{
    class Data
    {
        public string text;
        public string chn_text;
        public List<string> assetPaths;
        public bool foldout;

        public Data(string text)
        {
            this.text = text;
            this.chn_text = ChineseToUnicode(text);
            assetPaths = new List<string>();
            foldout = false;
        }

        public Data(string text, string chn_text)
        {
            this.text = text;
            this.chn_text = chn_text;
            assetPaths = new List<string>();
            foldout = false;
        }
    }

    [MenuItem("Tools/查找预设文字")]
    static void AddWindow()
    {
        var win = GetWindow<FindPrefabChar>("查找预设文字");
        win.Init();
    }

    private static readonly Regex textRegex = new Regex(@"m_Text: ""(\S+)""", RegexOptions.Singleline);
    private static readonly Regex codeRegex = new Regex(@"((\\u\w{4})+)", RegexOptions.Singleline);
    private static readonly Regex chnRegex = new Regex(@"([\u4E00-\u9FA5]+)", RegexOptions.Singleline);

    static public string StringToUnicode(string value)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(value);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < bytes.Length; i += 2)
        {
            sb.AppendFormat("\\u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'), bytes[i].ToString("x").PadLeft(2, '0'));
        }
        return sb.ToString();
    }

    static public string UnicodeToString(string value)
    {
        StringBuilder sb = new StringBuilder();
        value = value.Replace("\\", "");
        string[] unicodes = value.Split('u');
        for (int i = 1; i < unicodes.Length; i++)
        {
            sb.Append((char)int.Parse(unicodes[i], System.Globalization.NumberStyles.HexNumber));
        }
        return sb.ToString();
    }

    static public string ChineseToUnicode(string value)
    {
        var chn_matchs = chnRegex.Matches(value);
        foreach (Match cm in chn_matchs)
        {
            var str = cm.Groups[1].Value;
            var chn = StringToUnicode(str);
            value = value.Replace(str, chn);
        }
        return value;
    }

    static public string UnicodeToChinese(string value)
    {
        var code_matchs = codeRegex.Matches(value);
        foreach (Match cm in code_matchs)
        {
            var code = cm.Groups[1].Value;
            var chn = UnicodeToString(code);
            value = value.Replace(code, chn);
        }
        return value;
    }

    string projectPath = "";
    string findPath = "";
    Dictionary<string, Data> dict = new Dictionary<string, Data>();
    List<Data> searchData = new List<Data>();
    string searchKey = "";
    string lastSearchKey = "";

    GameObject selectPrefab;
    bool isHaveCanvas = false;

    Vector2 scroller = Vector2.zero;

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

    void Init()
    {
        projectPath = Application.dataPath.Replace("Assets", "");
        findPath = Path.Combine(Application.dataPath, "Prefabs");
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();
        EditorGUIUtility.labelWidth = 64;

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
                EditorGUILayout.LabelField(i.ToString(), data.chn_text);
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

    void RefreshSearchText()
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
                if (chn.chn_text.IndexOf(searchKey) != -1)
                    searchData.Add(chn);
            }
        }
    }

    void FindTextReference()
    {
        dict.Clear();
        string[] dirs = Directory.GetFiles(findPath, "*.prefab", SearchOption.AllDirectories);
        for (int i = 0; i < dirs.Length; i++)
        {
            string path = dirs[i];
            path = "Assets" + path.Replace(Application.dataPath, "");
            path = path.Replace("\\", "/");
            if (path.Contains("Prefabs/Ch") || path.Contains("Prefabs/Effect"))
                continue;
            string text = File.ReadAllText(path);
            var text_matchs = textRegex.Matches(text);
            Data data;
            foreach (Match tm in text_matchs)
            {
                string txt = tm.Groups[1].Value;
                if (dict.TryGetValue(txt, out data))
                {
                    if (!data.assetPaths.Contains(path))
                        data.assetPaths.Add(path);
                }
                else
                {
                    string chn_txt = UnicodeToChinese(txt);
                    data = new Data(txt, chn_txt);
                    data.assetPaths.Add(path);
                    dict.Add(txt, data);
                }
            }
            EditorUtility.DisplayProgressBar("查找图片索引...", path, (float)i / dirs.Length);
        }
        EditorUtility.ClearProgressBar();
    }

    void ToCSV(Dictionary<string, Data> dict)
    {
        var filename = Path.Combine(projectPath, "预设文字.csv");
        using (StreamWriter w = new StreamWriter(filename, false, Encoding.UTF8))
        {
            foreach (var item in dict)
            {
                w.WriteLine(string.Format("{0},{1}",
                    item.Key,
                    item.Value.chn_text));
            }
            w.Close();
        }
    }

    void FromCSV(out Dictionary<string, Data> dict)
    {
        dict = new Dictionary<string, Data>();
        var filename = Path.Combine(projectPath, "预设文字.csv");
        Dictionary<string, int> needLoadAbs = new Dictionary<string, int>();
        using (StreamReader sr = new StreamReader(filename))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Replace("\n", "");
                if (line.StartsWith("#"))
                    continue;
                var tempStrs = line.Split(',');
                Data data = new Data(tempStrs[0], tempStrs[1]);
                dict.Add(tempStrs[0], data);
            }
        }
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

}