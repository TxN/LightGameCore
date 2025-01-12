using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils {
	sealed class Handler<T> : HandlerBase {
		List<Action<T>> _actions  = new List<Action<T>>(100);
		List<Action<T>> _removed  = new List<Action<T>>(100);
		public int ActionsCount => _actions.Count;

		public void Subscribe(object watcher, Action<T> action) {
			if ( _removed.Contains(action) ) {
				_removed.Remove(action);
			}
			if ( !_actions.Contains(action) ) {
				_actions.Add(action);
				Watchers.Add(watcher);
			} else if ( LogsEnabled ) {
#if UNITY_2017_1_OR_NEWER
				Debug.LogWarning($"Handler: {watcher} tries to subscribe to {action} again.");
#endif
			}
		}

		public void Unsubscribe(Action<T> action) =>
			SafeUnsubscribe(action);

		void SafeUnsubscribe(Action<T> action) {
			var index = _actions.IndexOf(action);
			SafeUnsubscribe(index);
			if ( (index < 0) && LogsEnabled ) {
#if UNITY_2017_1_OR_NEWER
				Debug.LogWarning($"Handler: Trying to unsubscribe action {action} without watcher.");
#endif
			}
		}

		void SafeUnsubscribe(int index) {
			if ( index >= 0 ) {
				_removed.Add(_actions[index]);
			}
		}

		void FullUnsubscribe(int index) {
			if ( index >= 0 ) {
				_actions.RemoveAt(index);
				if ( index < Watchers.Count ) {
					Watchers.RemoveAt(index);
				}
			}
		}

		void FullUnsubscribe(Action<T> action) {
			var index = _actions.IndexOf(action);
			FullUnsubscribe(index);
		}

		public void Fire(T arg) {
			for ( var i = 0; i < _actions.Count; i++ ) {
				var current = _actions[i];
				if ( _removed.Contains(current) ) {
					continue;
				}
				try {
					current.Invoke(arg);
				} catch ( Exception e ) {
#if UNITY_2017_1_OR_NEWER
					Debug.LogException(e);
#else
					Console.WriteLine( e.ToString() );
#endif
				}
			}
			CleanUp();
			if ( AllFireLogs ) {
#if UNITY_2017_1_OR_NEWER
				Debug.Log($"[{ typeof(T).Name}] fired (Listeners: {Watchers.Count})");
#endif
			}
		}

		public override void CleanUp() {
			foreach ( var item in _removed ) {
				FullUnsubscribe(item);
			}
			_removed.Clear();
		}

		public override bool FixWatchers() {
			CleanUp();
			var count = 0;
			for ( var i = 0; i < Watchers.Count; i++ ) {
				var watcher = Watchers[i];
				#if UNITY_2017_1_OR_NEWER
if ( watcher is MonoBehaviour behaviour ) {
					if ( !behaviour ) {
						SafeUnsubscribe(i);
						count++;
					}
				}
#endif
			}
			if ( count > 0 ) {
				CleanUp();
			}
#if UNITY_2017_1_OR_NEWER
			if ( (count > 0) && LogsEnabled ) {
				Debug.LogError($"{count} destroyed scripts subscribed to event {typeof(T)}.");
			}
#endif
			return count == 0;
		}
	}
}
