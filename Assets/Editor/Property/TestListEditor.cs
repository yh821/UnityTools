using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(TestList))]
public class TestListEditor : Editor
{
	private ReorderableList m_colors;

	private void OnEnable()
	{
		m_colors = new ReorderableList(serializedObject, serializedObject.FindProperty("m_colors"), true, true, true,
			true);
		//绘制元素
		m_colors.drawElementCallback = (Rect rect, int index, bool selected, bool focused) =>
		{
			SerializedProperty itemData = m_colors.serializedProperty.GetArrayElementAtIndex(index);
			rect.y += 2;
			rect.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.PropertyField(rect, itemData, GUIContent.none);
		};
		//绘制表头
		m_colors.drawHeaderCallback = (Rect rect) => { GUI.Label(rect, "Colors"); };
		//当移除元素时回调
		m_colors.onRemoveCallback = (ReorderableList list) =>
		{
			//弹出一个对话框
			if (EditorUtility.DisplayDialog("警告", "是否确定删除该颜色", "是", "否"))
			{
				//当点击“是”
				ReorderableList.defaultBehaviours.DoRemoveButton(list);
			}
		};
		//添加按钮回调
		m_colors.onAddCallback = (ReorderableList list) =>
		{
			if (list.serializedProperty != null)
			{
				list.serializedProperty.arraySize++;
				list.index = list.serializedProperty.arraySize - 1;
				SerializedProperty itemData = list.serializedProperty.GetArrayElementAtIndex(list.index);
				itemData.colorValue = Color.red;
			}
			else
			{
				ReorderableList.defaultBehaviours.DoAddButton(list);
			}
		};
		//鼠标抬起回调
		m_colors.onMouseUpCallback = (ReorderableList list) => { Debug.Log("MouseUP"); };
		//当选择元素回调
		m_colors.onSelectCallback = (ReorderableList list) =>
		{
			//打印选中元素的索引
			Debug.Log(list.index);
		};
	}

	public override void OnInspectorGUI()
	{
		EditorGUILayout.Space();
		serializedObject.Update();
		m_colors.DoLayoutList();
		serializedObject.ApplyModifiedProperties();
	}
}