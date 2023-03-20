using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Common;
using UObject = UnityEngine.Object;
using Newtonsoft.Json;

public class TexturePackerFind : EditorWindow
{
	class Data
	{
		public int index;
		public string name;
		public string assetPath;
		public Texture2D texture;
		public int size;

		public Data(int index, Texture2D texture, string assetPath)
		{
			this.index = index;
			this.texture = texture;
			this.assetPath = assetPath;
			this.name = texture.name;
			this.size = texture.width * texture.height;
		}
	}

	#region PathOption

	const string OPTION_PATH = "Assets/Editor/TexturePacker/TexturePackerOption.json";

	public class OptionData
	{
		public int type;
		public string particlePath;

		public OptionData(string particlePath)
		{
			this.particlePath = particlePath;
			type = (int) EScreenCondition.未优化;
		}
	}

	public Dictionary<string, OptionData> PathOption
	{
		get
		{
			if (mPathOption == null)
			{
				if (File.Exists(OPTION_PATH))
				{
					string content = File.ReadAllText(OPTION_PATH);
					mPathOption = JsonConvert.DeserializeObject<Dictionary<string, OptionData>>(content);
				}
				else
					mPathOption = new Dictionary<string, OptionData>();
			}

			return mPathOption;
		}
		set { mPathOption = value; }
	}

	Dictionary<string, OptionData> mPathOption = null;

	void SavePathOption()
	{
		if (PathOption != null)
		{
			string content = JsonConvert.SerializeObject(PathOption);
			File.WriteAllText(OPTION_PATH, content);
		}
	}

	void SetPathOption(string key, string value = "")
	{
		var data = GetPathOption(key);
		if (data != null)
		{
			if (!string.IsNullOrEmpty(value))
				data.particlePath = value;
		}
		else
		{
			data = new OptionData(value);
			PathOption.Add(key, data);
		}
	}

	OptionData GetPathOption(string key)
	{
		OptionData data;
		if (PathOption.TryGetValue(key, out data))
			return data;
		else
			return null;
	}

	#endregion

	#region IgnoreOption

	//const string IGNORE_PATH = "Assets/Editor/TexturePacker/TextureIgnoreOption.json";
	//public List<string> IgnoreOption
	//{
	//    get
	//    {
	//        if (mIgnoreOption == null)
	//        {
	//            if (File.Exists(IGNORE_PATH))
	//            {
	//                string content = File.ReadAllText(IGNORE_PATH);
	//                mIgnoreOption = JsonConvert.DeserializeObject<List<string>>(content);
	//            }
	//            else
	//                mIgnoreOption = new List<string>();
	//        }
	//        return mIgnoreOption;
	//    }
	//    set
	//    {
	//        mIgnoreOption = value;
	//    }
	//}
	//List<string> mIgnoreOption = null;
	//void SaveIgnoreOption()
	//{
	//    if (IgnoreOption != null)
	//    {
	//        string content = JsonConvert.SerializeObject(IgnoreOption);
	//        File.WriteAllText(IGNORE_PATH, content);
	//    }
	//}
	//void SetIgnoreOption(string key, bool isDelete = false)
	//{
	//    if (isDelete)
	//        IgnoreOption.Remove(key);
	//    else if (!IgnoreOption.Contains(key))
	//        IgnoreOption.Add(key);
	//}

	#endregion

	[MenuItem("Tools/TexturePack/查找图集")]
	static void AddWindow()
	{
		//创建窗口
		GetWindow<TexturePackerFind>("查找图集");
	}

	public enum EScreenCondition
	{
		忽略 = -1,
		未优化,
		已优化,
		全部,
	}

	Vector2 scroller = Vector2.zero;
	List<Data> mAssetsDatas = new List<Data>();
	List<string> mAssetsNames = new List<string>();
	List<Data> searchDatas = new List<Data>();
	Queue<int> recyclePath = new Queue<int>();
	bool isLock = true;
	bool isExists = false;
	EScreenCondition curCondition = EScreenCondition.全部;

	string filerName = "";
	string uiPath = "";
	string curPath = "";
	string searchKey = "";
	string lastSearchKey = "";
	UObject obj = null;

	const string UIPathKey = "TexturePackerOption_UIPath";

	void Awake()
	{
		curPath = EditorPrefs.GetString(UIPathKey, "");
	}

	void OnLostFocus()
	{
		SavePathOption();
		//SaveIgnoreOption();
	}

	void OnDestroy()
	{
		OnLostFocus();
	}

