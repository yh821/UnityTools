using UnityEngine;
using UnityEditor;

//定制Serializable类的每个实例的GUI
[CustomPropertyDrawer(typeof(Character))]
public class CharacterDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		//创建一个属性包装器，用于将常规GUI控件与SerializedProperty一起使用
		using (new EditorGUI.PropertyScope(position, label, property))
		{
			//设置属性名宽度 Name HP
			EditorGUIUtility.labelWidth = 60;
			//输入框高度，默认一行的高度
			position.height = EditorGUIUtility.singleLineHeight;

			//ico 位置矩形
			Rect iconRect = new Rect(position)
			{
				width = 68,
				height = 68
			};

			Rect nameRect = new Rect(position)
			{
				width = position.width - 70, //减去icon的width 64
				x = position.x + 70 //在icon的基础上右移64
			};

			Rect hpRect = new Rect(nameRect)
			{
				//在name的基础上，y坐标下移
				y = nameRect.y + EditorGUIUtility.singleLineHeight + 2
			};

			Rect powerRect = new Rect(hpRect)
			{
				//在hp的基础上，y坐标下移
				y = hpRect.y + EditorGUIUtility.singleLineHeight + 2
			};

			Rect weaponLabelRect = new Rect(powerRect)
			{
				y = powerRect.y + EditorGUIUtility.singleLineHeight + 2,
				width = 60
			};

			Rect weaponRect = new Rect(weaponLabelRect)
			{
				x = weaponLabelRect.x + 60,
				width = powerRect.width - 60
			};

			//找到每个属性的序列化值
			SerializedProperty iconProperty = property.FindPropertyRelative("icon");
			SerializedProperty nameProperty = property.FindPropertyRelative("name");
			SerializedProperty hpProperty = property.FindPropertyRelative("hp");
			SerializedProperty powerProperty = property.FindPropertyRelative("power");
			SerializedProperty weaponProperty = property.FindPropertyRelative("weapon");

			//绘制icon
			iconProperty.objectReferenceValue =
				EditorGUI.ObjectField(iconRect, iconProperty.objectReferenceValue, typeof(Texture), false);

			//绘制name
			nameProperty.stringValue =
				EditorGUI.TextField(nameRect, nameProperty.displayName, nameProperty.stringValue);

			//Slider，范围在0-100
			EditorGUI.IntSlider(hpRect, hpProperty, 0, 100);
			//Slider，范围在0-10
			EditorGUI.IntSlider(powerRect, powerProperty, 0, 10);

			EditorGUI.PrefixLabel(weaponLabelRect, new GUIContent("weapon"));
			EditorGUI.PropertyField(weaponRect, weaponProperty, GUIContent.none);
		}
	}
}