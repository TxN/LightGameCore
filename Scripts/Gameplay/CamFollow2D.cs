using UnityEngine;

namespace SMGCore {
	public sealed class CamFollow2D : MonoSingleton<CamFollow2D> {
		public AnimationCurve lerpCoef  = null;
		public Transform      player    = null;
		public float          initDelta = 10f;

		float _initZ     = 0f;
		float _moveError = 0f;

		void Start() {
			_initZ = transform.position.z;
		}

		void LateUpdate() {
			if ( !player ) {
				return;
			}
			_moveError = Vector3.Distance(transform.position, player.position) - initDelta;
			var cLerp = lerpCoef.Evaluate(_moveError);
			var newPos = Vector3.Lerp(transform.position, player.position, cLerp);
			newPos.z = _initZ;
			transform.position = newPos;
		}

		public float Map(float value, float fromSource, float toSource, float fromTarget, float toTarget, bool clamp = false) {
			var val = (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
			if ( clamp ) {
				val = Mathf.Clamp(val, fromTarget, toTarget);
			}
			return val;
		}

		public void ReplaceTargetByDummy() {
			var dummy = new GameObject("[camDummy]");
			dummy.transform.position = player.transform.position;
			player = dummy.transform;
		}
	}
}
