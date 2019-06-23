using UnityEngine;

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

