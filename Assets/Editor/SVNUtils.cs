using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

[Serializable]
public class SVNUtilsOption : ScriptableObject
{
    public List<string> UpdatePath = new List<string>();
    public List<string> CommitPath = new List<string>();
}

public class SVNUtilsEditor : EditorWindow
{
    public const string GUI_ROOT_NAME = "GUI_ROOT/DefaultCanvas";
    static string ProcPathKey = "SVNOption_proc_path";
    static string PATH = "Assets/Editor/SVNUtilsOption.json";

    public static SVNUtilsOption Option
    {
        get
        {
            if (mOption == null)
            {
                if (File.Exists(PATH))
                {
                    string content = File.ReadAllText(PATH);
                    mOption = JsonConvert.DeserializeObject<SVNUtilsOption>(content);
                }
                else
                    mOption = new SVNUtilsOption();
            }
            return mOption;
        }
        set
        {
            mOption = value;
        }
    }
    public static SVNUtilsOption mOption;

    public static string SvnProcPath
    {
        get
        {
            if (string.IsNullOrEmpty(mSvnProcPath))
            {
                mSvnProcPath = EditorPrefs.GetString(ProcPathKey, "");
                if (string.IsNullOrEmpty(mSvnProcPath))
                {
                    for (int i = 0; i < svnPath.Length; ++i)
                    {
                        foreach (var item in drives)
                        {
                            var path = string.Concat(item, svnPath[i], svnProc);
                            if (File.Exists(path))
                            {
                                mSvnProcPath = path;
                                return mSvnProcPath;
                            }
                        }
                    }
                    mSvnProcPath = EditorUtility.OpenFilePanel("Select TortoiseProc.exe", "c:\\", "exe");
                }
            }
            return mSvnProcPath;
        }
    }
    static string mSvnProcPath = "";
    static readonly string[] drives = { "c:", "d:", "e:", "f:" };
    static readonly string[] svnPath = {
        @"\Program Files (x86)\TortoiseSVN\bin\",
        @"\Program Files\TortoiseSVN\bin\"
    };
    static string svnProc = @"TortoiseProc.exe";

    static string DefaultProjectPath
    {
        get
        {
            var dir = new DirectoryInfo(Application.dataPath + "../");
            return dir.Parent.FullName.Replace('/', '\\');
        }
    }

    string newUpdatePath = "";
    string newCommitPath = "";

