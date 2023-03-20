using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class GameViewHelper
{
	[MenuItem("Tools/GameView/AddIPhoneXSize")]
	public static void AddIPhoneXSize()
	{
		SetAspectRatio(39, 18, "iPhoneX");
	}

	[MenuItem("Tools/GameView/AddDefaultSize")]
	public static void AddDefaultSize()
	{
		SetFixedResolution(1334, 768, "Default");
	}


	public enum GameViewSizeType
	{
		AspectRatio,
		FixedResolution
	}

	private static readonly object GameViewSizesInstance;
	private static readonly MethodInfo _GetGroup;

	static GameViewHelper()
	{
		// gameViewSizesInstance  = ScriptableSingleton<GameViewSizes>.instance;
		var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
		var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
		var instanceProp = singleType.GetProperty("instance");
		_GetGroup = sizesType.GetMethod("GetGroup");
		GameViewSizesInstance = instanceProp.GetValue(null, null);
	}

	public static void SetFixedResolution(int width, int height, string name)
	{
		SetOrAddSize(GameViewSizeType.FixedResolution, width, height, name);
	}

	public static void SetAspectRatio(int width, int height, string name)
	{
		SetOrAddSize(GameViewSizeType.AspectRatio, width, height, name);
	}

	public static void SetOrAddSize(GameViewSizeType type, int width, int height, string name)
	{
		if (!FindSize(width, height, out var index))
			AddCustomSize(type, width, height, name);
		SetSize(index);
	}

	private static void SetSize(int index)
	{
		var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
		var gvWnd = EditorWindow.GetWindow(gvWndType);
#if UNITY_5_4_OR_NEWER
		var sizeSelectionCallback = gvWndType.GetMethod("SizeSelectionCallback",
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		sizeSelectionCallback.Invoke(gvWnd, new object[] {index, null});
#else
        var selectedSizeIndexProp = gvWndType.GetProperty("selectedSizeIndex",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        selectedSizeIndexProp.SetValue(gvWnd, index, null);
#endif
	}

	private static void AddCustomSize(GameViewSizeType viewSizeType, int width, int height, string text)
	{
		var group = GetGroup(GetCurrentGroupType());
		var addCustomSize = _GetGroup.ReturnType.GetMethod("AddCustomSize"); // or group.GetType().
		var gameViewSize = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
#if NET_4_6
		var gameViewSizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
		var ctor = gameViewSize.GetConstructor(new Type[] {gameViewSizeType, typeof(int), typeof(int), typeof(string)});
#else
        var ctor = gameViewSize.GetConstructor(new Type[] {typeof(int), typeof(int), typeof(int), typeof(string)});
#endif
		var newSize = ctor.Invoke(new object[] {(int) viewSizeType, width, height, text});
		addCustomSize.Invoke(group, new object[] {newSize});
	}

	private static bool FindSize(string text, out int index)
	{
		// GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
		// string[] texts = group.GetDisplayTexts();
		// for loop...

		var group = GetGroup(GetCurrentGroupType());
		var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
		var displayTexts = getDisplayTexts.Invoke(group, null) as string[];
		index = displayTexts.Length;
		for (int i = 0; i < displayTexts.Length; i++)
		{
			string display = displayTexts[i];
			// the text we get is "Name (W:H)" if the size has a name, or just "W:H" e.g. 16:9
			// so if we're querying a custom size text we substring to only get the name
			// You could see the outputs by just logging
			// Debug.Log(display);
			int pren = display.IndexOf('(');
			if (pren != -1)
				display = display.Substring(0,
					pren - 1); // -1 to remove the space that's before the prens. This is very implementation-depdenent
			if (display == text)
			{
				index = i;
				return true;
			}
		}

		return false;
	}

	private static bool FindSize(int width, int height, out int index)
	{
		// goal:
		// GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
		// int sizesCount = group.GetBuiltinCount() + group.GetCustomCount();
		// iterate through the sizes via group.GetGameViewSize(int index)

		var group = GetGroup(GetCurrentGroupType());
		var groupType = group.GetType();
		var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
		var getCustomCount = groupType.GetMethod("GetCustomCount");
		index = (int) getBuiltinCount.Invoke(group, null) + (int) getCustomCount.Invoke(group, null);
		var getGameViewSize = groupType.GetMethod("GetGameViewSize");
		var gvsType = getGameViewSize.ReturnType;
		var widthProp = gvsType.GetProperty("width");
		var heightProp = gvsType.GetProperty("height");
		var indexValue = new object[1];
		for (int i = 0, len = index; i < len; i++)
		{
			indexValue[0] = i;
			var size = getGameViewSize.Invoke(group, indexValue);
			int sizeWidth = (int) widthProp.GetValue(size, null);
			int sizeHeight = (int) heightProp.GetValue(size, null);
			if (sizeWidth == width && sizeHeight == height)
			{
				index = i;
				return true;
			}
		}

		return false;
	}

	private static object GetGroup(GameViewSizeGroupType type)
	{
		return _GetGroup.Invoke(GameViewSizesInstance, new object[] {(int) type});
	}

	private static GameViewSizeGroupType GetCurrentGroupType()
	{
		var getCurrentGroupTypeProp = GameViewSizesInstance.GetType().GetProperty("currentGroupType");
		return (GameViewSizeGroupType) (int) getCurrentGroupTypeProp.GetValue(GameViewSizesInstance, null);
	}
}