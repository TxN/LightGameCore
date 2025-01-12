#if UNITY_2017_1_OR_NEWER
using UnityEngine;

namespace SMGCore {
	public sealed class CopyX : MonoBehaviour {
		public bool      WatchMainCamera  = false;
		public Transform WatchedTransform = null;

		private void Start() {
			if ( WatchMainCamera ) {
				WatchedTransform = Camera.main.transform;
			}
		}

		void LateUpdate() {
			if ( !WatchedTransform ) {
				return;
			}
			var curPos = transform.position;
			curPos.x   = WatchedTransform.position.x;
			transform.position = curPos;
		}
	}
}
#endif