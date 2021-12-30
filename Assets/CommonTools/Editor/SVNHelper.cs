using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Common
{
    public class SVNHelper
    {
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
            Resolve, //解决
            Remove,
            Rename,
            Diff,
            Ignore,
            Lock,
            UnLock,
        }

        #endregion

        private static string _defaultProjectPath = string.Empty;

        private static string DefaultProjectPath
        {
            get
            {
                if (string.IsNullOrEmpty(_defaultProjectPath))
                {
                    var dir = new DirectoryInfo(Application.dataPath + "../");
                    _defaultProjectPath = dir.Parent.FullName.Replace('/', '\\');
                }

                return _defaultProjectPath;
            }
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
            var cmdStr = "/c tortoiseproc.exe /command:{0} /path:\"{1}\"";
            cmdStr = string.Format(cmdStr, cmd.ToString().ToLower(), path);
            if (closeonend >= 0 && closeonend <= 3)
                cmdStr += $" /closeonend:{closeonend}";
            var info = new ProcessStartInfo("cmd.exe", cmdStr);
            info.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(info);
        }

        public static void ExecuteCommand(Command cmd, List<string> paths, int closeonend = -1)
        {
	        var sb = new StringBuilder();
	        for (int i = 0, len = paths.Count; i < len; i++)
	        {
		        if (i == 0)
			        sb.Append(paths[i]);
		        else
			        sb.Append('*').Append(paths[i]);
	        }
	        ExecuteCommand(cmd, sb.ToString(), closeonend);
        }

        #region 菜单选项

        [MenuItem("Tools/SVN/更新 %&e")]
        public static void UpdateFromSVN()
        {
            ExecuteCommand(Command.Update, DefaultProjectPath, 0);
        }

        [MenuItem("Tools/SVN/提交 %&r")]
        public static void CommitToSVN()
        {
            ExecuteCommand(Command.Commit, DefaultProjectPath);
        }

        [MenuItem("Tools/SVN/清理")]
        public static void CleanUpFromSVN()
        {
            ExecuteCommand(Command.CleanUp, DefaultProjectPath);
        }

        [MenuItem("Tools/SVN/解决")]
        public static void ResolveFromSVN()
        {
            ExecuteCommand(Command.Resolve, DefaultProjectPath);
        }

        #endregion

        #region 右键选项

        private static void ExecuteSelectionSvnCmd(Command cmd, int closeonend = -1)
        {
            ExecuteSelectionSvnCmd(Selection.activeObject, cmd, closeonend);
        }

        public static void ExecuteSelectionSvnCmd(Object obj, Command cmd, int closeonend = -1)
        {
            if (obj == null)
                return;

            string path = AssetDatabase.GetAssetOrScenePath(obj);
            if (string.IsNullOrEmpty(path))
                return;

            path = Path.GetFullPath(path);
            path = $"{path}*{path}.meta";
            ExecuteCommand(cmd, path, closeonend);
        }

        [MenuItem("Assets/SVN Command/Log")]
        public static void SvnLogCommand()
        {
            ExecuteSelectionSvnCmd(Command.Log);
        }

        [MenuItem("Assets/SVN Command/Revert")]
        public static void SvnRevertCommand()
        {
            ExecuteSelectionSvnCmd(Command.Revert, 3);
        }

        [MenuItem("Assets/SVN Command/Update")]
        public static void SvnUpdateCommand()
        {
            ExecuteSelectionSvnCmd(Command.Update);
        }

        [MenuItem("Assets/SVN Command/Commit")]
        public static void SvnCommitCommand()
        {
            ExecuteSelectionSvnCmd(Command.Commit);
        }

        [MenuItem("Assets/SVN Command/Add")]
        public static void SvnAddCommand()
        {
            ExecuteSelectionSvnCmd(Command.Add);
        }

        [MenuItem("Assets/SVN Command/Remove")]
        public static void SvnRemoveCommand()
        {
            ExecuteSelectionSvnCmd(Command.Remove);
        }

        #endregion

        [MenuItem("Tools/SVN/路径配置")]
        public static void OpenPathOption()
        {
	        var win = EditorWindow.GetWindow<SVNHelperEditor>("SVN路径配置");
	        win.InitPathGui();
        }

        public const string CommitPathKey = "SVNHelper.CommitPathList";
        public const string UpdatePathKey = "SVNHelper.UpdatePathList";

        public static List<string> GetPaths(string content)
        {
	        var pathArray = content.Split('|');
	        if (string.IsNullOrEmpty(pathArray[0]))
		        pathArray[0] = DefaultProjectPath;
	        return new List<string>(pathArray);
        }

        public static List<string> GetCommitPaths()
        {
	        return GetPaths(EditorPrefs.GetString(CommitPathKey));
        }

        public static List<string> GetUpdatePaths()
        {
	        return GetPaths(EditorPrefs.GetString(UpdatePathKey));
        }
    }

    public class SVNHelperEditor : EditorWindow
    {
	    public List<string> CommitPathList;
	    public List<string> UpdatePathList;

        private SerializedObject mPathSerializedObject;
	    private SerializedProperty mCommitPathSerializedProperty;
	    private ReorderableList mCommitPathReorderableList;
	    private SerializedProperty mUpdatePathSerializedProperty;
	    private ReorderableList mUpdatePathReorderableList;

        private const int BtnWidth = 32;
	    private const int Padding = 4;
	    private const int WidthPadding = BtnWidth + Padding;

        void OnLostFocus()
	    {
		    SavePath();
	    }

        void OnGUI()
        {
	        mCommitPathReorderableList?.DoLayoutList();
	        mUpdatePathReorderableList?.DoLayoutList();
        }

        public void InitPathGui()
	    {
		    CommitPathList = SVNHelper.GetCommitPaths();
		    UpdatePathList = SVNHelper.GetUpdatePaths();
		    mPathSerializedObject = new SerializedObject(this);

		    mCommitPathSerializedProperty = mPathSerializedObject.FindProperty("CommitPathList");
		    mCommitPathReorderableList = new ReorderableList(mPathSerializedObject, mCommitPathSerializedProperty)
		    {
			    drawHeaderCallback = rect => GUI.Label(rect, "提交路径:"),
			    drawElementCallback = (rect, index, selected, focused) =>
			    {
				    var element = mCommitPathSerializedProperty.GetArrayElementAtIndex(index);
				    if (GUI.Button(new Rect(rect.x, rect.y + 2, BtnWidth, rect.height - 4),
					    EditorGUIUtility.IconContent("Folder Icon", "选择文件夹")))
				    {
					    var path = EditorUtility.OpenFolderPanel("选择提交文件夹", element.stringValue, "");
					    SetPath(path, element);
				    }

				    if (GUI.Button(new Rect(rect.x + BtnWidth + Padding, rect.y + 2, BtnWidth, rect.height - 4),
					    EditorGUIUtility.IconContent("TextAsset Icon", "选择文件")))
				    {
					    var path = EditorUtility.OpenFilePanel("选择提交文件", element.stringValue, "*.*");
					    SetPath(path, element);
				    }

				    EditorGUI.LabelField(new Rect(rect.x + WidthPadding * 2, rect.y, rect.width - WidthPadding * 2, rect.height),
					    element.stringValue);
			    }
		    };

		    mUpdatePathSerializedProperty = mPathSerializedObject.FindProperty("UpdatePathList");
		    mUpdatePathReorderableList = new ReorderableList(mPathSerializedObject, mUpdatePathSerializedProperty)
		    {
			    drawHeaderCallback = rect => GUI.Label(rect, "更新路径:"),
			    drawElementCallback = (rect, index, selected, focused) =>
			    {
				    var element = mUpdatePathSerializedProperty.GetArrayElementAtIndex(index);
				    if (GUI.Button(new Rect(rect.x, rect.y + 2, BtnWidth, rect.height - 4),
					    EditorGUIUtility.IconContent("Folder Icon", "选择文件夹")))
				    {
					    var path = EditorUtility.OpenFolderPanel("选择更新文件夹", element.stringValue, "");
					    SetPath(path, element);
				    }

				    if (GUI.Button(new Rect(rect.x + BtnWidth + Padding, rect.y + 2, BtnWidth, rect.height - 4),
					    EditorGUIUtility.IconContent("TextAsset Icon", "选择文件")))
				    {
					    var path = EditorUtility.OpenFilePanel("选择更新文件", element.stringValue, "*.*");
					    SetPath(path, element);
				    }

				    EditorGUI.LabelField(new Rect(rect.x + WidthPadding * 2, rect.y, rect.width - WidthPadding * 2, rect.height),
					    element.stringValue);
			    }
		    };
        }

	    private void SetPath(string path, SerializedProperty element)
	    {
		    if (!string.IsNullOrEmpty(path))
		    {
			    path = path.Replace('/', '\\');
			    element.stringValue = path;
		    }
        }

	    private void SavePath()
	    {
		    mPathSerializedObject.ApplyModifiedProperties();

		    for (int i = 0; i < CommitPathList.Count; i++)
		    {
			    if (string.IsNullOrEmpty(CommitPathList[i]))
				    CommitPathList.RemoveAt(i);
		    }
		    EditorPrefs.SetString(SVNHelper.CommitPathKey, string.Join("|", CommitPathList));

		    for (int i = 0; i < UpdatePathList.Count; i++)
		    {
			    if (string.IsNullOrEmpty(UpdatePathList[i]))
				    UpdatePathList.RemoveAt(i);
		    }
		    EditorPrefs.SetString(SVNHelper.UpdatePathKey, string.Join("|", UpdatePathList));
        }

    }
}