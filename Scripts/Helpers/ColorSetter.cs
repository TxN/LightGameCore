#if UNITY_2017_1_OR_NEWER
using UnityEngine;

namespace SMGCore {
	public sealed class ColorSetter {
		public static void UpdateModelColor(GameObject target, Color color) {
			var meshes = target.GetComponentsInChildren<MeshRenderer>(true);
			foreach (var mesh in meshes) {
				if ( mesh.gameObject.name.Contains("colorSkip") ) {
					continue;
				}
				var mats  = mesh.materials;
				var mat   = mats[0];
				mat.color = color;
				mats[0]   = mat;
				mesh.materials = mats;
			}
		}
	}
}
#endif