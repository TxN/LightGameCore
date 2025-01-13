#if UNITY_2017_1_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SMGCore;

namespace Utils {
	public sealed class EventBusManager : MonoSingleton<EventBusManager> {
		const float CleanUpInterval = 5f;

		public static bool Exists => _instance;

		public readonly List<WeakReference<BaseEventBus>> EventBuses = new List<WeakReference<BaseEventBus>>();

		readonly HashSet<WeakReference<BaseEventBus>> _toRemove = new HashSet<WeakReference<BaseEventBus>>();

		float _lastCleanupTime;
		int _mainThreadID;

		bool IsMainThread => _mainThreadID == System.Threading.Thread.CurrentThread.ManagedThreadId;

		void Update() {
			TryCleanUp();
		}			

		public void TryRegister(BaseEventBus addEventBus) {
			for ( int i = 0; i < EventBuses.Count; i++ ) {
				var wr = EventBuses[i];
				if ( wr == null) {
					continue;
				}
				if ( wr.TryGetTarget(out var eventBus) && ReferenceEquals(eventBus, addEventBus) ) {
					return;
				}
			}
#if UNITY_EDITOR
			if ( !IsMainThread ) {
				Debug.LogWarning("Eventbus is not on main thread. This is unsupported and may cause issues.");
			}
#endif
			EventBuses.Add(new WeakReference<BaseEventBus>(addEventBus));
		}

		protected override void Awake() {
			_mainThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
			base.Awake();
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnDestroy() {
			if ( _instance == this ) {
				_instance = null;
			}
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		public void TryUnregister(BaseEventBus eventBus) {
			foreach ( var wr in EventBuses ) {
				if ( wr.TryGetTarget(out var tmpEventBus) && ReferenceEquals(eventBus, tmpEventBus) ) {
					_toRemove.Add(wr);
					break;
				}
			}
		}	

		void OnSceneLoaded(Scene scene, LoadSceneMode mode) =>	CheckEventBusesOnLoad();

		[ContextMenu("Fix Watchers")]
		void CheckEventBusesOnLoad() => CheckEventBuses(x => x.FixWatchers());

		void TryCleanUp() {
			if ( Time.time - _lastCleanupTime > CleanUpInterval ) {
				CleanUp();
				_lastCleanupTime = Time.time;
			}
		}

		[ContextMenu("CleanUp")]
		void CleanUp() => CheckEventBuses(x => x.CleanUp());

		void CheckEventBuses(Action<BaseEventBus> act) {
			foreach ( var wr in EventBuses ) {
				if ( wr.TryGetTarget(out var eventBus) ) {
					act?.Invoke(eventBus);
				} else {
					_toRemove.Add(wr);
				}
			}
			var removed = EventBuses.RemoveAll(x => _toRemove.Contains(x));
			_toRemove.Clear();
#if UNITY_EDITOR
			if ( !IsMainThread ) {
				Debug.LogWarning("Eventbus is not on main thread. This is unsupported and may cause issues.");
			}
#endif
		}
	}
}
#else
using System;
using System.Collections.Generic;

namespace Utils {
	public sealed class EventBusManager {
		const float CleanUpInterval = 5f;

		public static bool Exists => _instance != null;

		public static EventBusManager Instance {
			get {
				if ( _instance == null ) {
					_instance = new EventBusManager();
				}
				return _instance;
			}
		}

		static EventBusManager _instance;
		float _lastCleanupTime;
		int _mainThreadID;

		public readonly List<WeakReference<BaseEventBus>> EventBuses = new List<WeakReference<BaseEventBus>>();

		readonly HashSet<WeakReference<BaseEventBus>> _toRemove = new HashSet<WeakReference<BaseEventBus>>();
		bool IsMainThread => _mainThreadID == System.Threading.Thread.CurrentThread.ManagedThreadId;

		public void Update(float time) {
			TryCleanUp(time);
		}

		public void TryRegister(BaseEventBus addEventBus) {
			for ( int i = 0; i < EventBuses.Count; i++ ) {
				var wr = EventBuses[i];
				if ( wr == null ) {
					continue;
				}
				if ( wr.TryGetTarget(out var eventBus) && ReferenceEquals(eventBus, addEventBus) ) {
					return;
				}
			}
			EventBuses.Add(new WeakReference<BaseEventBus>(addEventBus));
		}

		public void OnDestroy() {
			if ( _instance == this ) {
				_instance = null;
			}
		}

		public void TryUnregister(BaseEventBus eventBus) {
			foreach ( var wr in EventBuses ) {
				if ( wr.TryGetTarget(out var tmpEventBus) && ReferenceEquals(eventBus, tmpEventBus) ) {
					_toRemove.Add(wr);
					break;
				}
			}
		}

		void CheckEventBusesOnLoad() => CheckEventBuses(x => x.FixWatchers());

		void TryCleanUp(float time) {
			if ( time - _lastCleanupTime > CleanUpInterval ) {
				CleanUp();
				_lastCleanupTime = time;
			}
		}
		void CleanUp() => CheckEventBuses(x => x.CleanUp());

		void CheckEventBuses(Action<BaseEventBus> act) {
			foreach ( var wr in EventBuses ) {
				if ( wr.TryGetTarget(out var eventBus) ) {
					act?.Invoke(eventBus);
				}
				else {
					_toRemove.Add(wr);
				}
			}
			var removed = EventBuses.RemoveAll(x => _toRemove.Contains(x));
			_toRemove.Clear();
		}
	}
}
#endif