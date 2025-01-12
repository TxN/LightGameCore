#if UNITY_2017_1_OR_NEWER
using UnityEngine;

namespace SMGCore {
	public class ConstantRotation : MonoBehaviour {
		public Vector3 LocalAxis;
		public float Speed = 30f;

		void Update() {
			transform.Rotate(LocalAxis, Speed * Time.deltaTime);
		}
	}
}
#endif