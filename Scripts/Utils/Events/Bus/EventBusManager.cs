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