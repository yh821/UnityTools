using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GravitySensor : MonoBehaviour
{
	[Serializable]
	public class TransData
	{
		public RectTransform rect;
		public Vector3 offset;
		[HideInInspector]
		public Vector3 origin;
	}

	[SerializeField]
	public List<TransData> transNodes;

	[SerializeField]
	public List<TransData> rotateNodes;

	private Vector3 mousePos;
	private Vector3 centerPos;
	private Vector3 direction;

	private float halfHeight;
	private float halfWidth;

    // Start is called before the first frame update
    void Start()
    {
        halfWidth = Screen.width / 2f;
        halfHeight = Screen.height / 2f;
        centerPos = new Vector3(halfWidth, halfHeight);

        foreach(var data in transNodes)
        {
        	data.origin = data.rect.localPosition;
        }

        foreach(var data in rotateNodes)
        {
        	data.origin = data.rect.localEulerAngles;
        }
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        mousePos = Input.mousePosition;
        direction = mousePos - centerPos;
        var intensity = Mathf.Clamp01(direction.magnitude / halfWidth);
        foreach(var node in transNodes)
        {
        	node.rect.localPosition = node.origin + Offset(direction.normalized, node.offset) * intensity;
        }

        foreach(var node in rotateNodes)
        {
        	intensity = (mousePos.x - node.rect.anchoredPosition.x) / Screen.width;
        	node.rect.localEulerAngles = node.origin + node.offset * intensity;
        }
#elif UNITY_IOS || UNITY_ANDROID
        mousePos = new Vector3(Input.acceleration.x, Input.acceleration.y);
        direction = mousePos.normalized;
        foreach(var node in transNodes)
        {
        	node.rect.localPosition = node.origin + Offset(direction, node.offset) * mousePos.magnitude;
        }

        foreach(var node in rotateNodes)
        {
        	var intensity = (mousePos.x * halfWidth - node.rect.anchoredPosition.x) / Screen.width;
        	node.rect.localEulerAngles = node.origin + node.offset * intensity;
        }
#endif
    }

    Vector3 Offset(Vector3 a, Vector3 b)
    {
    	return new Vector3(a.x*b.x, a.y*b.y, a.z*b.z);
    }

#if UNITY_EDITOR
    private Rect labelRect = new Rect(100,10,Screen.width-110,Screen.height-20);
    void OnGUI()
    {
    	GUI.Label(labelRect, $"{mousePos}");
    }
#endif
}
