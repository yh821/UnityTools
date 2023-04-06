using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RectTransform), true)]
public class RectTransformDecorator : DecoratorEditor
{
	public RectTransformDecorator() : base("RectTransformInspector") { }

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		if (GUILayout.Button("位置取整"))
		{
			var rt = serializedObject.targetObject as RectTransform;
			rt.anchoredPosition = MathHelper.RoundToInt(rt.anchoredPosition, true);
			rt.sizeDelta = MathHelper.RoundToInt(rt.sizeDelta);
		}
	}
}