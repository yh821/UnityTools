using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Common;
using UObject = UnityEngine.Object;

public class TexturePackerEditor : EditorWindow
{
	static string ProcPathKey = "TexturePackerPath";

	public static string TexturePackerPath
	{
		get
		{
			if (string.IsNullOrEmpty(mTexturePackPath))
			{
				mTexturePackPath = EditorPrefs.GetString(ProcPathKey, "");
				if (string.IsNullOrEmpty(mTexturePackPath))
				{
					for (int i = 0; i < svnPath.Length; ++i)
					{
						foreach (var item in drives)
						{
							var path = string.Concat(item, svnPath[i], svnProc);
							if (File.Exists(path))
							{
								mTexturePackPath = path;
								return mTexturePackPath;
							}
						}
					}

					mTexturePackPath = EditorUtility.OpenFilePanel("Select TexturePacker.exe", "c:\\", "exe");
				}
			}

			return mTexturePackPath;
		}
	}

	static string mTexturePackPath = "";
	static readonly string[] drives = {"c:", "d:", "e:", "f:"};

	static readonly string[] svnPath =
	{
		@"\Program Files (x86)\TexturePacker\bin\",
		@"\Program Files\TexturePacker\bin\",
		@"\Program Files (x86)\CodeAndWeb\TexturePacker\bin\",
		@"\Program Files\CodeAndWeb\TexturePacker\bin\",
	};

	static string svnProc = @"TexturePacker.exe";

	public enum ETrimMode
	{
		None, //不裁剪
		Trim, //裁剪透明区域,但使用原始贴图大小
		Crop, //裁剪透明区域,使用剪裁后的贴图大小(刷新位置)
		CropKeepPos, //裁剪透明区域,使用裁剪后的贴图大小(位置不变)
	}

