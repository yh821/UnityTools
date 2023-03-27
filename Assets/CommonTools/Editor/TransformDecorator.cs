using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Transform), true)]
public class TransformDecorator : DecoratorEditor
{
	public TransformDecorator() : base("TransformInspector")
	{
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		if (GUILayout.Button("取整"))
		{
			var trans = serializedObject.targetObject as Transform;
			var pos = trans.localPosition;
			trans.localPosition =
				new Vector3(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
		}
	}
}