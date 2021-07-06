using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
#if UNITY_EDITOR	
using UnityEditor;
using System.Text.RegularExpressions;
#endif

namespace Common
{
    public static class Utility
    {

        public static string GetNodePathTree([NotNull] Transform trans, string split = "/",Transform root = null)
        {
            if (trans == null) throw new ArgumentNullException(nameof(trans));
            var currentNode = trans;
            var nodes = new Stack<string>();

            //不包括根节点
            while (currentNode != null)
            {
                if (root != null && currentNode == root)
                {
                    nodes.Push(currentNode.name);
                    break;
                }
                nodes.Push(currentNode.name);
                currentNode = currentNode.parent;
                
            }

            var sb = new StringBuilder();
            while (nodes.Count > 0)
            {
                if (sb.Length > 0)
                    sb.Append(split);
                sb.Append(nodes.Pop());
            }

            return sb.ToString();
        }


        #region Utility AssetBundle
#if UNITY_EDITOR
        static int m_SimulateAssetBundleInEditor = -1;
        const string kSimulateAssetBundles = "SimulateAssetBundles";
        static int m_SimulateAPPInEditor = -1;
        // Flag to indicate if we want to simulate assetBundles in Editor without building them actually.
        public static bool SimulateAssetBundleInEditor
        {
            get
            {
                if (m_SimulateAssetBundleInEditor == -1)
                    m_SimulateAssetBundleInEditor = EditorPrefs.GetBool(kSimulateAssetBundles, true) ? 1 : 0;

                return m_SimulateAssetBundleInEditor != 0;
            }
            set
            {
                int newValue = value ? 1 : 0;
                if (newValue != m_SimulateAssetBundleInEditor)
                {
                    m_SimulateAssetBundleInEditor = newValue;
                    EditorPrefs.SetBool(kSimulateAssetBundles, value);
                }
            }
        }

        public static bool SimulateAPPInEditor
        {
            get
            {
                if (m_SimulateAPPInEditor == -1)
                    m_SimulateAPPInEditor = EditorPrefs.GetBool("SimulateAPPInEditor", false) ? 1 : 0;

                return m_SimulateAPPInEditor != 0;
            }
            set
            {
                int newValue = value ? 1 : 0;
                if (newValue != m_SimulateAPPInEditor)
                {
                    m_SimulateAPPInEditor = newValue;
                    EditorPrefs.SetBool("SimulateAPPInEditor", value);
                }
            }
        }
#endif
        #endregion

        public static string AssetBundlesOutputPath = "LocalFile";
        public const string PrefabPath = "Prefabs";
        public const string AssetBundleExt = ".ab";
        public const string UITexturePath = "";
        private static string PlatformName = "";
        public delegate bool PathTraveller(string parent, string file, bool isDir, object obj);

        public static void SetupPlatformName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                PlatformName = GetPlatformForAssetBundles(Application.platform);
            }
            else
            {
                PlatformName = name;
            }
        }

        public static string GetPlatformName()
        {
            if (string.IsNullOrEmpty(PlatformName))
            {
                PlatformName = GetPlatformForAssetBundles(Application.platform);
            }
            return PlatformName;
        }

        private static string GetPlatformForAssetBundles(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "Windows";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "OSX";
                // Add more build targets for your own.
                // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
                default:
                    return null;
            }
        }

        #region Editor use
#if UNITY_EDITOR
        /// <summary>
        /// ��ȡ��һ��ƥ��
        /// </summary>
        /// <param name="str"></param>
        /// <param name="regexStr"></param>
        /// <returns></returns>
        static string GetFirstMatch(string str, string regexStr)
        {
            Match m = Regex.Match(str, regexStr);
            if (!string.IsNullOrEmpty(m.ToString()))
            {
                return m.ToString();
            }
            return null;
        }

        /// �����չ��
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string GetFileExName(string str)
        {
            string rexStr = @"(?<=\\[^\\]+.)[^\\.]+$|(?<=/[^/]+.)[^/.]+$";
            return GetFirstMatch(str, rexStr);
        }

        public static string GetPlatformNameForBuild(BuildTarget target)
        {
            return GetPlatformForAssetBundles(target);
            //return GetPlatformForAssetBundles(Application.platform);
        }

        private static string GetPlatformForAssetBundles(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.WebGL:
                    return "WebGL";
                //case BuildTarget.WebPlayer:
                //    return "WebPlayer";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.StandaloneOSX:
                    return "OSX";
                // Add more build targets for your own.
                // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
                default:
                    return null;
            }
        }

        public static void DirSearch(string sDir, PathTraveller callback, bool ShowProgress = false, object obj = null)
        {
            try
            {
                if (Directory.Exists(sDir))
                {
                    foreach (string f in Directory.GetFiles(sDir))
                    {
                        callback(sDir, f, false, obj);
                    }

                    var dirlist = Directory.GetDirectories(sDir);
                    var i = 0;
                    foreach (string d in dirlist)
                    {
#if UNITY_EDITOR
                        if (ShowProgress)
                            EditorUtility.DisplayProgressBar("Parsing Path", d, i / (float)dirlist.Length);
#endif
                        if (callback(sDir, d, true, obj))
                            DirSearch(d, callback, false, obj);
                        i++;
                    }
                }
                else if (File.Exists(sDir))
                {
                    callback(sDir, sDir, false, obj);
                }

            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
#if UNITY_EDITOR	
            if (ShowProgress)
                EditorUtility.ClearProgressBar();
#endif
        }

        #region deploy
        public static string GetDeployPath(BuildTarget target, string deployPath = null)
        {
            if (deployPath == null)
            {
                deployPath = Application.dataPath.Substring(0, Application.dataPath.IndexOf("/Assets")).Replace('\\', '/');
                deployPath = deployPath.Substring(0, deployPath.LastIndexOf("/"));
                deployPath = Path.Combine(deployPath, "deploy").Replace("\\", "/").TrimEnd('/');
            }
            deployPath = $"{deployPath}/{GetPlatformForAssetBundles(target)}";
            return deployPath;
        }

        public static string GetDeployAssetBundlePath(BuildTarget target, string deployPath = null)
        {
            if (deployPath == null)
            {
                deployPath = GetDeployPath(target);
            }
            var path = $"{deployPath}/assetbundles".Replace("\\", "/");
            return path;
        }

        public static string GetDeployPluginPath(string deployPath = null)
        {
            if (deployPath == null)
            {
                deployPath = GetDeployPath(BuildTarget.Android);
            }
            return Path.Combine(deployPath, "Assets/Plugins").Replace("\\", "/");
        }

        public static string GetNodePath([NotNull] Transform trans, bool includeRoot = false, string split = "/")
        {
            if (trans == null) throw new ArgumentNullException(nameof(trans));
            var currentNode = trans;
            var nodes = new Stack<string>();

            //不包括根节点
            while (currentNode != null && PrefabUtility.IsPartOfAnyPrefab(currentNode))
            {
                if (includeRoot == false && PrefabUtility.IsOutermostPrefabInstanceRoot(currentNode.gameObject))
                    break;

                nodes.Push(currentNode.name);
                currentNode = currentNode.parent;
            }

            var sb = new StringBuilder();
            while (nodes.Count > 0)
            {
                if (sb.Length > 0)
                    sb.Append(split);
                sb.Append(nodes.Pop());
            }

            return sb.ToString();
        }

        

        public static string GetPublishPath(BuildTarget target, string publishRootPath, int ver)
        {
            return $"{publishRootPath}/{GetPlatformNameForBuild(target)}/{ver.ToString()}";
        }

        #endregion
#endif
        #endregion
    }
}