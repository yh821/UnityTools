using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Common;
//using UObject = UnityEngine.Object;
using Newtonsoft.Json;

public class TexturePackerFlag : EditorWindow
{
	class Data : OptionData
	{
		public string name;
		public Texture2D texture;
		public int size;

		public Data(Texture2D texture, string assetPath, long timeStamp, bool select)
			: base(assetPath, timeStamp)
		{
			this.texture = texture;
			this.select = select;
			this.name = texture.name;
			this.size = texture.width * texture.height;
		}
	}

	#region 时间戳

	private static DateTime timeStampStartTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static long DateTimeToTimeStamp(DateTime dateTime)
	{
		return (long) (dateTime.ToUniversalTime() - timeStampStartTime).TotalSeconds;
	}

	public static long NowTimeStamp()
	{
		return DateTimeToTimeStamp(DateTime.Now);
	}

	#endregion

	#region PathOption

	const string OPTION_PATH = "Assets/Editor/TexturePacker/TexturePackerFlag.json";

	public class OptionData
	{
		public bool select;
		public string assetPath;
		public TextureImporterFormat format;
		public long addTime = 0;

		public OptionData(string particlePath, long timeStamp)
		{
			this.assetPath = particlePath;
			this.addTime = timeStamp;
			this.format = TextureImporterFormat.ASTC_4x4;
			this.select = true;
		}
	}

	public List<OptionData> PathOption
	{
		get
		{
			if (mPathOption == null)
			{
				if (File.Exists(OPTION_PATH))
				{
					string content = File.ReadAllText(OPTION_PATH);
					mPathOption = JsonConvert.DeserializeObject<List<OptionData>>(content);
				}
				else
					mPathOption = new List<OptionData>();
			}

			return mPathOption;
		}
		set { mPathOption = value; }
	}

	List<OptionData> mPathOption = null;

	void SavePathOption()
	{
		if (PathOption != null)
		{
			string content = JsonConvert.SerializeObject(PathOption, Formatting.Indented);
			File.WriteAllText(OPTION_PATH, content);
			logMsg = "保存数据";
		}
	}

	void SetPathOption(string path, TextureImporterFormat format)
	{
		var option = PathOption.Find((e) => e.assetPath == path);
		option.format = format;
		SavePathOption();
	}

	OptionData GetPathOption(string path)
	{
		return PathOption.Find((e) => e.assetPath == path);
	}

	void DelPathOption(string path)
	{
		var option = PathOption.Find((e) => e.assetPath == path);
		PathOption.Remove(option);
		SavePathOption();
	}

	#endregion

	[MenuItem("Tools/TexturePack/标记图集")]
	static void AddWindow()
	{
		//创建窗口
		GetWindow<TexturePackerFlag>("标记图集");
	}

	Vector2 scroller = Vector2.zero;
	List<Data> mAssetsDatas = new List<Data>();
	Queue<Data> mRecycleDatas = new Queue<Data>();
	string search = "";
	string logMsg = "";
	Texture2D addTexture = null;

	void OnLostFocus()
	{
		SavePathOption();
	}

	void OnDestroy()
	{
		SavePathOption();
	}

