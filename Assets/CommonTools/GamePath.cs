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

		/// <summary>
		/// 项目根目录
		/// </summary>
		public static string rootPath
		{
			get
			{
				if (string.IsNullOrEmpty(_rootPath))
				{
					_rootPath = Application.dataPath.Replace("\\", "/");
					_rootPath = _rootPath.Replace("/Assets", "");
				}

				return _rootPath;
			}
		}

		private static string _rootPath = string.Empty;

#if UNITY_EDITOR
		/// <summary>
		/// Lua根目录
		/// </summary>
		public static string luaPath
		{
			get
			{
				if (string.IsNullOrEmpty(_luaPath))
				{
					_luaPath = Path.Combine(rootPath, ProjectPathOption.Option.luaPath);
					_luaPath = _luaPath.Replace("\\", "/");
				}

				return _luaPath;
			}
		}

		private static string _luaPath = string.Empty;
#endif

		private static bool _isInit;

		//[InitializeOnLoadMethod]
		public static void Init()
		{
			if (_isInit) return;
			_isInit = true;
			Debug.Log("GamePath Init");
#if UNITY_EDITOR
			streamingAssetsPath = Application.streamingAssetsPath;
			streamingAssetsUrl = "file://" + streamingAssetsPath;
			writablePath = Path.Combine(rootPath, ProjectPathOption.Option.writablePath);
			writableAssetUrl = "file://" + writablePath;

			logPath = Path.Combine(rootPath, ProjectPathOption.Option.logPath);
			cachePath = Path.Combine(rootPath, ProjectPathOption.Option.cachePath);
#else
		    streamingAssetsPath = Application.streamingAssetsPath;
		    streamingAssetsUrl = Application.streamingAssetsPath;
		    writablePath = Application.persistentDataPath;
		    writableAssetUrl = "file://" + Application.persistentDataPath;

		    logPath = writablePath;
		    cachePath = writablePath;
#endif
			var platformName = GetPlatformName();
			writableAssetBundlePath = writablePath + "/" + platformName;
			streamingAssetsAssetBundleUrl = streamingAssetsUrl + "/" + platformName;
			streamingAssetsAssetBundlePath = streamingAssetsPath + "/" + platformName;

			IOHelper.CreateDirectory(logPath);
			IOHelper.CreateDirectory(cachePath);
			IOHelper.CreateDirectory(writablePath);
			IOHelper.CreateDirectory(writableAssetBundlePath);
		}

		public static void DumpPaths()
		{
			var pathFile = Path.Combine(writablePath, "path.txt");
			string[] lines =
			{
				"writablePath: " + writablePath,
				"writableAssetBundlePath: " + writableAssetBundlePath,
				"streamingAssetsPath: " + streamingAssetsPath,
				"streamingAssetsAssetBundlePath:" + streamingAssetsAssetBundlePath,
			};
			File.WriteAllLines(pathFile, lines);
#if UNITY_EDITOR
			var sb = new StringBuilder("DumpPaths:");
			foreach (var line in lines)
			{
				sb.AppendLine(line);
			}

			Debug.Log(sb.ToString());
#endif
		}

		/// <summary>
		/// 平台名
		/// </summary>
		/// <returns></returns>
		public static string GetPlatformName()
		{
			switch (Application.platform)
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
				default:
					return string.Empty;
			}
		}

		/// <summary>
		/// 实例在场景的路径
		/// </summary>
		/// <param name="g"></param>
		/// <returns></returns>
		public static string ScenePath(this GameObject g)
		{
			var t = g.transform;
			var path = g.name;
			while (t.parent != null)
			{
				path = t.parent.name + "/" + path;
				t = t.parent;
			}

			return path;
		}
	}


	#region 路径配置

	public class ProjectPathOption
	{
		public const string OptionFile = "ProjectSettings/ProjectPathOption.json";

		private static PathOption _option;
		public static PathOption Option => _option ??= PathOption.LoadOrCreate(OptionFile);
	}

	public class PathOption
	{
		public string writablePath = "Cache";
		public string logPath = "Logs";
		public string cachePath = "Cache";
		public string luaPath = "Assets/Game/Lua";

		public static PathOption LoadOrCreate(string path)
		{
			PathOption pathOption;
			if (System.IO.File.Exists(path))
			{
				using var jsonReader = System.IO.File.OpenText(path);
				pathOption = JsonUtility.FromJson<PathOption>(jsonReader.ReadToEnd());
			}
			else
			{
				pathOption = new PathOption();
				Save(path, pathOption);
			}

			return pathOption;
		}

		public static void Save(string path, PathOption pathOption)
		{
			using var jsonWriter = System.IO.File.CreateText(path);
			jsonWriter.Write(JsonUtility.ToJson(pathOption, true));
		}
	}

	#endregion
}