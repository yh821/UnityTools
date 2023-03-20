using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public partial class BuildInWindow
{
	// [MenuItem("Tools/内置Icon", false, 10)]
	// public static void ShowWindow()
	// {
	// 	var win = GetWindow<GUIIconViewer>();
	// 	win.Init();
	// }

	private const string IconFile = "Assets/Editor/Build-In/IconName.txt";

	private string[] allIcons;
	private List<string> searchIcons;
	private string searchKey = "";
	private string lastSearchKey;
	private string selectIcon = "";

	private bool _showMouseCursor;
	private readonly int _btnSize = 64;
	private Vector2 _scrollPosition;

	private GUIContent searchIcon;

	public void InitStyle()
	{
		var file = AssetDatabase.LoadAssetAtPath<TextAsset>(IconFile);
		allIcons = file.text.Split('\n');
		searchIcons = new List<string>();
		searchIcon = EditorGUIUtility.IconContent("Search Icon");
		searchIcon.text = "搜索";
		searchIcon.tooltip = "搜索图标";
	}

	private void OnGuiIcon()
	{
		GUILayout.Space(6);
		EditorGUIUtility.labelWidth = 64;

		#region 搜索

		searchKey = EditorGUILayout.DelayedTextField(searchIcon, searchKey);
		if (lastSearchKey != searchKey)
		{
			searchKey = searchKey.ToLower();
			lastSearchKey = searchKey;
			searchIcons.Clear();
			if (string.IsNullOrEmpty(searchKey))
			{
				foreach (var icon in allIcons)
				{
					searchIcons.Add(icon);
				}
			}
			else
			{
				foreach (var icon in allIcons)
				{
					if (icon.ToLower().IndexOf(searchKey) != -1)
						searchIcons.Add(icon);
				}
			}
		}

		#endregion

		EditorGUILayout.SelectableLabel(selectIcon);

		_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
		{
			//内置图标
			for (int i = 0; i < searchIcons.Count; i += 10)
			{
				GUILayout.BeginHorizontal();
				for (int j = 0; j < 10; j++)
				{
					int index = i + j;
					if (index < searchIcons.Count)
					{
						var icon = searchIcons[index].Trim();
						if (string.IsNullOrEmpty(icon) || icon.StartsWith("#"))
							continue;
						if (GUILayout.Button(EditorGUIUtility.IconContent(icon),
							GUILayout.Width(_btnSize), GUILayout.Height(_btnSize)))
							selectIcon = icon;
					}
				}

				GUILayout.EndHorizontal();
			}

			//鼠标样式
			_showMouseCursor = EditorGUILayout.Foldout(_showMouseCursor, "鼠标样式", true);
			if (_showMouseCursor)
			{
				foreach (MouseCursor item in Enum.GetValues(typeof(MouseCursor)))
				{
					GUILayout.Button(Enum.GetName(typeof(MouseCursor), item), GUILayout.Height(30));
					EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), item);
				}
			}
		}
		GUILayout.EndScrollView();
	}
}