	static void BuildAtlas(string particlePath, string atlasPath, ETrimMode mode = ETrimMode.CropKeepPos,
		int border = 1, int shape = 1, int maxSize = 2048)
	{
		System.Diagnostics.Process process = new System.Diagnostics.Process();
		process.StartInfo.FileName = Application.dataPath + "/Editor/TexturePacker/TPackageTool.bat";
		process.StartInfo.UseShellExecute = true;
		process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" {2} {3} {4} {5} {6}", particlePath,
			TexturePackerPath, atlasPath, mode, maxSize, border, shape);
		process.Start();
		process.WaitForExit();
		process.Close();
	}

	[MenuItem("Tools/TexturePack/编辑图集")]
	public static void AddWindow()
	{
		//创建窗口
		GetWindow<TexturePackerEditor>("编辑图集");
	}

	public void Init(Texture2D tex2D, string path)
	{
		inputTexture = tex2D;
		particlePath = path;
	}

	Texture2D inputTexture = null;
	string particlePath = "";
	ETrimMode mode = ETrimMode.CropKeepPos;
	int border = 2;
	int shape = 2;
	int mMaxSize = 2048;
	Vector2 scroller = Vector2.zero;
	List<bool> SelectToggle = new List<bool>();
	bool isLock = true;

	private void OnGUI()
	{
		EditorGUILayout.BeginVertical();

		EditorGUILayout.BeginHorizontal();
		EditorGUIUtility.labelWidth = 0;
		inputTexture =
			(Texture2D) EditorGUILayout.ObjectField("", inputTexture, typeof(Texture2D), false, GUILayout.MaxWidth(64));
		EditorGUILayout.BeginVertical();

		EditorGUILayout.BeginHorizontal();

		#region 散图路径

		if (GUILayout.Button("散图路径", GUILayout.MaxWidth(100)))
		{
			if (!string.IsNullOrEmpty(particlePath))
				IOHelper.OpenFolder(particlePath);
		}

		#endregion

		#region 更新

		if (!string.IsNullOrEmpty(particlePath))
		{
			if (GUILayout.Button("更新", GUILayout.MaxWidth(60)))
			{
				SVNHelper.ExecuteCommand(SVNHelper.Command.Update, particlePath);
			}
		}

		#endregion

		#region 提交

		if (!string.IsNullOrEmpty(particlePath))
		{
			if (GUILayout.Button("提交", GUILayout.MaxWidth(60)))
			{
				SVNHelper.ExecuteCommand(SVNHelper.Command.Commit, particlePath);
			}
		}

		#endregion

		particlePath = EditorGUILayout.TextField(particlePath);

		#region 开/关锁

		if (GUILayout.Button("开/关锁", GUILayout.MaxWidth(60)))
		{
			isLock = !isLock;
		}

		#endregion

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();

		#region size

		if (inputTexture != null)
		{
			var size = inputTexture.width * inputTexture.height;
			if (size >= IOHelper.RED_LIMIT)
				GUI.color = Color.red;
			else if (size >= IOHelper.ORANGE_LIMIT)
				GUI.color = new Color(1f, 0.5f, 0);
			else if (size >= IOHelper.YELLOW_LIMIT)
				GUI.color = Color.yellow;
			EditorGUILayout.LabelField(inputTexture.name + "  " + inputTexture.width + "*" + inputTexture.height);
			GUI.color = Color.white;
		}

		#endregion

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.EndVertical();
		EditorGUILayout.EndHorizontal();
		EditorGUIUtility.labelWidth = 40;
		if (inputTexture != null && Directory.Exists(particlePath))
		{
			var assetPath = AssetDatabase.GetAssetPath(inputTexture);
			UObject[] objs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
			string[] dirs = Directory.GetFiles(particlePath, "*.png", SearchOption.AllDirectories);
			//SetSelectToggle(objs, dirs);

			EditorGUILayout.BeginHorizontal();

			#region Option

			mode = (ETrimMode) EditorGUILayout.EnumPopup("Trim:", mode, GUILayout.MaxWidth(128));
			border = Mathf.Clamp(EditorGUILayout.IntField("Border:", border, GUILayout.MaxWidth(70)), 0, 5);
			shape = Mathf.Clamp(EditorGUILayout.IntField("Shape:", shape, GUILayout.MaxWidth(70)), 0, 5);
			mMaxSize = Mathf.Clamp(EditorGUILayout.IntField("MaxSize:", mMaxSize, GUILayout.MaxWidth(100)), 2, 4096);

			#endregion

			EditorGUILayout.EndHorizontal();

			#region Build

			if (GUILayout.Button("Build"))
			{
				string altasPath = Application.dataPath.Replace("Assets", "") +
				                   assetPath.Substring(0, assetPath.LastIndexOf(".png"));
				//System.Text.StringBuilder sb = new System.Text.StringBuilder();
				//for (int i = 0; i < dirs.Length; i++)
				//{
				//    if (SelectToggle[i])
				//        sb.Append(dirs[i]).Append(" ");
				//}
				BuildAtlas(particlePath, altasPath, mode, border, shape, mMaxSize);
			}

			#endregion

			EditorGUILayout.BeginHorizontal();

			#region 全选

			//if (GUILayout.Button("全选", GUILayout.MaxWidth(50)))
			//{
			//    for (int i = 0; i < SelectToggle.Count; i++)
			//        SelectToggle[i] = true;
			//}

			#endregion

			#region 全不选

			//if (GUILayout.Button("全不选", GUILayout.MaxWidth(50)))
			//{
			//    for (int i = 0; i < SelectToggle.Count; i++)
			//        SelectToggle[i] = false;
			//}

			#endregion

			if (dirs.Length != objs.Length - 1)
				GUI.color = new Color(1, 0.2f, 0.2f);
			EditorGUILayout.LabelField(string.Format("散图[{0}]：", dirs.Length), GUILayout.MinWidth(140));
			EditorGUILayout.LabelField(string.Format("精灵[{0}]：", objs.Length - 1), GUILayout.MinWidth(50));
			GUI.color = Color.white;
			EditorGUILayout.EndHorizontal();

			scroller = EditorGUILayout.BeginScrollView(scroller);
			if (dirs.Length >= objs.Length - 1)
			{
				for (int i = 0; i < dirs.Length; i++)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(i.ToString(), GUILayout.MaxWidth(25));
					var fileName = Path.GetFileNameWithoutExtension(dirs[i]);

					#region 打开

					if (GUILayout.Button("打开", GUILayout.MaxWidth(30)))
					{
						IOHelper.OpenFile(dirs[i]);
					}

					#endregion

					#region 选中

					if (GUILayout.Button("选中", GUILayout.MaxWidth(30)))
					{
						IOHelper.SelectFile(dirs[i]);
					}

					#endregion

					#region 删除

					if (!isLock)
					{
						if (GUILayout.Button("删除", GUILayout.MaxWidth(30)))
						{
							IOHelper.DeleteFile(dirs[i]);
						}
					}

					#endregion

					//SelectToggle[i] = EditorGUILayout.Toggle(SelectToggle[i], GUILayout.MaxWidth(10));
					var obj = GetByName(objs, fileName);
					if (obj != null)
					{
						EditorGUILayout.LabelField(fileName, GUILayout.MinWidth(50));

						#region 引用

						if (GUILayout.Button("引用", GUILayout.MaxWidth(40)))
						{
							var win = PrefabTools.OpenWindow();
							win.SetFindImage(obj as Sprite);
						}

						#endregion

						EditorGUILayout.ObjectField(obj, typeof(Texture2D), false, GUILayout.MinWidth(50));
					}
					else
					{
						GUI.color = new Color(1, 0.2f, 0.2f);
						EditorGUILayout.LabelField(fileName, GUILayout.MinWidth(50));
						GUI.color = Color.white;
					}

					EditorGUILayout.EndHorizontal();
				}
			}
			else
			{
				for (int i = 1; i < objs.Length; i++) //objs第一个是自身
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(i.ToString(), GUILayout.MaxWidth(25));
					var fileName = objs[i].name;
					if (GetByName(dirs, fileName))
					{
						#region 打开

						if (GUILayout.Button("打开", GUILayout.MaxWidth(30)))
						{
							IOHelper.OpenFile(dirs[i]);
						}

						#endregion

						#region 选中

						if (GUILayout.Button("选中", GUILayout.MaxWidth(30)))
						{
							IOHelper.OpenFolder(dirs[i]);
						}

						#endregion

						#region 删除

						if (!isLock)
						{
							if (GUILayout.Button("删除", GUILayout.MaxWidth(30)))
							{
								IOHelper.DeleteFile(dirs[i]);
							}
						}

						#endregion

						//SelectToggle[i] = EditorGUILayout.Toggle(SelectToggle[i], GUILayout.MaxWidth(10));
						EditorGUILayout.LabelField(fileName, GUILayout.MinWidth(50));
					}
					else
						EditorGUILayout.LabelField("", GUILayout.MinWidth(50));

					#region 引用

					if (GUILayout.Button("引用", GUILayout.MaxWidth(40)))
					{
						var win = PrefabTools.OpenWindow();
						win.SetFindImage(objs[i] as Sprite);
					}

					#endregion

					EditorGUILayout.ObjectField(objs[i], typeof(Sprite), false, GUILayout.MinWidth(50));
					EditorGUILayout.EndHorizontal();
				}
			}

			EditorGUILayout.EndScrollView();
		}

		EditorGUILayout.EndVertical();
	}

	void SetSelectToggle(UObject[] objs, string[] dirs)
	{
		for (int i = 0; i < dirs.Length; i++)
		{
			var fileName = Path.GetFileNameWithoutExtension(dirs[i]);
			var b = GetByName(objs, fileName);
			SelectToggle.Add(b != null);
		}
	}

	UObject GetByName(UObject[] objs, string name)
	{
		for (int i = 0; i < objs.Length; i++)
		{
			if (objs[i].name == name)
				return objs[i];
		}

		return null;
	}

	bool GetByName(string[] dirs, string name)
	{
		string fileName = "";
		for (int i = 0; i < dirs.Length; i++)
		{
			fileName = Path.GetFileNameWithoutExtension(dirs[i]);
			if (fileName == name)
				return true;
		}

		return false;
	}
}