using UnityEngine;

namespace CookingCapital.WorldMap {
	public sealed class ParallaxElement : MonoBehaviour {

		public Transform cam     = null;

		public Transform[] backgrounds;
		public float[]     parallaxScales;
		public float       smoothing      = 1f;

		private Vector3[] _parallaxInitPoss;
		private Vector3   _cameraInitPos;

		void Start() {
			_parallaxInitPoss = new Vector3[backgrounds.Length];
			_cameraInitPos    = cam.position;

			for ( int i = 0; i < backgrounds.Length; i++ ) {
				_parallaxInitPoss[i] = backgrounds[i].position;
			}
		}

		void LateUpdate() {
			float cameraDelta = cam.position.x - _cameraInitPos.x;

			
			for ( int i = 0; i < backgrounds.Length; i++ ) {
				if ( Mathf.Abs(parallaxScales[i]) <= Mathf.Epsilon) {
					continue;
				}
				Vector3 newPos = _parallaxInitPoss[i] - new Vector3(cameraDelta * parallaxScales[i] *0.01f, 0, 0 );
				backgrounds[i].position = newPos;
			}
		}

	}
}
