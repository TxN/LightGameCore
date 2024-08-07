using System;
using System.Collections.Concurrent;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_2017_3_OR_NEWER
using UnityEngine;
#endif

namespace SMGCore.EventSys {
	public sealed class EventManager {

		public const float CleanUpInterval = 10.0f;

		static EventManager _instance = null;
		public static EventManager Instance {
			get {
				if ( _instance == null ) {
					_instance = new EventManager();
#if UNITY_2017_3_OR_NEWER
					_instance.AddHelper();
#endif
				}
				return _instance;
			}
		}

		public EventManager() {
#if UNITY_EDITOR
			EditorApplication.playModeStateChanged += OnPlaymodeChanged;
#endif
		}

#if UNITY_EDITOR
		void OnPlaymodeChanged(PlayModeStateChange e) {
			if ( e == PlayModeStateChange.EnteredEditMode ) {
				EditorApplication.playModeStateChanged -= OnPlaymodeChanged;
				if ( _instance != null ) {
					CleanUp();
				}
				_instance = null;
				//Debug.Log("Clear EventManager instance");
			}
		}
#endif

		public ConcurrentDictionary<Type, HandlerBase> Handlers {
			get {
				return _handlers;
			}
		}

		ConcurrentDictionary<Type, HandlerBase> _handlers = new ConcurrentDictionary<Type, HandlerBase>();

		public static void Subscribe<T>(object watcher, Action<T> action) where T : struct {
			Instance.Sub(watcher, action);
		}

		public static void Unsubscribe<T>(Action<T> action) where T : struct {
			if ( _instance != null ) {
				Instance.Unsub(action);
			}
		}

		public static void Fire<T>() where T : struct {
			var emptyStruct = new T();
			Instance.FireEvent(emptyStruct);
		}

		public static void Fire<T>(T args) where T : struct {
			Instance.FireEvent(args);
		}

		public static bool HasWatchers<T>() where T : struct {
			return Instance.HasWatchersDirect<T>();
		}

		void Sub<T>(object watcher, Action<T> action) {
			HandlerBase handler;
			if ( !_handlers.TryGetValue(typeof(T), out handler) ) {
				handler = _handlers.GetOrAdd(typeof(T), new Handler<T>());
			}
			(handler as Handler<T>).Subscribe(watcher, action);
		}

		void Unsub<T>(Action<T> action) {
			HandlerBase handler = null;
			if ( _handlers.TryGetValue(typeof(T), out handler) ) {
				(handler as Handler<T>).Unsubscribe(action);
			}
		}

		void FireEvent<T>(T args) {
			HandlerBase handler = null;
			if (!_handlers.TryGetValue(typeof(T), out handler)) {
				handler = _handlers.GetOrAdd(typeof(T), new Handler<T>());
			}
			(handler as Handler<T>).Fire(args);
		}

		bool HasWatchersDirect<T>() where T : struct {
			HandlerBase container = null;
			if (_handlers.TryGetValue(typeof(T), out container)) {
				return (container.Watchers.Count > 0);
			}
			return false;
		}

#if UNITY_2017_3_OR_NEWER
		void AddHelper() {
			var go = new GameObject("[EventHelper]");
			go.AddComponent<EventHelper>();
		}
#endif

		public void CheckHandlersOnLoad() {
			var iter = _handlers.GetEnumerator();
			while (iter.MoveNext()) {
				iter.Current.Value.FixWatchers();
			}
		}

		public void CleanUp() {
			var iter = _handlers.GetEnumerator();
			while (iter.MoveNext()) {
				iter.Current.Value.CleanUp();
			}
		}
	}
}
