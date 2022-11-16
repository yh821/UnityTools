using UnityEditor;
using UnityEngine;

public partial class PrefabTools : EditorWindow
{
	public const int SPACE = 10;
	public const int BTN_WITCH = 100;
	public const int SLT_WITCH = 160;
	public const int WIN_WIDTH = 1080;

	private static string[] TAB =
	{
		"查找图片",
		"查找字体",
        "查找文本",
	};

	[MenuItem("Tools/预制工具", false, 900)]
	public static PrefabTools OpenWindow()
	{
		var winWidth = Mathf.Min(TAB.Length * (BTN_WITCH + 3) + 3, WIN_WIDTH);
		_win = GetWindow<PrefabTools>();
		_win.minSize = new Vector2(winWidth, 400);
		_win.maxSize = new Vector2(WIN_WIDTH, 980);
		_win.Init();
		return _win;
	}
	private static PrefabTools _win;

	public int Tab { get; set; }

	private void Init()
    {
        InitFindImage();
        InitFindChar();
        InitPathGui();
    }

	void OnLostFocus()
	{
		SaveCommitPath();
	}

    void OnGUI()
	{
		var oldColor = GUI.color;
		GUILayout.BeginHorizontal();
		{
			for (int i = 0; i < TAB.Length; i++)
			{
				GUI.color = Tab == i ? Color.white : Color.grey;
				if (GUILayout.Button(TAB[i], GUILayout.MaxWidth(BTN_WITCH)))
					Tab = i;
			}
		}
		GUI.color = oldColor;
		GUILayout.EndHorizontal();

		switch (Tab)
		{
            case 0:
				OnGUI_FindImage();
				break;
            case 1:
                OnGUI_FindFont();
                break;
            case 2:
				OnGUI_FindChar();
	            break;
		}
	}

}