    #region 界面
    private void Init()
    {
        minSize = new Vector2(320f, 240f);
        maxSize = new Vector2(720f, 720f);
        Focus();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        #region 更新路径
        EditorGUILayout.LabelField("需更新目录的路径：");
        List<string> update = new List<string>();
        for (int i = 0, len = Option.UpdatePath.Count; i < len; i++)
        {
            string path = Option.UpdatePath[i];
            if (Directory.Exists(path))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField(path);
                if (!GUILayout.Button("移除", GUILayout.Width(100)))
                    update.Add(path);
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.LabelField("-------------------------------------------------------------------------------------------------");
        EditorGUILayout.BeginHorizontal();
        newUpdatePath = EditorGUILayout.TextField(newUpdatePath);
        if (GUILayout.Button("新增", GUILayout.Width(100)))
        {
            if (!string.IsNullOrEmpty(newUpdatePath)
                && !CheckUpdateSame(newUpdatePath)
                && Directory.Exists(newUpdatePath))
                update.Add(newUpdatePath);
        }
        EditorGUILayout.EndHorizontal();

        Option.UpdatePath.Clear();
        for (int i = 0, len = update.Count; i < len; i++)
        {
            Option.UpdatePath.Add(update[i]);
        }
        #endregion

        #region 提交路径
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("需提交目录的路径：");
        List<string> commit = new List<string>();
        for (int i = 0, len = Option.CommitPath.Count; i < len; i++)
        {
            string path = Option.CommitPath[i];
            if (Directory.Exists(path))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField(path);
                if (!GUILayout.Button("移除", GUILayout.Width(100)))
                    commit.Add(path);
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.LabelField("-------------------------------------------------------------------------------------------------");
        EditorGUILayout.BeginHorizontal();
        newCommitPath = EditorGUILayout.TextField(newCommitPath);
        if (GUILayout.Button("新增", GUILayout.Width(100)))
        {
            if (!string.IsNullOrEmpty(newCommitPath)
                && !CheckCommitSame(newCommitPath)
                && Directory.Exists(newCommitPath))
                commit.Add(newCommitPath);
        }
        EditorGUILayout.EndHorizontal();

        Option.CommitPath.Clear();
        for (int i = 0, len = commit.Count; i < len; i++)
        {
            Option.CommitPath.Add(commit[i]);
        }
        #endregion

        #region 程序路径
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("TortoiseSVN.exe路径：");
        EditorGUILayout.LabelField(SvnProcPath);
        #endregion

        EditorGUILayout.EndVertical();
    }

    bool CheckUpdateSame(string str)
    {
        return Option.UpdatePath.Exists(e => e == str);
    }

    bool CheckCommitSame(string str)
    {
        return Option.CommitPath.Exists(e => e == str);
    }

    void OnDestroy()
    {
        EditorPrefs.SetString(ProcPathKey, SvnProcPath);
        if (Option != null)
        {
            string content = JsonConvert.SerializeObject(Option);
            File.WriteAllText(PATH, content);
        }
    }
    #endregion

    #region 命令枚举
    public enum Command
    {
        Log,
        CheckOut,
        Update,
        Commit,
        Add,
        Revert,
        CleanUp,
        Resolve,//解决
        Remove,
        Rename,
        Diff,
        Ignore,//忽略
        Lock,
        UnLock,
    }
    #endregion

    #region 菜单选项
    [MenuItem("Tools/SVN/更新 %&e")]
    public static void UpdateFromSVN()
    {
        ExecuteCommand(Command.Update, GetUpdatePath(), 0);
    }

    [MenuItem("Tools/SVN/提交 %&r")]
    public static void CommitToSVN()
    {
        ExecuteCommand(Command.Commit, GetCommitPath());
    }

    [MenuItem("Tools/SVN/清理")]
    public static void CleanUpFromSVN()
    {
        ExecuteCommand(Command.CleanUp, GetUpdatePath());
    }

    [MenuItem("Tools/SVN/解决")]
    public static void ResolveFromSVN()
    {
        ExecuteCommand(Command.Resolve, GetUpdatePath());
    }

    [MenuItem("Tools/SVN/设置")]
    static void AddWindow()
    {
        //创建窗口
        SVNUtilsEditor window = GetWindow<SVNUtilsEditor>("SVNUtilsOption");
        window.Init();
        window.Show();
    }

    /// <summary>
    /// 执行SVN命令
    /// </summary>
    /// <param name="cmd">命令</param>
    /// <param name="path">操作路径</param>
    /// <param name="closeonend">0:不自动关闭,1:如果没发生错误则自动关闭对话框,
    /// 2:如果没发生错误和冲突则自动关闭对话框,3:如果没有错误、冲突和合并，会自动关闭</param>
    public static void ExecuteCommand(Command cmd, string path, int closeonend = -1)
    {
        if (string.IsNullOrEmpty(SvnProcPath) || string.IsNullOrEmpty(path))
        {
            AddWindow();
            return;
        }

        string cmdString = string.Format("/command:{0} /path:\"{1}\"", cmd.ToString().ToLower(), path);
        if (closeonend >= 0 && closeonend <= 3)
            cmdString += string.Format(" /closeonend:{0}", closeonend);
        System.Diagnostics.Process.Start(SvnProcPath, cmdString);
    }

    static string GetUpdatePath()
    {
        List<string> paths = Option.UpdatePath;
        if (paths.Count <= 0)
            return DefaultProjectPath;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0, len = paths.Count; i < len; i++)
        {
            if (i != 0)
                sb.Append('*');
            sb.Append(paths[i]);
        }
        return sb.ToString();
    }

    static string GetCommitPath()
    {
        List<string> paths = Option.CommitPath;
        if (paths.Count <= 0)
            return DefaultProjectPath;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0, len = paths.Count; i < len; i++)
        {
            if (i != 0)
                sb.Append('*');
            sb.Append(paths[i]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 应用预设模型修改
    /// </summary>
    static public void ApplyPrefabChanges()
    {
        var obj = Selection.activeGameObject;
        if (obj != null)
        {
            var prefab_root = PrefabUtility.FindPrefabRoot(obj);
            var prefab_src = PrefabUtility.GetPrefabParent(prefab_root);
            if (prefab_src != null)
            {
                PrefabUtility.ReplacePrefab(prefab_root, prefab_src, ReplacePrefabOptions.ConnectToPrefab);
                Debug.Log("Updating prefab : " + AssetDatabase.GetAssetPath(prefab_src));
            }
            else
            {
                Debug.Log("Selected has no prefab");
            }
        }
        else
        {
            Debug.Log("Nothing selected");
        }
    }
    #endregion

    #region 右键选项
    private static void ExecuteSelectionSvnCmd(Command cmd, int closeonend = -1)
    {
        ExecuteSelectionSvnCmd(Selection.activeObject, cmd, closeonend);
    }

    public static void ExecuteSelectionSvnCmd(UObject uobj, Command cmd, int closeonend = -1)
    {
        if (uobj == null)
            return;

        string path = AssetDatabase.GetAssetOrScenePath(uobj);
        if (string.IsNullOrEmpty(path))
            return;

        path = Application.dataPath + path.Replace("Assets", "");
        ExecuteCommand(cmd, path, closeonend);
    }

    [MenuItem("GameObject/SVN Command/Log", false, 12)]
    [MenuItem("Assets/SVN Command/Log")]
    public static void SvnLogCommand()
    {
        ExecuteSelectionSvnCmd(Command.Log);
    }

    [MenuItem("GameObject/SVN Command/Revert", false, 12)]
    [MenuItem("Assets/SVN Command/Revert")]
    public static void SvnRevertCommand()
    {
        ExecuteSelectionSvnCmd(Command.Revert, 3);
    }

    [MenuItem("GameObject/SVN Command/Update", false, 12)]
    [MenuItem("Assets/SVN Command/Update")]
    public static void SvnUpdateCommand()
    {
        ExecuteSelectionSvnCmd(Command.Update);
    }

    [MenuItem("GameObject/SVN Command/Commit", false, 12)]
    [MenuItem("Assets/SVN Command/Commit")]
    public static void SvnCommitCommand()
    {
        ExecuteSelectionSvnCmd(Command.Commit);
    }

    [MenuItem("GameObject/SVN Command/Add", false, 12)]
    [MenuItem("Assets/SVN Command/Add")]
    public static void SvnAddCommand()
    {
        ExecuteSelectionSvnCmd(Command.Add);
    }

    [MenuItem("GameObject/SVN Command/Remove", false, 12)]
    [MenuItem("Assets/SVN Command/Remove")]
    public static void SvnRemoveCommand()
    {
        ExecuteSelectionSvnCmd(Command.Remove);
    }
    #endregion

    #region 实例化预设
    [MenuItem("Tools/编辑器工具/实例化预设 #&s")]
    public static void InstantiatePrefab()
    {
        var select = Selection.activeObject;
        if (select != null && PrefabUtility.GetPrefabType(select) == PrefabType.Prefab)
        {
            var target = GetSceneObject(select.name);
            if (target == null)
            {
                target = PrefabUtility.InstantiatePrefab(select) as GameObject;
                var isHaveCanvas = target.GetComponentInChildren<UnityEngine.UI.CanvasScaler>() != null;
                if (!isHaveCanvas && TempParent != null)
                    target.transform.SetParent(TempParent, false);
                target.name = select.name;
                Selection.activeObject = target;
            }
        }
    }

    static Transform TempParent
    {
        get
        {
            if (mTempParent == null)
            {
                var go = GameObject.Find(GUI_ROOT_NAME);
                if (go != null)
                    mTempParent = go.transform;
            }
            return mTempParent;
        }
    }
    static Transform mTempParent = null;

    static GameObject GetSceneObject(string name)
    {
        if (TempParent != null)
        {
            var t = TempParent.Find(name);
            if (t != null)
                return t.gameObject;
        }
        return GameObject.Find(name);
    }
    #endregion
}