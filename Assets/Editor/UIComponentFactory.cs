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

    [MenuItem("CONTEXT/Image/转换成RawImage")]
    public static void ReplaceImageToRawImage()
    {
        var image = Selection.activeGameObject.GetComponent<Image>();
        if (image != null)
        {
            Texture2D tex = null;
            if (image.sprite)
                tex = image.sprite.texture;
            Material mat = null;
            if (image.mainTexture.name != "UIDefault")
                mat = image.material;
            var ray = image.raycastTarget;
            Object.DestroyImmediate(image);
            var rawImage = Selection.activeGameObject.AddComponent<RawImage>();
            rawImage.texture = tex;
            rawImage.material = mat;
            rawImage.raycastTarget = ray;
            EditorUtility.SetDirty(Selection.activeGameObject);
        }
    }

    [MenuItem("CONTEXT/RawImage/转换成Image")]
    public static void ReplaceRawImageToImage()
    {
        var rawImage = Selection.activeGameObject.GetComponent<RawImage>();
        if (rawImage != null)
        {
            var path = AssetDatabase.GetAssetPath(rawImage.texture);
            if (string.IsNullOrEmpty(path))
                return;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogFormat("转换失败, 图片 <color=yellow>{0}</color> 不是 sprite 类型!");
                return;
            }
            Material mat = null;
            if (rawImage.mainTexture.name != "UIDefault")
                mat = rawImage.material;
            var ray = rawImage.raycastTarget;
            Object.DestroyImmediate(rawImage);
            var image = Selection.activeGameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.material = mat;
            image.raycastTarget = ray;
            EditorUtility.SetDirty(Selection.activeGameObject);
        }
    }
}
