#if UNITY_2017_1_OR_NEWER
using System.IO;
using UnityEditor;
using UnityEngine;

using SMGCore;

namespace SMGCore.Editor {
	public static class ScriptableObjectUtility {
		/// <summary>
		//	This makes it easy to create, name and place unique new ScriptableObject asset files.
		/// </summary>
		public static void CreateAsset<T>() where T : ScriptableObject {
			T asset = ScriptableObject.CreateInstance<T>();

			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if ( path == "" ) {
				path = "Assets";
			}
			else if ( Path.GetExtension(path) != "" ) {
				path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
			}

			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + ".asset");
			AssetDatabase.CreateAsset(asset, assetPathAndName);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = asset;
		}

		[MenuItem("Assets/Create/SoundLibrary")]
		public static void CreateAsset() {
			CreateAsset<SoundLibrary>();
		}
	}
}
#endif