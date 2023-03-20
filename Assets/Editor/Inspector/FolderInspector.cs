using UnityEngine;
using UnityEditor;
using UObject = UnityEngine.Object;

[CustomEditor(typeof(DefaultAsset))]
public class FolderInspector : Editor
{
	private string mGuid;
	private string mLastGuid;
	private UObject mObj;
	private UObject mLastObj;
	private string mPath;
	private string mLastPath;

	public override void OnInspectorGUI()
	{
		var defaultLabelWidth = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = 64;
		GUI.enabled = true;
		{
			if (mObj == null)
				mObj = target;

			if (mGuid != mLastGuid)
			{
				mLastGuid = mGuid;
				mPath = AssetDatabase.GUIDToAssetPath(mGuid);
				mObj = AssetDatabase.LoadAssetAtPath<UObject>(mPath);
				mLastObj = mObj;
				mLastPath = mPath;
			}

			if (mObj != mLastObj)
			{
				mLastObj = mObj;
				mPath = AssetDatabase.GetAssetPath(mObj);
				mGuid = AssetDatabase.AssetPathToGUID(mPath);
				mLastGuid = mGuid;
				mLastPath = mPath;
			}

			if (mPath != mLastPath)
			{
				mLastPath = mPath;
				mObj = AssetDatabase.LoadAssetAtPath<UObject>(mPath);
				mGuid = AssetDatabase.AssetPathToGUID(mPath);
				mLastObj = mObj;
				mLastGuid = mGuid;
			}

			mGuid = EditorGUILayout.TextField("GUID:", mGuid);
			mPath = EditorGUILayout.TextField("Path:", mPath);
			mObj = EditorGUILayout.ObjectField("Asset:", mObj, typeof(UObject), false);
		}
		EditorGUIUtility.labelWidth = defaultLabelWidth;
		GUI.enabled = false;
		base.OnInspectorGUI();
	}
}