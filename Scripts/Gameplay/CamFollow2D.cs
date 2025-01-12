#if UNITY_2017_1_OR_NEWER
using UnityEngine;

namespace SMGCore {
	public sealed class CamFollow2D : MonoSingleton<CamFollow2D> {
		public AnimationCurve lerpCoef           = null;
		public Transform      player             = null;
		public float          initDelta          = 10f;
		public Transform      DebugGizmo         = null;
		public Vector3        ShiftOffset        = new Vector3(0, 0, 0);
		public AnimationCurve VerticalShiftCurve = null;
		public bool           VerticalSpeedShift = false;

		float _initZ     = 0f;
		float _moveError = 0f;

		Vector3 _targetLastPos    = Vector3.zero;

		void Start() {
			if ( VerticalShiftCurve != null ) {
				VerticalShiftCurve.preWrapMode = WrapMode.Clamp;
				VerticalShiftCurve.postWrapMode = WrapMode.Clamp;
			}
			_initZ = transform.position.z;
			_targetLastPos = player.position;
		}

		void LateUpdate() {
			if ( !player ) {
				return;
			}
			var targetPosition = player.position;
			targetPosition += ShiftOffset;
			var vSpeed = (player.position.y - _targetLastPos.y) / Time.deltaTime;
			if ( VerticalSpeedShift && vSpeed < 0 ) {
				var val = VerticalShiftCurve.Evaluate(-vSpeed);
				targetPosition += Vector3.up * val;
			}
			_moveError = Vector3.Distance(transform.position, targetPosition) - initDelta;
			var cLerp = lerpCoef.Evaluate(_moveError);
			var newPos = Vector3.Lerp(transform.position, targetPosition, cLerp * Time.deltaTime);
			newPos.z = _initZ;
			transform.position = newPos;
			if ( DebugGizmo ) {
				DebugGizmo.transform.position =  new Vector3(targetPosition.x, targetPosition.y, 0.25f);
			}
			_targetLastPos = player.position;
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
#endif