using System;
using System.Collections.Generic;

#if UNITY_2017_3_OR_NEWER
using UnityEngine;
#endif

namespace SMGCore.EventSys {
	public readonly struct SubscriberSnapshot {
		public readonly object Watcher;
		public readonly string MethodName;
		public readonly string MethodDeclaringTypeName;
		public readonly bool IsPendingRemoval;

		public SubscriberSnapshot(object watcher, string methodName, string methodDeclaringTypeName, bool isPendingRemoval) {
			Watcher = watcher;
			MethodName = methodName;
			MethodDeclaringTypeName = methodDeclaringTypeName;
			IsPendingRemoval = isPendingRemoval;
		}
	}

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

		public virtual int FireDepth => 0;
		public virtual int PendingRemovalCount => 0;

		protected List<object> _watchers = new List<object>(100);

		public virtual void CleanUp() {
		}

		public virtual bool FixWatchers() {
			return false;
		}

		public virtual void CollectSubscribers(List<SubscriberSnapshot> output) {
			if ( output == null ) {
				return;
			}
			for ( var i = 0; i < _watchers.Count; i++ ) {
				output.Add(new SubscriberSnapshot(_watchers[i], "?", "?", false));
			}
		}
	}

	public class Handler<T> : HandlerBase {

		List<Action<T>> _actions = new List<Action<T>>(100);
		List<Action<T>> _removed = new List<Action<T>>(100);
		int _fireDepth;

		public override int FireDepth => _fireDepth;
		public override int PendingRemovalCount => _removed.Count;

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
			_fireDepth++;
			try {
				for (int i = 0; i < _actions.Count; i++) {
					var current = _actions[i];
					if ( !_removed.Contains(current) ) {
						current.Invoke(arg);
					}
				}
			} finally {
				_fireDepth--;
				if ( _fireDepth == 0 ) {
					CleanUp();
				}
			}
		}

		public override void CleanUp() {
			if ( _fireDepth > 0 ) {
				return;
			}
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
#if UNITY_2017_3_OR_NEWER
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
			return count == 0;
		}

		public override void CollectSubscribers(List<SubscriberSnapshot> output) {
			if ( output == null ) {
				return;
			}
			for ( var i = 0; i < _actions.Count; i++ ) {
				var action = _actions[i];
				var method = action?.Method;
				output.Add(new SubscriberSnapshot(
					i < _watchers.Count ? _watchers[i] : null,
					method != null ? method.Name : "null",
					method != null && method.DeclaringType != null ? method.DeclaringType.Name : "null",
					action != null && _removed.Contains(action)
				));
			}
		}
	}
}
