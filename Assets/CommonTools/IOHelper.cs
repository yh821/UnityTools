using System.IO;
using System.Text;

namespace Common
{
	public static class IOHelper
	{
		public static readonly int RED_LIMIT = (int) UnityEngine.Mathf.Pow(2, 21);
		public static readonly int ORANGE_LIMIT = (int) UnityEngine.Mathf.Pow(2, 20);
		public static readonly int YELLOW_LIMIT = (int) UnityEngine.Mathf.Pow(2, 19);

		public static void OpenFolder(string path)
		{
#if UNITY_EDITOR_WIN
			path = path.Replace("/", "\\");
			if (Directory.Exists(path))
				System.Diagnostics.Process.Start("explorer.exe", path);
#endif
		}

		public static void DeleteFolder(string path)
		{
			path = path.Replace("/", "\\");
			var dir = new DirectoryInfo(path);
			//if (Directory.Exists(path))
			//	Directory.Delete(path);
			if (dir.Exists)
				dir.Delete(true);
		}

		public static void CreateDirectory(string path)
		{
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
		}

		public static void CreateDirectoryFromFile(string path)
		{
			path = path.Replace('\\', '/');
			var ind = path.LastIndexOf('/');
			if (ind < 0) return;
			path = path.Substring(0, ind);
			CreateDirectory(path);
		}

		public static void SaveFile(string path, string content)
		{
			CheckFileSavePath(path);
			File.WriteAllText(path, content);
		}

		public static void SaveFile(string path, string[] lines)
		{
			CheckFileSavePath(path);
			File.WriteAllLines(path, lines);
		}

		public static string ReadAllText(string path)
		{
			var encoding = new UTF8Encoding(false);
			return File.ReadAllText(path, encoding);
		}

		public static string[] ReadAllLines(string path)
		{
			var encoding = new UTF8Encoding(false);
			return File.ReadAllLines(path, encoding);
		}

		public static void WriteAllText(string path, string content)
		{
			var encoding = new UTF8Encoding(false);
			File.WriteAllText(path, content, encoding);
		}

		public static bool CheckFileSavePath(string path)
		{
			CreateDirectoryFromFile(path);
			return true;
		}

		public static void CopyFolder(string srcDir, string dstDir,
			//string[] skips_dir = null,
			//string[] skips_ext = null, 
			bool overwrite = true)
		{
			if (!Directory.Exists(srcDir))
				return;

			srcDir = srcDir.Replace("\\", "/");
			dstDir = dstDir.Replace("\\", "/");
			var files = Directory.GetFiles(srcDir, "*.*", SearchOption.AllDirectories);
			foreach (var file in files)
			{
				var srcFile = file.Replace("\\", "/");
				var dstFile = srcFile.Replace(srcDir, dstDir);
				CreateDirectoryFromFile(dstFile);
				File.Copy(srcFile, dstFile, overwrite);
			}
		}


		public static void SelectFile(string path)
		{
#if UNITY_EDITOR_WIN
			path = path.Replace("/", "\\");
			if (File.Exists(path))
				System.Diagnostics.Process.Start("explorer.exe", "/select," + path);
#endif
		}

		public static void OpenFile(string path)
		{
			path = path.Replace("/", "\\");
			if (File.Exists(path))
				System.Diagnostics.Process.Start(path);
		}

		public static void DeleteFile(string path)
		{
			path = path.Replace("/", "\\");
			if (File.Exists(path))
				File.Delete(path);
		}


		#region Encryption-Decryption File

		private static string _encryptKey;
		private static byte[] _encryptKeyBytes;

		public static string EncryptKey
		{
			get => _encryptKey;
			set
			{
				_encryptKey = value;
				_encryptKeyBytes = string.IsNullOrEmpty(value) ? null : Encoding.ASCII.GetBytes(value);
			}
		}

		public static byte[] DecodeWithEncryptKey(byte[] input)
		{
			if (_encryptKeyBytes == null)
				return input;
			return _XORDecode(input, _encryptKeyBytes);
		}

		public static string ReadEncodeFile(string filePath)
		{
			return BytesToUTF8(DecodeWithEncryptKey(File.ReadAllBytes(filePath)));
		}

		public static string BytesToUTF8(byte[] input)
		{
			if (input == null || input.Length <= 0)
			{
				return null;
			}

			return Encoding.UTF8.GetString(input);
		}

		public static byte[] XORDecode(byte[] input, string key)
		{
			if (input == null || input.Length <= 0)
				return input;
			if (string.IsNullOrEmpty(key))
				return input;
			var keyByte = Encoding.ASCII.GetBytes(key);
			return _XORDecode(input, keyByte);
		}

		private static byte[] _XORDecode(byte[] input, byte[] keyByte)
		{
			var len = keyByte.Length;
			for (var i = 0; i < input.Length; i++)
			{
				input[i] ^= keyByte[i % len];
			}

			return input;
		}

		#endregion
	}
}