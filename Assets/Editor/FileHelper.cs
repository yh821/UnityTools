using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
//using LuaInterface;
//using SLua;

namespace Common
{
    //CustomLuaClassAttribute    
    static public class FileHelper
    {
        const string SAVE_PATH = "savePath.txt";

        static public void CreateDirectoryFromFile(string path)
        {
            path = path.Replace('\\', '/');
            var ind = path.LastIndexOf('/');
            if (ind >= 0)
            {
                path = path.Substring(0, ind);
            }
            else
            {
                return;
            }
            CreateDirectory(path);
        }
        static public void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        static public void SaveFile(string path, string content, bool needUtf8 = false)
        {
            CheckFileSavePath(path);
            if (needUtf8)
            {
                var encoding = new UTF8Encoding(false);
                File.WriteAllText(path, content, encoding);
            }
            else
            {
                File.WriteAllText(path, content, Encoding.Default);
            }
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        static public void WriteAllText(string path, string content)
        {
            var encoding = new UTF8Encoding(false);
            File.WriteAllText(path, content, encoding);
        }

        static public void DelFolder(string path)
        {
            if (!IsDirectoryExists(path))
            {
                return;
            }
            Directory.Delete(path, true);
        }

        static public void DelFile(string path)
        {
            if (!IsFileExists(path))
            {
                return;
            }
            File.Delete(path);
        }

        static public void CleanFolder(string path)
        {
            if (!IsDirectoryExists(path))
            {
                return;
            }
            DOCleanFolder(path);
        }

        static void DOCleanFolder(string path)
        {
            DirectoryInfo source = new DirectoryInfo(path);
            FileInfo[] files = source.GetFiles();
            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i].FullName);
            }
            DirectoryInfo[] dirs = source.GetDirectories();
            for (int i = 0; i < dirs.Length; i++)
            {
                DOCleanFolder(dirs[i].FullName);
            }
        }

        public static IEnumerator CleanupDirectory(string path, int count)
        {
            if (Directory.Exists(path) == false)
                yield break;

            string[] filePaths = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            yield return Yielders.EndOfFrame;
            for (int i = 0; i < filePaths.Length; i++)
            {
                File.Delete(filePaths[i]);
                if (i % count == 0)
                {
                    yield return Yielders.EndOfFrame;
                }
            }
        }


        /// <summary>
        /// 读取文件的字符串
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public string ReadFileText(string path, bool reportError = true)
        {
            if (!File.Exists(path))
            {
                if (reportError)
                {
                    Debug.LogError("unable to load file " + path);
                }
                return "";
            }
            var encoding = new UTF8Encoding(false);
            string str = File.ReadAllText(path, encoding);
            return str;
        }

        /// <summary>
        /// 获取场景里实例的路径
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public static string GetSceneGameObjectPath(GameObject go)
        {
            var t = go.transform;
            var path = go.name;
            while (t.parent != null)
            {
                path = t.parent.name + "/" + path;
                t = t.parent;
            }
            return path;
        }

        /// <summary>
        /// 检查某文件夹路径是否存在，如不存在，创建
        /// </summary>
        /// <param name="path"></param>
        static public bool CheckDirection(string path)
        {
            if (!Directory.Exists(path))
            {
                var info = Directory.CreateDirectory(path);
                if (info.Exists)
                {
                    return true;
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// 单纯检查某个文件夹路径是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public bool IsDirectoryExists(string path)
        {
            if (Directory.Exists(path))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查某个文件是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public bool IsFileExists(string path)
        {
            if (File.Exists(path))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="tarPath"></param>
        static public void CopyFile(string path, string tarPath)
        {
            if (!IsFileExists(path))
            {
                return;
            }
            CheckFileSavePath(tarPath);
            File.Copy(path, tarPath, true);
        }


        static public void CopyDirectory(string srcDir,
                                         string tgtDir,
                                         string[] skips_dir_contains = null,
                                         string[] skips_ext = null)
        {
            var dirSep = @"\";
            var osInfo = System.Environment.OSVersion;
            if (osInfo.Platform == PlatformID.MacOSX || osInfo.Platform == PlatformID.Unix)
                dirSep = @"/";

            DirectoryInfo source = new DirectoryInfo(srcDir);
            DirectoryInfo target = new DirectoryInfo(tgtDir);

            if (target.FullName.StartsWith(source.FullName))
            {
                //throw new Exception(CSTRING.GET_CN(7));//"父目录不能拷贝到子目录！"
            }

            if (skips_dir_contains != null)
            {
                for (int j = 0; j < skips_dir_contains.Length; j++)
                {
                    if (srcDir.Contains(skips_dir_contains[j]))
                    {
                        Debug.LogFormat("skip dir {0}", srcDir);
                        return;
                    }
                }

            }


            if (!source.Exists)
            {
                return;
            }

            if (!target.Exists)
            {
                target.Create();
            }

            FileInfo[] files = source.GetFiles();

            for (int i = 0; i < files.Length; i++)
            {
                bool isSkiped = false;
                if (skips_ext != null)
                {
                    for (int j = 0; j < skips_ext.Length; j++)
                    {
                        if (files[i].Name.EndsWith(skips_ext[j]))
                        {
                            isSkiped = true;
                            break;
                        }
                    }
                }
                if (isSkiped)
                {
                    //Debug.LogFormat("skip file {0}", files[i].FullName);
                    continue;
                }
                File.Copy(files[i].FullName, target.FullName + dirSep + files[i].Name, true);
            }

            DirectoryInfo[] dirs = source.GetDirectories();

            for (int j = 0; j < dirs.Length; j++)
            {
                CopyDirectory(dirs[j].FullName,
                              target.FullName + dirSep + dirs[j].Name,
                              skips_dir_contains,
                              skips_ext);
            }
        }

        static public bool CheckFileSavePath(string path)
        {
            CreateDirectoryFromFileName(path);
            return true;
        }

        static HashSet<string> s_IsDirectoryExists = new HashSet<string>();

        static public void CreateDirectoryFromFileName(string filePath)
        {
            var directoryName = Path.GetDirectoryName(filePath);
            CreateDirectoryFromDirectoryName(directoryName);
        }

        static public void ClearDirectoryCache()
        {
        }

        static bool CreateDirectory_Internal(string path)
        {
            Directory.CreateDirectory(path);
            return Directory.Exists(path);
        }
        static public void CreateDirectoryFromDirectoryName(string directoryName)
        {
            if (Directory.Exists(directoryName))
            {
                return;
            }
            CreateDirectory_Internal(directoryName);
        }

#region NotToLua
        /// <summary>
#if UNITY_ANDROID        /// <summary>
        /// 保存bytes
        /// </summary>
        /// <param name="path"></param>
        /// <param name="bytes"></param>
        static public void SaveBytes(string path, byte[] bytes)
        {
            CheckFileSavePath(path);
            File.WriteAllBytes(path, bytes);
        }
#endif
        /// 获取某目录下所有指定类型的文件的路径
        /// </summary>
        /// <param name="path"></param>
        /// <param name="exName"></param>
        /// <returns></returns>
        static public List<string> GetAllFiles(string path, string exName)
        {
            if (!IsDirectoryExists(path))
            {
                return null;
            }
            List<string> names = new List<string>();
            DirectoryInfo root = new DirectoryInfo(path);
            FileInfo[] files = root.GetFiles();
            string ex;
            for (int i = 0; i < files.Length; i++)
            {
                ex = GetExName(files[i].FullName);
                if (ex != exName)
                {
                    continue;
                }
                names.Add(files[i].FullName);
            }
            DirectoryInfo[] dirs = root.GetDirectories();
            if (dirs.Length > 0)
            {
                for (int i = 0; i < dirs.Length; i++)
                {
                    List<string> subNames = GetAllFiles(dirs[i].FullName, exName);
                    if (subNames.Count > 0)
                    {
                        for (int j = 0; j < subNames.Count; j++)
                        {
                            names.Add(subNames[j]);
                        }
                    }
                }
            }

            return names;

        }

        static public List<FileInfo> GetAllFileInfos(string path, string exName)
        {
            if (!IsDirectoryExists(path))
            {
                return null;
            }
            List<FileInfo> names = new List<FileInfo>();
            DirectoryInfo root = new DirectoryInfo(path);
            FileInfo[] files = root.GetFiles();
            string ex;
            for (int i = 0; i < files.Length; i++)
            {
                ex = GetExName(files[i].FullName);
                if (string.IsNullOrEmpty(exName) == false)
                {
                    if (ex != exName)
                    {
                        continue;
                    }
                }
                names.Add(files[i]);
            }
            DirectoryInfo[] dirs = root.GetDirectories();
            if (dirs.Length > 0)
            {
                for (int i = 0; i < dirs.Length; i++)
                {
                    List<FileInfo> subNames = GetAllFileInfos(dirs[i].FullName, exName);
                    if (subNames.Count > 0)
                    {
                        for (int j = 0; j < subNames.Count; j++)
                        {
                            names.Add(subNames[j]);
                        }
                    }
                }
            }

            return names;

        }

        /// <summary>
        /// 获取某目录下所有除了指定类型外的文件的路径
        /// </summary>
        /// <param name="path"></param>
        /// <param name="exName"></param>
        /// <returns></returns>
        static public List<string> GetAllFilesExcept(string path, string[] exName)
        {
            List<string> names = new List<string>();
            DirectoryInfo root = new DirectoryInfo(path);
            FileInfo[] files = root.GetFiles();
            string ex;
            for (int i = 0; i < files.Length; i++)
            {
                ex = GetExName(files[i].FullName);
                if (Array.IndexOf(exName, ex) != -1)
                {
                    continue;
                }
                names.Add(files[i].FullName);
            }
            DirectoryInfo[] dirs = root.GetDirectories();
            if (dirs.Length > 0)
            {
                for (int i = 0; i < dirs.Length; i++)
                {
                    List<string> subNames = GetAllFilesExcept(dirs[i].FullName, exName);
                    if (subNames.Count > 0)
                    {
                        for (int j = 0; j < subNames.Count; j++)
                        {
                            names.Add(subNames[j]);
                        }
                    }
                }
            }

            return names;

        }

        /// <summary>
        /// 获得扩展名
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static public string GetExName(string str)
        {
            string rexStr = @"(?<=\\[^\\]+.)[^\\.]+$|(?<=/[^/]+.)[^/.]+$";
            return StringTools.GetFirstMatch(str, rexStr);

        }
#endregion

#region File Translation
        private static string s_AssetBundleMD5Postfix;
        private static byte[] s_AssetBundleMD5PostfixBytes;

        private static string s_AssetBundleDecryptKey;
        private static byte[] s_AssetBundleDecryptKeyBytes;

        public static string assetBundleMD5Postfix
        {
            get
            {
                return s_AssetBundleMD5Postfix;
            }
            set
            {
                s_AssetBundleMD5Postfix = value;
                if (string.IsNullOrEmpty(value))
                    s_AssetBundleMD5PostfixBytes = null;
                else
                    s_AssetBundleMD5PostfixBytes = Encoding.ASCII.GetBytes(value);
            }
        }

        public static string assetBundleDecryptKey
        {
            get
            {
                return s_AssetBundleDecryptKey;
            }
            set
            {
                s_AssetBundleDecryptKey = value;
                if (string.IsNullOrEmpty(value))
                    s_AssetBundleDecryptKeyBytes = null;
                else
                    s_AssetBundleDecryptKeyBytes = Encoding.ASCII.GetBytes(value);
            }
        }

        public static string BytesToUTF8(byte[] input)
        {
            if (input == null || input.Length <= 0)
            {
                return null;
            }
            return System.Text.Encoding.UTF8.GetString(input);
        }

        public static byte[] XORDecode(byte[] input, string key)
        {
            if (input == null || input.Length <= 0)
            {
                return input;
            }
            if (string.IsNullOrEmpty(key))
            {
                return input;
            }
            var keyByte = System.Text.Encoding.ASCII.GetBytes(key);
            var keyByteLength = keyByte.Length;
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = (byte)(input[i] ^ keyByte[i % keyByteLength]);
            }
            return input;
        }

        public static byte[] XORDecode(byte[] input, byte[] keyByte)
        {
            var keyByteLength = keyByte.Length;
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = (byte)(input[i] ^ keyByte[i % keyByteLength]);
            }
            return input;
        }

        /// <summary>
        /// Decodes the with app config key.
        /// 使用AppConfig中的abmd5Posfix值 作为key加密与解密
        /// </summary>
        /// <returns>The with app config key.</returns>
        /// <param name="input">Input.</param>
        public static byte[] DecodeWithAppConfigKey(byte[] input)
        {
            if (s_AssetBundleMD5PostfixBytes == null)
                return input;
            //if (string.IsNullOrEmpty(s_AssetBundleMD5Postfix))
            //    return input;
            ////			string abmd5Posfix = AppDataModel.AppConfig_GetString ("abmd5Posfix", "");
            ////			if (string.IsNullOrEmpty (abmd5Posfix)) 
            ////			{
            ////				return input;
            ////			}
            return XORDecode(input, s_AssetBundleMD5Postfix);
        }

        public static byte[] XORAssetBundleContent(byte[] input)
        {
            if (s_AssetBundleDecryptKeyBytes == null)
                return input;
            return XORDecode(input, s_AssetBundleDecryptKeyBytes);
        }

        public static string ReadEncodeFileInStreamingAssets(string fileName)
        {
            string filePath = Path.Combine(GamePath.streamingAssetsAssetBundlePath, fileName);
            //这样去判定文件是否存在时，会返回False，因为这本就不是一个文件物理路径
            byte[] fileBytes = null;
            try
            {
                fileBytes = File.ReadAllBytes(filePath);
            }
            catch (Exception e)
            {
                fileBytes = null;
                Debug.LogFormat("Exception ReadEncodeFileInStreamingAssets.filePath:{0},Error:{1}", filePath, e.Message);
            }
            if (fileBytes == null)
            {
                return null;
            }
            return BytesToUTF8(DecodeWithAppConfigKey(fileBytes));
        }

        public static string ReadEncodeFileInWritable(string fileName)
        {
            string filePath = Path.Combine(GamePath.writableAssetBundlePath, fileName);
            if (!File.Exists(filePath))
            {
                Debug.LogFormat("Exception ReadEncodeFileInWritable filePath not exist.fileName:{0},filePath:{1}", fileName, filePath);
                return null;
            }
            return BytesToUTF8(DecodeWithAppConfigKey(File.ReadAllBytes(filePath)));
        }
#endregion
    }
}
