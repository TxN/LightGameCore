using System;
using System.Collections.Generic;

#if UNITY_2017_3_OR_NEWER
using UnityEngine;
#endif

namespace SMGCore.EventSys {
	public abstract class HandlerBase {
		public static bool LogsEnabled {
			get { return false;	}
		}

		public static bool AllFireLogs {
			get { return LogsEnabled; }
		}

		public List<object> Watchers {
			get { return _watchers;	}
		}

		protected List<object> _watchers = new List<object>(100);

		public virtual void CleanUp() {
		}

		public virtual bool FixWatchers() {
			return false;
		}
	}

	public class Handler<T> : HandlerBase {

		List<Action<T>> _actions = new List<Action<T>>(100);
		List<Action<T>> _removed = new List<Action<T>>(100);

		public void Subscribe(object watcher, Action<T> action) {
			if ( _removed.Contains(action) ) {
				_removed.Remove(action);
			}
			if (!_actions.Contains(action)) {
				_actions.Add(action);
				_watchers.Add(watcher);
			}
		}

		public void Unsubscribe(Action<T> action) {
			SafeUnsubscribe(action);
		}

		void SafeUnsubscribe(Action<T> action) {
			var index = _actions.IndexOf(action);
			SafeUnsubscribe(index);
		}

		void SafeUnsubscribe(int index) {
			if ( index >= 0 ) {
				_removed.Add(_actions[index]);
			}
		}

		void FullUnsubscribe(int index) {
			if ( index >= 0 ) {
				_actions.RemoveAt(index);
				_watchers.RemoveAt(index);
			}
		}

		void FullUnsubscribe(Action<T> action) {
			var index = _actions.IndexOf(action);
			FullUnsubscribe(index);
		}

		public void Fire(T arg) {
			for (int i = 0; i < _actions.Count; i++) {
				var current = _actions[i];
				if ( !_removed.Contains(current) ) {
					current.Invoke(arg);
				}
			}
			CleanUp();
		}

		public override void CleanUp() {
			var iter = _removed.GetEnumerator();
			while (iter.MoveNext()) {
				FullUnsubscribe(iter.Current);
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
			return count == 0;
		}
	}
}
