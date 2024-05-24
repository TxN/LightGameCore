using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils {
	sealed class UntypedHandler : HandlerBase {
		readonly List<Action> _actions  = new List<Action>(10);
		readonly List<Action> _removed  = new List<Action>(10);

		public void Subscribe(object watcher, Action action) {
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

		public void Unsubscribe(Action action) =>
			SafeUnsubscribe(action);

		void SafeUnsubscribe(Action action) {
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

		void FullUnsubscribe(Action action) {
			var index = _actions.IndexOf(action);
			FullUnsubscribe(index);
		}

		public void Fire() {
			for ( var i = 0; i < _actions.Count; i++ ) {
				var current = _actions[i];
				if ( _removed.Contains(current) ) {
					continue;
				}
				try {
					current.Invoke();
				} catch ( Exception e ) {
					Debug.LogException(e);
				}
			}
			CleanUp();
			if ( AllFireLogs ) {
				Debug.Log($"[{nameof(UntypedHandler)}] fired (Listeners: { Watchers.Count})");
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
				if ( watcher is MonoBehaviour comp ) {
					if ( !comp ) {
						SafeUnsubscribe(i);
						count++;
					}
				}
			}
			if ( count > 0 ) {
				CleanUp();
			}
			if ( (count > 0) && LogsEnabled ) {
				Debug.LogError($"{count} destroyed scripts subscribed to event {typeof(UntypedHandler)}.");
			}
			return (count == 0);
		}
	}
}
