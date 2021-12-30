using UnityEngine;
using UnityEditor;
using System;

class GUIIconViewer : EditorWindow
{
	static string[] iconList;
	[MenuItem("Tools/内置Icon", false, 10)]
	public static void ShowWindow()
	{
		GetWindow(typeof(GUIIconViewer));
		var file = Resources.Load<TextAsset>("IconName");
        iconList = file.text.Split('\n');
	}

	Vector2 scrollPosition;
	private bool showMouseCursor = false;
	private int BtnSize = 64;

	void OnGUI()
	{
		scrollPosition = GUILayout.BeginScrollView(scrollPosition);
		{
            //鼠标放在按钮上的样式
            showMouseCursor = EditorGUILayout.Foldout(showMouseCursor, "鼠标样式", true);
            if (showMouseCursor)
            {
	            foreach (MouseCursor item in Enum.GetValues(typeof(MouseCursor)))
	            {
		            GUILayout.Button(Enum.GetName(typeof(MouseCursor), item), GUILayout.Height(30));
		            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), item);
	            }
            }

            //内置图标
            for (int i = 0; i < iconList.Length; i += 10)
			{
				GUILayout.BeginHorizontal();
				for (int j = 0; j < 10; j++)
				{
					int index = i + j;
					if (index < iconList.Length)
					{
						var icon = iconList[index].Trim();
						if (icon.StartsWith("#"))
							continue;
                        if(GUILayout.Button(EditorGUIUtility.IconContent(icon), 
							GUILayout.Width(BtnSize), GUILayout.Height(BtnSize)))
							Debug.Log(icon);
					}
				}
				GUILayout.EndHorizontal();
			}
		}
		GUILayout.EndScrollView();
	}
}