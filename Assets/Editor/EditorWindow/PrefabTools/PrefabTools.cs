using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

public partial class PrefabTools : EditorWindow
{
	public const int SPACE = 10;
	public const int BTN_WITCH = 100;
	public const int SLT_WITCH = 160;
	public const int WIN_WIDTH = 1080;
	public const int TGO_WIDTH = 14;

	private static string[] TAB =
	{
		"查找图片",
		"查找字体",
		"查找文本",
		"查找组件",
		"查找错误",
	};

	private const int tab_column = 4;

	[MenuItem("Tools/预制工具", false, 900)]
	public static PrefabTools OpenWindow()
	{
		var winWidth = Mathf.Min(tab_column * (BTN_WITCH + 3) + 3, WIN_WIDTH);
		_win = GetWindow<PrefabTools>("预制工具");
		_win.minSize = new Vector2(winWidth, 400);
		_win.maxSize = new Vector2(WIN_WIDTH, 980);
		_win.Init();
		return _win;
	}

	private static PrefabTools _win;

	public int Tab { get; set; }

	public const string PathListKey = "FindPrefabImage.PrefabPathList";
	public const string DefaultPath = "Assets/Game/UIs";
	private SerializedObject mPathSerializedObject;
	private ReorderableList mPathReorderableList;
	public List<Object> PrefabPathList = null;

	private Transform mTempParent = null;

	public Transform TempParent
	{
		get
		{
			if (mTempParent == null)
			{
				var go = GameObject.Find("GameRoot/BaseView/Root");
				if (go != null)
					mTempParent = go.transform;
			}

			return mTempParent;
		}
	}

	private FindPrefabFont mFindFont;
	private FindPrefabImage mFindImage;

	private void Init()
	{
		mFindFont = new FindPrefabFont(this);
		mFindImage = new FindPrefabImage(this);

		InitFindChar();
		InitPrefabPaths();
	}

	public void SetFindImage(Sprite sprite)
	{
		mFindImage.InitFindImage(sprite);
	}

	void OnLostFocus()
	{
		SavePrefabPaths();
	}

	public void InitPrefabPaths()
	{
		var pathStr = EditorPrefs.GetString(PathListKey, DefaultPath);
		PrefabPathList = new List<Object>();
		var paths = pathStr.Split('|');
		if (string.IsNullOrEmpty(paths[0]))
			paths[0] = DefaultPath;
		foreach (var path in paths)
		{
			var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
			PrefabPathList.Add(obj);
		}

		mPathSerializedObject = new SerializedObject(this);
		var property = mPathSerializedObject.FindProperty("PrefabPathList");
		mPathReorderableList = new ReorderableList(mPathSerializedObject, property)
		{
			drawHeaderCallback = rect => GUI.Label(rect, "搜索预制路径:"),
			elementHeight = EditorGUIUtility.singleLineHeight,
			drawElementCallback = (rect, index, selected, focused) =>
			{
				var element = property.GetArrayElementAtIndex(index);
				EditorGUI.ObjectField(rect, element, GUIContent.none);
			}
		};
	}

	public void SavePrefabPaths()
	{
		mPathSerializedObject.ApplyModifiedProperties();
		var paths = GetPrefabPaths();
		EditorPrefs.SetString(PathListKey, string.Join("|", paths));
	}

	public string[] GetPrefabPaths()
	{
		var paths = new List<string>();
		foreach (var obj in PrefabPathList)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			if (string.IsNullOrEmpty(path))
				continue;
			paths.Add(path);
		}

		return paths.ToArray();
	}

	void OnGUI()
	{
		var oldColor = GUI.color;
		for (int i = 0, len = TAB.Length; i < len; i += tab_column)
		{
			GUILayout.BeginHorizontal();
			for (int j = 0; j < tab_column; j++)
			{
				var index = i + j;
				if (index < len)
				{
					GUI.color = Tab == index ? Color.white : Color.grey;
					if (GUILayout.Button(TAB[index], GUILayout.MaxWidth(BTN_WITCH)))
						Tab = index;
				}
			}

			GUILayout.EndHorizontal();
		}

		GUI.color = oldColor;

		EditorGUIUtility.labelWidth = 64;
		mPathReorderableList?.DoLayoutList();

		switch (Tab)
		{
			case 0:
				mFindImage.OnGUI();
				break;
			case 1:
				mFindFont.OnGUI();
				break;
			case 2:
				OnGUI_FindChar();
				break;
		}
	}


	public GameObject GetSceneObject(string name)
	{
		if (TempParent != null)
		{
			var t = TempParent.Find(name);
			if (t != null)
				return t.gameObject;
		}

		return GameObject.Find(name);
	}

	public static PrefabStage GetPrefabStage(GameObject prefab, string path)
	{
		var stage = PrefabStageUtility.GetCurrentPrefabStage();
		if (stage == null || stage.prefabAssetPath != path)
		{
			AssetDatabase.OpenAsset(prefab);
			stage = PrefabStageUtility.GetCurrentPrefabStage();
		}

		return stage;
	}

	public static string GetPath(GameObject g)
	{
		Transform t = g.transform;
		string path = t.gameObject.name;
		while (t.parent != null && t.parent.parent != null)
		{
			path = t.parent.name + "/" + path;
			t = t.parent;
		}

		return path;
	}
}