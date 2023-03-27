using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RectTransform), true)]
public class RectTransformDecorator : DecoratorEditor
{
	public RectTransformDecorator() : base("RectTransformInspector")
	{
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		if (GUILayout.Button("取整"))
		{
			var rt = serializedObject.targetObject as RectTransform;
			var pos = rt.anchoredPosition3D;
			rt.anchoredPosition3D =
				new Vector3(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
			var size = rt.sizeDelta;
			rt.sizeDelta = new Vector2(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
		}
	}
}