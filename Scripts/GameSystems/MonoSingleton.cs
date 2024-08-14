using UnityEngine;

namespace SMGCore {
	public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour {
		public static T Instance {
			get {
				if ( !_instance ) {
					_instance = (T)FindObjectOfType(typeof(T));
				}
				if ( !_instance ) {
					var obj = new GameObject(typeof(T).ToString());
					obj.AddComponent<T>();
				}
				return _instance;
			}
		}

		public static T WeakInstance {
			get {
				if ( !_instance ) {
					return null;
				}
				return _instance;
			}
		}

		public static bool IsAlive { get { return _instance; } }

		protected static T _instance = null;

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