	void OnGUI()
	{
		EditorGUILayout.BeginVertical();
		EditorGUIUtility.labelWidth = 100;

		EditorGUILayout.BeginHorizontal();

		#region 查找图集

		if (GUILayout.Button("查找图集", GUILayout.MaxWidth(200)))
		{
			searchKey = "";
			lastSearchKey = null;
			mAssetsDatas.Clear();
			mAssetsNames.Clear();
			string[] dirs = Directory.GetFiles(Application.dataPath + "/Prefabs", "*.tpsheet",
				SearchOption.AllDirectories);
			Texture2D texture;
			int index = 0;
			for (int i = 0; i < dirs.Length; i++)
			{
				string path = dirs[i];
				path = path.Substring(0, path.LastIndexOf(".tpsheet")) + ".png";
				path = path.Replace("\\", "/");
				if (File.Exists(path))
				{
					path = "Assets" + path.Replace(Application.dataPath, "");
					texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
					if (texture != null)
					{
						mAssetsDatas.Add(new Data(index, texture, path));
						mAssetsNames.Add(texture.name.ToLower());
						SetPathOption(path);
						index++;
					}
				}

				EditorUtility.DisplayProgressBar("查找图集中...", path, (float) i / dirs.Length);
			}

			scroller = Vector2.zero;
			EditorUtility.ClearProgressBar();
		}

		#endregion

		#region UI目录

		curPath = EditorGUILayout.TextField("本地工程的UI目录:", curPath);
		if (curPath != uiPath)
		{
			if (Directory.Exists(curPath))
			{
				uiPath = curPath;
				EditorPrefs.SetString(UIPathKey, uiPath);
			}
		}

		#endregion

		#region 开/关锁

		if (GUILayout.Button("开/关锁", GUILayout.MaxWidth(60)))
		{
			isLock = !isLock;
		}

		#endregion

		#region 更新项目

		if (GUILayout.Button("更新项目", GUILayout.MaxWidth(80)))
		{
			SVNHelper.UpdateFromSVN();
		}

		#endregion

		#region 保存

		if (GUILayout.Button("保存", GUILayout.MaxWidth(100)))
		{
			OnLostFocus();
		}

		#endregion

		EditorGUILayout.EndHorizontal();

		EditorGUIUtility.labelWidth = 50;
		EditorGUILayout.BeginHorizontal();

		EditorGUILayout.BeginVertical(GUILayout.MaxWidth(200));
		curCondition = (EScreenCondition) EditorGUILayout.EnumPopup("筛选条件", curCondition);

		#region 搜索

		searchKey = EditorGUILayout.DelayedTextField("搜索图集", searchKey, GUILayout.MaxWidth(200));
		if (lastSearchKey != searchKey)
		{
			searchKey = searchKey.ToLower();
			lastSearchKey = searchKey;
			searchDatas.Clear();
			if (string.IsNullOrEmpty(searchKey))
			{
				if (curCondition == EScreenCondition.全部)
				{
					foreach (var data in mAssetsDatas)
						searchDatas.Add(data);
				}
				else
				{
					OptionData option;
					foreach (var data in mAssetsDatas)
					{
						option = GetPathOption(data.assetPath);
						if (option != null && option.type == (int) curCondition)
							searchDatas.Add(data);
					}
				}
			}
			else
			{
				for (int i = 0; i < mAssetsNames.Count; i++)
				{
					if (mAssetsNames[i].IndexOf(searchKey) != -1)
						searchDatas.Add(mAssetsDatas[i]);
				}
			}
		}

		#endregion

		EditorGUILayout.EndVertical();

		EditorGUILayout.BeginVertical(GUILayout.MaxWidth(64));

		#region 名字排序

		if (GUILayout.Button("名字排序"))
		{
			searchDatas.Sort((a, b) => string.Compare(a.name, b.name));
		}

		#endregion

		#region 大小排序

		if (GUILayout.Button("大小排序"))
		{
			searchDatas.Sort((a, b) =>
			{
				if (a.size < b.size) return 1;
				else if (a.size > b.size) return -1;
				else return 0;
			});
		}

		#endregion

		EditorGUILayout.EndVertical();

		#region HelpBox

		isExists = false;
		if (string.IsNullOrEmpty(curPath))
			EditorGUILayout.HelpBox("请先填好“本地工程的UI目录”，否则无法编辑图集。例如：D:\\SVN\\M5-A\\UI\\", MessageType.Error);
		else
		{
			if (Directory.Exists(curPath))
			{
				isExists = true;
				EditorGUILayout.HelpBox("v1.2 Y(^o^)Y", MessageType.Info);
			}
			else
				EditorGUILayout.HelpBox("目录不存在!!!!!!", MessageType.Warning);
		}

		#endregion

		EditorGUILayout.EndHorizontal();

		if (mAssetsDatas != null && searchDatas.Count > 0)
		{
			scroller = EditorGUILayout.BeginScrollView(scroller);
			EditorGUIUtility.labelWidth = 0;
			EditorGUILayout.BeginVertical();
			for (int i = 0; i < searchDatas.Count; i++)
				DrawTextureItem(searchDatas[i], i);
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
		}

		while (recyclePath.Count > 0)
		{
			var index = recyclePath.Dequeue();
			mAssetsDatas.RemoveAt(index);
			mAssetsNames.RemoveAt(index);
		}

		EditorGUILayout.EndVertical();
	}

