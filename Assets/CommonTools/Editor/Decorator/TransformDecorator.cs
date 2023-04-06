using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Transform), true)]
public class TransformDecorator : DecoratorEditor
{
	public TransformDecorator() : base("TransformInspector") { }

	protected override void OnSceneGUI() { }

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		if (GUILayout.Button("取整"))
		{
			var trans = serializedObject.targetObject as Transform;
			trans.localPosition = MathHelper.RoundToInt(trans.localPosition);
		}
	}
}