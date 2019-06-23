using UnityEngine;

public class CopyX : MonoBehaviour {
	public Transform WatchedTransform = null;

	void LateUpdate() {
		var curPos = transform.position;
		curPos.x = WatchedTransform.position.x;
		transform.position = curPos;
	}
}
