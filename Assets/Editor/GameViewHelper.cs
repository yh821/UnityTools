using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class GameViewHelper
{
	[MenuItem("Tools/GameView/AddIPhoneXSize")]
	public static void AddIPhoneXSize()
	{
		var index = FindSize(39, 18, out var count);
		if (index >= 0)
		{
			SetSize(index);
		}
		else
		{
			AddCustomSize(GameViewSizeType.AspectRatio, 39, 18, "iPhoneX");
			SetSize(count);
		}
	}
	

    [MenuItem("Tools/GameView/AddDefaultSize")]
    public static void AddDefaultSize()
    {
        var index = FindSize(1334, 768, out var count);
        if (index >= 0)
        {
            SetSize(index);
        }
        else
        {
            AddCustomSize(GameViewSizeType.FixedResolution, 1334, 768, "Default");
            SetSize(count);
        }
    }

    public enum GameViewSizeType
    {
        AspectRatio,
        FixedResolution
    }

    static object gameViewSizesInstance;
    static MethodInfo getGroup;

    static GameViewHelper()
    {
        // gameViewSizesInstance  = ScriptableSingleton<GameViewSizes>.instance;
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instanceProp = singleType.GetProperty("instance");
        getGroup = sizesType.GetMethod("GetGroup");
        gameViewSizesInstance = instanceProp.GetValue(null, null);
    }

    public static void SetSize(int index)
    {
        var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var gvWnd = EditorWindow.GetWindow(gvWndType);
#if UNITY_5_4_OR_NEWER
        var sizeSelectionCallback = gvWndType.GetMethod("SizeSelectionCallback",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        sizeSelectionCallback.Invoke(gvWnd, new object[] { index, null });
#else
        var selectedSizeIndexProp = gvWndType.GetProperty("selectedSizeIndex",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        selectedSizeIndexProp.SetValue(gvWnd, index, null);
#endif
    }

    public static void AddCustomSize(GameViewSizeType viewSizeType, int width, int height, string text)
    {
        var group = GetGroup(GetCurrentGroupType());
        var addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize"); // or group.GetType().
        var gameViewSize = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
#if NET_4_6
        var gameViewSizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
        var ctor = gameViewSize.GetConstructor(new Type[] { gameViewSizeType, typeof(int), typeof(int), typeof(string)});
#else
        var ctor = gameViewSize.GetConstructor(new Type[] {typeof(int), typeof(int), typeof(int), typeof(string)});
#endif
        var newSize = ctor.Invoke(new object[] {(int)viewSizeType, width, height, text});
        addCustomSize.Invoke(group, new object[] {newSize});
    }

    public static int FindSize(string text)
    {
        // GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
        // string[] texts = group.GetDisplayTexts();
        // for loop...

        var group = GetGroup(GetCurrentGroupType());
        var getDisplayTexts = group.GetType().GetMethod("GetDisplayTexts");
        var displayTexts = getDisplayTexts.Invoke(group, null) as string[];
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
                return i;
        }

        return -1;
    }

    public static int FindSize(int width, int height, out int count)
    {
        // goal:
        // GameViewSizes group = gameViewSizesInstance.GetGroup(sizeGroupType);
        // int sizesCount = group.GetBuiltinCount() + group.GetCustomCount();
        // iterate through the sizes via group.GetGameViewSize(int index)

        var group = GetGroup(GetCurrentGroupType());
        var groupType = group.GetType();
        var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
        var getCustomCount = groupType.GetMethod("GetCustomCount");
        count = (int) getBuiltinCount.Invoke(group, null) + (int) getCustomCount.Invoke(group, null);
        var getGameViewSize = groupType.GetMethod("GetGameViewSize");
        var gvsType = getGameViewSize.ReturnType;
        var widthProp = gvsType.GetProperty("width");
        var heightProp = gvsType.GetProperty("height");
        var indexValue = new object[1];
        for (int i = 0; i < count; i++)
        {
            indexValue[0] = i;
            var size = getGameViewSize.Invoke(group, indexValue);
            int sizeWidth = (int) widthProp.GetValue(size, null);
            int sizeHeight = (int) heightProp.GetValue(size, null);
            if (sizeWidth == width && sizeHeight == height)
                return i;
        }

        return -1;
    }

    private static object GetGroup(GameViewSizeGroupType type)
    {
        return getGroup.Invoke(gameViewSizesInstance, new object[] {(int) type});
    }

    public static GameViewSizeGroupType GetCurrentGroupType()
    {
        var getCurrentGroupTypeProp = gameViewSizesInstance.GetType().GetProperty("currentGroupType");
        return (GameViewSizeGroupType) (int) getCurrentGroupTypeProp.GetValue(gameViewSizesInstance, null);
    }
}