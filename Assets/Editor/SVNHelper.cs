using System.Diagnostics;
using System.IO;
using UnityEditor;
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

        #region 实例化预设

        public const string UGUI_ROOT_NAME = "GameRoot/BaseView/Root";

        [MenuItem("Tools/实例化预设 #&s")]
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
                    var go = GameObject.Find(UGUI_ROOT_NAME);
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
}