	void OnGUI()
	{
		EditorGUIUtility.labelWidth = 100;
		EditorGUILayout.Space();
		EditorGUILayout.BeginHorizontal();
		addTexture =
			EditorGUILayout.ObjectField("", addTexture, typeof(Texture2D), false, GUILayout.MaxWidth(64)) as Texture2D;

		if (GUILayout.Button("添加图集", GUILayout.Width(64), GUILayout.Height(64)))
		{
			if (addTexture == null)
				return;
			if (mAssetsDatas.Count == 0)
				ReadOption();
			var path = AssetDatabase.GetAssetPath(addTexture);
			if (!mAssetsDatas.Exists((e) => e.assetPath == path))
			{
				var timeStamp = NowTimeStamp();
				mAssetsDatas.Add(new Data(addTexture, path, timeStamp, true));
				PathOption.Add(new OptionData(path, timeStamp));
				logMsg = "添加成功";
			}
			else
				logMsg = "重复添加";

			addTexture = null;
		}

		#region 读取数据

		if (GUILayout.Button("读取数据", GUILayout.Width(64), GUILayout.Height(64)))
		{
			search = "";
			scroller = Vector2.zero;
			ReadOption();
		}

		#endregion

		#region 保存

		if (GUILayout.Button("保存", GUILayout.Width(64), GUILayout.Height(64)))
		{
			SavePathOption();
		}

		#endregion

		EditorGUILayout.BeginVertical(GUILayout.Width(64));
		if (GUILayout.Button("名字排序"))
		{
			mAssetsDatas.Sort((a, b) => string.Compare(a.name, b.name));
		}

		if (GUILayout.Button("时间排序"))
		{
			mAssetsDatas.Sort((a, b) =>
			{
				if (a.addTime < b.addTime) return 1;
				else if (a.addTime > b.addTime) return -1;
				else return 0;
			});
		}

		if (GUILayout.Button("大小排序"))
		{
			mAssetsDatas.Sort((a, b) =>
			{
				if (a.size < b.size) return 1;
				else if (a.size > b.size) return -1;
				else return 0;
			});
		}

		EditorGUILayout.EndVertical();

		EditorGUILayout.HelpBox(logMsg, MessageType.Info);
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();
		EditorGUILayout.BeginHorizontal();
		search = EditorGUILayout.TextField("", search, "SearchTextField");
		GUILayout.Label("", "SearchCancelButtonEmpty");
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();
		scroller = EditorGUILayout.BeginScrollView(scroller);
		int inde = 0;
		foreach (var info in mAssetsDatas)
		{
			if (info.name.ToLower().Contains(search.ToLower()))
			{
				DrawTextureItem(info, inde);
				inde++;
			}
		}

		while (mRecycleDatas.Count > 0)
		{
			var data = mRecycleDatas.Dequeue();
			mAssetsDatas.Remove(data);
		}

		EditorGUILayout.EndScrollView();

		EditorGUILayout.Space();
		if (GUILayout.Button("Apply All", GUILayout.Height(27)))
		{
			if (EditorUtility.DisplayDialog("提示", "是否应用全部图集格式(耗时较久)", "确定", "取消"))
			{
				for (int i = 0, len = mAssetsDatas.Count; i < len; i++)
				{
					var data = mAssetsDatas[i];
					EditorUtility.DisplayProgressBar("应用图集格式中...", data.assetPath, (float) i / len);
					if (!data.select)
						continue;
					ApplyFormat(data.assetPath, data.format);
				}

				AssetDatabase.Refresh();
				EditorUtility.ClearProgressBar();
			}
		}

		EditorGUILayout.Space();
	}

	void DrawTextureItem(Data data, int searchIndex)
	{
		if (data == null)
			return;
		EditorGUILayout.BeginHorizontal("Box");
		EditorGUILayout.ObjectField("", data.texture, typeof(Texture2D), false, GUILayout.MaxWidth(64));
		EditorGUILayout.BeginVertical();

		EditorGUILayout.BeginHorizontal();
		data.format = (TextureImporterFormat) EditorGUILayout.EnumPopup(data.format, GUILayout.MaxWidth(130));
		EditorGUILayout.LabelField(data.assetPath); //图集路径
		data.select = EditorGUILayout.Toggle(data.select, GUILayout.Width(14));
		EditorGUILayout.EndHorizontal();

		if (GUILayout.Button("Apply", GUILayout.MaxWidth(130)))
		{
			SetPathOption(data.assetPath, data.format);
			ApplyFormat(data.assetPath, data.format);
			AssetDatabase.Refresh();
		}

		EditorGUILayout.BeginHorizontal();

		#region size

		if (data.size >= IOHelper.RED_LIMIT)
			GUI.color = Color.red;
		else if (data.size >= IOHelper.ORANGE_LIMIT)
			GUI.color = new Color(1f, 0.5f, 0);
		else if (data.size >= IOHelper.YELLOW_LIMIT)
			GUI.color = Color.yellow;
		EditorGUILayout.LabelField(string.Format("{0}: {1} x {2}", data.name, data.texture.width, data.texture.height));
		GUI.color = Color.white;

		#endregion

		if (GUILayout.Button("Delete", GUILayout.MaxWidth(100)))
		{
			DelPathOption(data.assetPath);
			mRecycleDatas.Enqueue(data);
		}

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();
	}

	void ApplyFormat(string assetPath, TextureImporterFormat format)
	{
		TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
		TextureImporterPlatformSettings setting = ti.GetPlatformTextureSettings("iPhone");
		if (setting.format == format)
			return;
		setting.format = format;
		ti.SetPlatformTextureSettings(setting);
		ti.SaveAndReimport();
	}

	void ReadOption()
	{
		mAssetsDatas.Clear();
		foreach (var data in PathOption)
		{
			if (File.Exists(data.assetPath))
			{
				var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(data.assetPath);
				if (texture != null)
				{
					mAssetsDatas.Add(new Data(texture, data.assetPath, data.addTime, data.select));
				}
			}
		}

		logMsg = "读取数据";
	}
}