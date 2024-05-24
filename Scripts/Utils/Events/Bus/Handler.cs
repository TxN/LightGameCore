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
				Debug.LogWarning($"Handler: {watcher} tries to subscribe to {action} again.");
			}
		}

		public void Unsubscribe(Action<T> action) =>
			SafeUnsubscribe(action);

		void SafeUnsubscribe(Action<T> action) {
			var index = _actions.IndexOf(action);
			SafeUnsubscribe(index);
			if ( (index < 0) && LogsEnabled ) {
				Debug.LogWarning($"Handler: Trying to unsubscribe action {action} without watcher.");
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
					Debug.LogException(e);
				}
			}
			CleanUp();
			if ( AllFireLogs ) {
				Debug.Log($"[{ typeof(T).Name}] fired (Listeners: {Watchers.Count})");
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
				if ( watcher is MonoBehaviour behaviour ) {
					if ( !behaviour ) {
						SafeUnsubscribe(i);
						count++;
					}
				}
			}
			if ( count > 0 ) {
				CleanUp();
			}
			if ( (count > 0) && LogsEnabled ) {
				Debug.LogError($"{count} destroyed scripts subscribed to event {typeof(T)}.");
			}
			return count == 0;
		}
	}
}
