#if UNITY_2017_1_OR_NEWER
using UnityEngine;

namespace SMGCore {
	public class ManualSingleton<T> : MonoBehaviour where T : MonoBehaviour {
		public static T Instance {
			get {
				if ( !_instance ) {
					_instance = FindObjectOfType<T>(true);
					if ( !_instance ) {
						Debug.LogWarning($"ManualSingleton({typeof(T).ToString()}).InstanceProperty: Cannot find instance");
					}
				}
				return _instance;
			}
		}

		/// <summary>
		/// Instance без попытки поиска синглтона на сцене. Полезно, если есть ожидаемая вероятность того, что синглтона не существует.
		/// </summary>
		public static T WeakInstance {
			get {
				if ( !_instance ) {
					return null;
				}
				return _instance;
			}
		}

		static T _instance = null;

		protected virtual void Awake() {
			if ( _instance && _instance != this ) {
				Debug.LogWarning($"ManualSingleton({typeof(T).ToString()}).Awake: another alive instance is found");
				Destroy(this);
				return;
			}

			if ( !_instance ) {
				_instance = GetComponent<T>();
				if ( !_instance ) {
					Debug.LogWarning($"ManualSingleton({typeof(T).ToString()}).Awake: Unable to set object as singleton instance: cannot find such component.");
				}
			}
		}
	}
}
#endif