	void DrawTextureItem(Data data, int searchIndex)
	{
		if (data == null)
			return;

		var option = GetPathOption(data.assetPath);

		EditorGUILayout.BeginHorizontal();

		#region 图集

		if (!File.Exists(data.assetPath))
		{
			recyclePath.Enqueue(data.index);
			searchDatas[searchIndex] = null;
			return;
		}

		EditorGUILayout.ObjectField("", data.texture, typeof(Texture2D), false, GUILayout.MaxWidth(64));

		#endregion

		EditorGUILayout.BeginVertical();

		EditorGUILayout.BeginHorizontal();

		#region 编辑图集

		bool canEditor = isExists && !string.IsNullOrEmpty(option.particlePath) &&
		                 Directory.Exists(curPath + option.particlePath);
		if (canEditor)
			GUI.color = Color.green;
		else
			GUI.color = Color.gray;
		if (GUILayout.Button("编辑图集", GUILayout.MaxWidth(64)))
		{
			if (canEditor)
			{
				TexturePackerEditor window = GetWindow<TexturePackerEditor>("编辑图集");
				window.Init(data.texture, curPath + option.particlePath);
			}
		}

		if (canEditor)
			GUI.color = Color.white;

		#endregion

		#region 散图目录

		if (GUILayout.Button("散图目录", GUILayout.MaxWidth(64)))
		{
			if (!string.IsNullOrEmpty(option.particlePath))
				IOHelper.OpenFolder(curPath + option.particlePath);
		}

		GUI.color = Color.white;

		#endregion

		#region 散图路径

		if (isLock)
		{
			if (!string.IsNullOrEmpty(option.particlePath))
				EditorGUILayout.LabelField(curPath + option.particlePath);
		}
		else
			option.particlePath = EditorGUILayout.TextField(option.particlePath);

		#endregion

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();

		#region 打开图集

		if (GUILayout.Button("打开图集", GUILayout.MaxWidth(64)))
		{
			IOHelper.OpenFile(data.assetPath);
		}

		#endregion

		#region 选中图集

		if (GUILayout.Button("选中图集", GUILayout.MaxWidth(64)))
		{
			IOHelper.SelectFile(data.assetPath);
		}

		#endregion

		EditorGUILayout.LabelField(data.assetPath); //图集路径
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();

		#region 已优化

		if (curCondition == EScreenCondition.未优化)
		{
			if (GUILayout.Button("已优化", GUILayout.MaxWidth(64)))
			{
				option.type = (int) EScreenCondition.已优化;
				recyclePath.Enqueue(data.index);
				searchDatas[searchIndex] = null;
			}
		}

		#endregion

		#region 忽略

		if (curCondition != EScreenCondition.忽略)
		{
			if (GUILayout.Button("忽略", GUILayout.MaxWidth(64)))
			{
				option.type = (int) EScreenCondition.忽略;
				recyclePath.Enqueue(data.index);
				searchDatas[searchIndex] = null;
			}
		}

		#endregion

		#region 恢复

		if (curCondition == EScreenCondition.忽略)
		{
			if (GUILayout.Button("恢复", GUILayout.MaxWidth(64)))
			{
				option.type = (int) EScreenCondition.未优化;
				recyclePath.Enqueue(data.index);
				searchDatas[searchIndex] = null;
			}
		}

		#endregion

		#region size

		if (data.size >= IOHelper.RED_LIMIT)
			GUI.color = Color.red;
		else if (data.size >= IOHelper.ORANGE_LIMIT)
			GUI.color = new Color(1f, 0.5f, 0);
		else if (data.size >= IOHelper.YELLOW_LIMIT)
			GUI.color = Color.yellow;
		EditorGUILayout.LabelField(data.name + "  " + data.texture.width + "*" + data.texture.height);
		GUI.color = Color.white;

		#endregion

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();
	}

	void LoadTexture(out List<string> assetPaths, out List<string> assetNames)
	{
		assetPaths = new List<string>();
		assetNames = new List<string>();
		string[] dirs = Directory.GetFiles(Application.dataPath + "/Prefabs", "*.tpsheet", SearchOption.AllDirectories);
		Texture2D texture;
		for (int i = 0; i < dirs.Length; i++)
		{
			string path = dirs[i];
			path = path.Substring(0, path.LastIndexOf(".tpsheet")) + ".png";
			path = path.Replace("\\", "/");
			if (File.Exists(path))
			{
				path = "Assets" + path.Replace(Application.dataPath, "");
				texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				if (texture != null)
				{
					assetPaths.Add(path);
					assetNames.Add(texture.name.ToLower());
					SetPathOption(path);
				}
			}

			EditorUtility.DisplayProgressBar("查找图集中...", dirs[i], (float) i / dirs.Length);
		}

		EditorUtility.ClearProgressBar();
	}
}