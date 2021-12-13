using System;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Common
{
    /// <summary>
    /// 游戏路径管理
    /// </summary>
    //CustomLuaClassAttribute    
    public static class GamePath
    {
        /// <summary>
        /// 外部资源的根目录, 一般用于配置存贮和读取
        /// </summary>
        public static string writablePath { get; private set; }
        /// <summary>
        /// 外部资源的url
        /// </summary>
        public static string writableAssetUrl { get; private set; }
        /// <summary>
        /// 外部存放Assetbundle的目录
        /// </summary>
        public static string writableAssetBundlePath { get; private set; }
        /// <summary>
        /// 内部资源路径
        /// </summary>
        public static string streamingAssetsPath { get; private set; }
        /// <summary>
        /// 内部资源url
        /// </summary>
        public static string streamingAssetsUrl { get; private set; }
        /// <summary>
        /// 内部Assetbundle url
        /// </summary>
        public static string streamingAssetsAssetBundleUrl { get; private set; }
        /// <summary>
        /// 内部Assetbundle目录
        /// </summary>
        public static string streamingAssetsAssetBundlePath { get; private set; }
        /// <summary>
        /// 日志打印路径
        /// </summary>
        public static string logPath { get; private set; }
        /// <summary>
        /// 本地缓存路径
        /// </summary>
        public static string cachePath { get; private set; }

        private static bool m_IsInit;

#if UNITY_EDITOR
        /// <summary>
        /// 初始化
        /// </summary>
        [InitializeOnLoadMethod]
#endif
        public static void Init()
        {
            if (m_IsInit) return;
            m_IsInit = true;
            Debug.Log("GamePath Init");

            if (Application.isEditor)
            {
                var rootPath = Application.dataPath.Replace("/Assets", "");
                streamingAssetsPath = Application.streamingAssetsPath;
                streamingAssetsUrl = "file://" + streamingAssetsPath;
                writablePath = Path.Combine(rootPath, ProjectExtensionSettings.editorCustom.writablePath);
                writableAssetUrl = "file://" + writablePath;

                logPath = Path.Combine(rootPath, ProjectExtensionSettings.editorCustom.logPath);
                //cachePath = Path.Combine(rootPath, ProjectExtensionSettings.editorCustom.cachePath);

                FileHelper.CreateDirectory(logPath);
                //FileHelper.CreateDirectory(cachePath);
            }
            else
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    var path = Application.persistentDataPath;
                    var index = path.LastIndexOf("/", StringComparison.Ordinal);
                    if (index != -1)
                        path = path.Substring(0, index + 1);

                    streamingAssetsPath = Application.streamingAssetsPath;
                    streamingAssetsUrl = Application.streamingAssetsPath;
                    writablePath = path;
                    writableAssetUrl = "file://" + path;
                }
                else
                {
                    streamingAssetsPath = Application.streamingAssetsPath;
                    streamingAssetsUrl = Application.streamingAssetsPath;
                    writablePath = Application.persistentDataPath;
                    writableAssetUrl = "file://" + Application.persistentDataPath;
                }
                logPath = writablePath;
                cachePath = writablePath;
            }
            var platformName = Utility.GetPlatformName();

            writableAssetBundlePath = writablePath + "/" + platformName;
            streamingAssetsAssetBundleUrl = streamingAssetsUrl + "/" + platformName;
            streamingAssetsAssetBundlePath = streamingAssetsPath + "/" + platformName;

            //FileHelper.CreateDirectory(writablePath);
            //FileHelper.CreateDirectory(writableAssetBundlePath);
        }

        public static void DumpPaths()
        {
            var pathtxt = Path.Combine(writablePath, "path.txt");
            string[] lines = {
                "writablePath: " + writablePath,
                "writableAssetBundlePath: " + writableAssetBundlePath,
                "streamingAssetsPath: " + streamingAssetsPath,
                "streamingAssetsAssetBundlePath:" + streamingAssetsAssetBundlePath,
            };
            File.WriteAllLines(pathtxt, lines);
#if UNITY_EDITOR
            var sb = new StringBuilder("DumpPaths:");
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }
            Debug.LogWarning(sb.ToString());
#endif
        }


#if UNITY_EDITOR
        public static string GetLocalFilePathEditor()
        {
            var path = Application.dataPath;
            int index = path.LastIndexOf("/", StringComparison.Ordinal);
            if (index != -1)
            {
                path = path.Substring(0, index + 1);
            }
            if (path.EndsWith("/") == false)
            {
                path += "/";
            }
            return path + "LocalFile/";
        }

        public static string GetConfigPathEditor()
        {
            return GetLocalFilePathEditor();
        }

        public static string GetLuaPathEditor()
        {
            return GetLocalFilePathEditor() + "lua/";
        }
#endif
    }

}
