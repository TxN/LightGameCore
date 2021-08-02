using UnityEngine;

namespace SMGCore {
	public class ManualSingleton<T> : MonoBehaviour where T : MonoBehaviour {
		public static T Instance {
			get {
				if ( !_instance ) {
					_instance = (T)FindObjectOfType(typeof(T));
				}
				return _instance;
			}
		}

		static T _instance = null;

		protected virtual void Awake() {
			if ( _instance && _instance != this ) {
				Destroy(this);
				return;
			}

			if ( !_instance ) {
				_instance = gameObject.GetComponent<T>();
			}
		}
	}
}
