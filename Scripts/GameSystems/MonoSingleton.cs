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

		static T _instance = null;

		protected virtual void Awake() {
			if ( _instance != null && _instance != this ) {
				Destroy(this);
				return;
			}

			if ( _instance == null ) {
				_instance = gameObject.GetComponent<T>();
			}
		}
	}
}
