using UnityEngine;
using UnityEditor;

public partial class BuildInWindow : EditorWindow
{
	private static string[] TAB = {"系统GUI", "系统ICON"};

	private GUIStyle[] BtnStateStyle =
	{
		//new GUIStyle("TE toolbarbutton"),
		//new GUIStyle("StatusBarIcon"),

		new GUIStyle("ProgressBarBack"),
		new GUIStyle("ProgressBarBar"),
	};

	private int mTab = 0;

	private static BuildInWindow window;

	[MenuItem("Tools/系统内置", false, 10)]
	private static void OpenStyleViewer()
	{
		window = GetWindow<BuildInWindow>(false, "系统内置");
		window.Init();
	}

	private void Init()
	{
		InitStyle();
	}

	void OnGUI()
	{
		GUILayout.BeginHorizontal();
		for (int i = 0, len = TAB.Length; i < len; ++i)
		{
			if (GUILayout.Button(TAB[i], mTab == i ? BtnStateStyle[1] : BtnStateStyle[0], GUILayout.MaxWidth(100)))
				mTab = i;
		}

		GUILayout.EndHorizontal();

		GUILayout.Space(5);

		switch (mTab)
		{
			case 0:
				OnGuiStyle();
				break;
			case 1:
				OnGuiIcon();
				break;
		}
	}
}