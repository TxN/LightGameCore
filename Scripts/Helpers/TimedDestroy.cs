#if UNITY_2017_1_OR_NEWER
using UnityEngine;

namespace SMGCore {
	public sealed class TimedDestroy : MonoBehaviour {
		public void Activate(float delay) {
			Invoke("Kill", delay);
		}

		void Kill() {
			Destroy(this.gameObject);
		}
	}
}
#endif