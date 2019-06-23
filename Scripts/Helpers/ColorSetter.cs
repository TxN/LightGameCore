using UnityEngine;

public class ColorSetter {
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
