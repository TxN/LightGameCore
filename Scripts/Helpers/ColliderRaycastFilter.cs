#if UNITY_2017_1_OR_NEWER
using UnityEngine;

namespace SMGCore {
	[RequireComponent(typeof(Collider2D))]
	public sealed class ColliderRaycastFilter : MonoBehaviour, ICanvasRaycastFilter {
		public int OverrideDepth = 0;
		Collider2D _collider;
		void Start() {
			_collider = GetComponent<Collider2D>();
		}

		public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera) {
			return _collider && _collider.OverlapPoint(eventCamera.ScreenToWorldPoint(sp));
		}
	}
}
#endif