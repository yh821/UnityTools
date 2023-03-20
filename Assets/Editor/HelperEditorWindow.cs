using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Common;

public class HelperEditorWindow : EditorWindow
{
	[MenuItem("Tools/工具窗口")]
	public static void AddWindow()
	{
		var win = GetWindow<HelperEditorWindow>("工具窗口");
		win.Init();
	}

	private string mFilePath = "";
	private string mSrcDir = "";
	private string mDstDir = "";

	private const int SPACE_SIZE = 6;
	private Vector2 mScroller = Vector2.zero;

	private void Init()
	{
		mFilePath = Path.Combine(Application.dataPath, "auto_build_time.txt");
	}

	void OnGUI()
	{
		EditorGUIUtility.labelWidth = 64;
		mScroller = EditorGUILayout.BeginScrollView(mScroller);
		{
			GUILayout.Space(SPACE_SIZE);
			EditorGUILayout.BeginVertical("Box");
			{
				EditorGUILayout.HelpBox("操作文件", MessageType.Info);
				EditorGUILayout.BeginHorizontal();
				{
					mFilePath = EditorGUILayout.TextField("文件路径:", mFilePath);
					if (GUILayout.Button("OpenFilePanel", GUILayout.MaxWidth(100)))
					{
						mFilePath = EditorUtility.OpenFilePanel("选择文件", mFilePath, "*.*");
					}

					if (GUILayout.Button("IOHelper.SaveFile", GUILayout.MaxWidth(120)))
					{
						IOHelper.SaveFile(mFilePath, $"SVN:{DateTime.Now:yyyy-MM-dd HH:mm:ss}, Cost:0 min");
					}
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				{
					if (GUILayout.Button("IOHelper.SelectFile"))
					{
						IOHelper.SelectFile(mFilePath);
					}

					if (GUILayout.Button("IOHelper.OpenFile"))
					{
						IOHelper.OpenFile(mFilePath);
					}

					if (GUILayout.Button("IOHelper.DeleteFile"))
					{
						IOHelper.DeleteFile(mFilePath);
					}
				}
				EditorGUILayout.EndHorizontal();

				#region Test

				//EditorGUILayout.BeginHorizontal();
				//{
				//	if (GUILayout.Button("SortText"))
				//	{
				//		if (string.IsNullOrEmpty(mFilePath))
				//			ShowNotification(new GUIContent("请选择文件"));
				//		else
				//		{
				//			var lines = new List<string>(IOHelper.ReadAllLines(mFilePath));
				//			lines.Sort(string.Compare);
				//			IOHelper.SaveFile(mFilePath, lines.ToArray());
				//		}
				//	}

				//	if (GUILayout.Button("DelSame"))
				//	{
				//		if (string.IsNullOrEmpty(mFilePath))
				//			ShowNotification(new GUIContent("请选择文件"));
				//		else
				//		{
				//			var lines = IOHelper.ReadAllLines(mFilePath);
				//			var dict = new Dictionary<string, int>();
				//			foreach (var line in lines)
				//			{
				//				if (dict.ContainsKey(line))
				//					dict[line] += 1;
				//				else
				//					dict.Add(line, 1);
				//			}

				//			IOHelper.SaveFile(mFilePath, dict.Keys.ToArray());
				//			var withoutExtPath = mFilePath.Substring(0, mFilePath.LastIndexOf('.'));
				//			var sb = new StringBuilder();
				//			foreach (var kv in dict)
				//			{
				//				sb.Append(kv.Value).Append("\t").Append(kv.Key).Append('\n');
				//			}

				//			IOHelper.SaveFile(withoutExtPath + "_c.txt", sb.ToString());
				//		}
				//	}

				//	if (GUILayout.Button("Del'_d'"))
				//	{
				//		if (string.IsNullOrEmpty(mFilePath))
				//			ShowNotification(new GUIContent("请选择文件"));
				//		else
				//		{
				//			var lines = IOHelper.ReadAllLines(mFilePath);
				//			var swd = new List<string>();
				//			var nwd = new List<string>();
				//			foreach (var line in lines)
				//			{
				//				if (line.StartsWith("d_"))
				//					swd.Add(line);
				//				else
				//					nwd.Add(line);
				//			}

				//			var array = new List<string>(nwd);
				//			foreach (var sd in swd)
				//			{
				//				var line = sd.Substring(2);
				//				if (!nwd.Contains(line))
				//					array.Add(sd);
				//			}

				//			IOHelper.SaveFile(mFilePath, array.ToArray());
				//		}
				//	}
				//}
				//EditorGUILayout.EndHorizontal();

				#endregion
			}
			EditorGUILayout.EndVertical();

			GUILayout.Space(SPACE_SIZE);
			EditorGUILayout.BeginVertical("Box");
			{
				EditorGUILayout.HelpBox("拷贝文件夹", MessageType.Info);
				EditorGUILayout.BeginHorizontal();
				{
					mSrcDir = EditorGUILayout.TextField("From:", mSrcDir);
					if (GUILayout.Button("OpenFolderPanel", GUILayout.MaxWidth(160)))
					{
						mSrcDir = EditorUtility.OpenFolderPanel("From", mSrcDir, "");
					}

					if (GUILayout.Button("IOHelper.OpenFolder", GUILayout.MaxWidth(160)))
					{
						IOHelper.OpenFolder(mSrcDir);
					}
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				{
					mDstDir = EditorGUILayout.TextField("To:", mDstDir);
					if (GUILayout.Button("OpenFolderPanel", GUILayout.MaxWidth(160)))
					{
						mDstDir = EditorUtility.OpenFolderPanel("Target", mDstDir, "");
					}

					if (GUILayout.Button("IOHelper.OpenFolder", GUILayout.MaxWidth(160)))
					{
						IOHelper.OpenFolder(mDstDir);
					}
				}
				EditorGUILayout.EndHorizontal();
				if (GUILayout.Button("开始拷贝文件夹"))
				{
					IOHelper.CopyFolder(mSrcDir, mDstDir);
				}
			}
			EditorGUILayout.EndVertical();

			GUILayout.Space(SPACE_SIZE);
			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("提示弹窗"))
				{
					if (EditorUtility.DisplayDialog("title", "message", "ok"))
						Debug.Log("click ok");
				}

				if (GUILayout.Button("提示弹窗"))
				{
					if (EditorUtility.DisplayDialog("title", "message", "ok", "cancel"))
						Debug.Log("click ok");
				}

				if (GUILayout.Button("选择弹窗"))
				{
					var id = EditorUtility.DisplayDialogComplex("title", "message", "id=0", "id=1", "id=2");
					switch (id)
					{
						case 0:
							Debug.Log("click ok");
							break;
						case 1:
							Debug.Log("click cancel");
							break;
						case 2:
							Debug.Log("click alt");
							break;
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}
		EditorGUILayout.EndScrollView();
	}
}