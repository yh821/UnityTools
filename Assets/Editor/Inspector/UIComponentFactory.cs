using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public class UIComponentFactory
{
	static UIComponentFactory()
	{
		ObjectFactory.componentWasAdded += OnComponentWasAdded;
	}

	public static void OnComponentWasAdded(Component com)
	{
		switch (com)
		{
			case Button button:
			{
				button.transition = Selectable.Transition.None;
				var image = button.GetComponent<Image>();
				if (image)
					image.raycastTarget = true;
				break;
			}

			case Image image:
			{
				image.raycastTarget = false;
				break;
			}

			case Text text:
			{
				text.raycastTarget = false;
				text.supportRichText = false;
				text.fontSize = 20;
				//text.font = AssetDatabase.LoadAssetAtPath<Font>("");
				//EditorUtility.SetDirty(text);
				break;
			}

			case RawImage rawImage:
			{
				rawImage.raycastTarget = false;
				break;
			}
		}
	}

	[MenuItem("CONTEXT/Image/ת����RawImage")]
	public static void ReplaceImageToRawImage()
	{
		var image = Selection.activeGameObject.GetComponent<Image>();
		if (image != null)
		{
			Texture2D tex = null;
			if (image.sprite)
				tex = image.sprite.texture;
			//Material mat = null;
			//if (image.mainTexture.name != "Default")
			//    mat = image.material;
			var ray = image.raycastTarget;
			Object.DestroyImmediate(image);
			var rawImage = Selection.activeGameObject.AddComponent<RawImage>();
			rawImage.texture = tex;
			//rawImage.material = mat;
			rawImage.raycastTarget = ray;
			EditorUtility.SetDirty(Selection.activeGameObject);
		}
	}

	[MenuItem("CONTEXT/RawImage/ת����Image")]
	public static void ReplaceRawImageToImage()
	{
		var rawImage = Selection.activeGameObject.GetComponent<RawImage>();
		if (rawImage != null)
		{
			Sprite sprite = null;
			if (rawImage.texture != null)
			{
				var path = AssetDatabase.GetAssetPath(rawImage.texture);
				sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
				if (sprite == null)
				{
					Debug.LogFormat("ת��ʧ��, ͼƬ <color=yellow>{0}</color> ���� sprite ����!", rawImage.texture.name);
					return;
				}
			}

			//Material mat = null;
			//if (rawImage.mainTexture.name != "Default")
			//    mat = rawImage.material;
			var ray = rawImage.raycastTarget;
			Object.DestroyImmediate(rawImage);
			var image = Selection.activeGameObject.AddComponent<Image>();
			image.sprite = sprite;
			//image.material = mat;
			image.raycastTarget = ray;
			EditorUtility.SetDirty(Selection.activeGameObject);
		}
	}
}