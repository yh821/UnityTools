using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public partial class PrefabTools : EditorWindow
{
	private class CharData
	{
		public string text;
		public string cn_text;
		public List<string> assetPaths;
		public bool foldout;

		public CharData(string text)
		{
			this.text = text;
			this.cn_text = ChineseToUnicode(text);
			assetPaths = new List<string>();
			foldout = false;
		}

		public CharData(string text, string chn_text)
		{
			this.text = text;
			this.cn_text = chn_text;
			assetPaths = new List<string>();
			foldout = false;
		}
	}

	private static readonly Regex TextRegex = new Regex(@"m_Text: ""(\S+)""", RegexOptions.Singleline);
	private static readonly Regex CodeRegex = new Regex(@"((\\u\w{4})+)", RegexOptions.Singleline);
	private static readonly Regex ChnRegex = new Regex(@"([\u4E00-\u9FA5]+)", RegexOptions.Singleline);

	public static string StringToUnicode(string value)
	{
		var bytes = Encoding.Unicode.GetBytes(value);
		var sb = new StringBuilder();
		for (int i = 0; i < bytes.Length; i += 2)
		{
			sb.AppendFormat("\\u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'),
				bytes[i].ToString("x").PadLeft(2, '0'));
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
			sb.Append((char) int.Parse(unicodes[i], System.Globalization.NumberStyles.HexNumber));
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
	private Dictionary<string, CharData> dict = new Dictionary<string, CharData>();
	private List<CharData> searchData = new List<CharData>();
	private string searchKey = "";
	private string lastSearchKey = "";

	private GameObject selectPrefab;
	private bool isHaveCanvas = false;

	private Vector2 scroller = Vector2.zero;

	private void InitFindChar()
	{
		projectPath = Application.dataPath.Replace("Assets", "");
		//findPath = Path.Combine(Application.dataPath, "Prefabs");
	}

	void OnGUI_FindChar()
	{
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
			CharData charData;
			for (int i = 0; i < searchData.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				charData = searchData[i];
				EditorGUILayout.LabelField(i.ToString(), charData.cn_text);
				charData.foldout = EditorGUILayout.Foldout(charData.foldout, "使用该文本的预设", true);
				EditorGUILayout.EndHorizontal();
				if (charData.foldout)
				{
					foreach (var prefab in charData.assetPaths)
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
		SavePrefabPaths();
		//string[] dirs = Directory.GetFiles(findPath, "*.prefab", SearchOption.AllDirectories);
		var paths = GetPrefabPaths();
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
					data = new CharData(txt, chnTxt);
					data.assetPaths.Add(path);
					dict.Add(txt, data);
				}
			}

			EditorUtility.DisplayProgressBar("查找图片索引...", path, (float) i / len);
		}

		EditorUtility.ClearProgressBar();
	}

	private void ToCSV(Dictionary<string, CharData> dict)
	{
		var filename = Path.Combine(projectPath, "预设文字.csv");
		using var w = new StreamWriter(filename, false, Encoding.UTF8);
		foreach (var item in dict)
		{
			w.WriteLine($"{item.Key},{item.Value.cn_text}");
		}

		w.Close();
	}

	private void FromCSV(out Dictionary<string, CharData> dict)
	{
		dict = new Dictionary<string, CharData>();
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
			var data = new CharData(lines[0], lines[1]);
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