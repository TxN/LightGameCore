using System;
using System.Collections.Generic;
using System.Threading;

#if UNITY_2017_1_OR_NEWER
using UnityEngine;
#endif

namespace SMGCore.EventSys {
	public readonly struct SubscriberSnapshot {
		public readonly object Watcher;
#if UNITY_2017_1_OR_NEWER
		public readonly UnityEngine.Object UnityObject;
#endif
		public readonly string MethodName;
		public readonly string MethodDeclaringTypeName;
		public readonly bool IsPendingRemoval;
		public readonly bool IsDestroyedUnityObject;

		public SubscriberSnapshot(
			object watcher,
#if UNITY_2017_1_OR_NEWER
			UnityEngine.Object unityObject,
#endif
			string methodName,
			string methodDeclaringTypeName,
			bool isPendingRemoval,
			bool isDestroyedUnityObject) {
			Watcher = watcher;
#if UNITY_2017_1_OR_NEWER
			UnityObject = unityObject;
#endif
			MethodName = methodName;
			MethodDeclaringTypeName = methodDeclaringTypeName;
			IsPendingRemoval = isPendingRemoval;
			IsDestroyedUnityObject = isDestroyedUnityObject;
		}
	}

	internal static class EventDiagnostics {
		public static bool DebugLogsEnabled { get; set; }
		static int _mainThreadId;

		public static void CaptureMainThread() {
			_mainThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		public static void AssertMainThread(string context) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			var expected = _mainThreadId;
			if ( expected == 0 ) {
				return;
			}
			var current = Thread.CurrentThread.ManagedThreadId;
			if ( current != expected ) {
				Debug.LogError($"[Events] {context} called from non-main thread (thread {current}, expected {expected}). EventManager/EventBus are main-thread only.");
			}
#endif
		}

		public static void LogNullAction(string eventName, object watcher) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			Debug.LogError($"[Events] Null action passed to Subscribe on '{eventName}' (watcher: {FormatWatcher(watcher)}).");
#endif
		}

		public static void LogDuplicateSubscribe(string eventName, object watcher, Delegate action) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			if ( DebugLogsEnabled ) {
				Debug.LogWarning($"[Events] Duplicate subscribe to '{eventName}' by {FormatWatcher(watcher)} -> {FormatAction(action)}.");
			}
#endif
		}

		public static void LogDestroyedSubscribe(string eventName, object watcher, Delegate action) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			Debug.LogWarning($"[Events] Ignored subscribe to '{eventName}' for already destroyed UnityEngine.Object ({FormatWatcher(watcher)} -> {FormatAction(action)}).");
#endif
		}

		public static void LogZombieRemoved(string eventName, object watcher, Delegate action) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			Debug.LogWarning($"[Events] Removed destroyed UnityEngine.Object subscriber from '{eventName}' ({FormatWatcher(watcher)} -> {FormatAction(action)}).");
#endif
		}

		public static void LogException(Exception e) {
#if UNITY_2017_1_OR_NEWER
			Debug.LogException(e);
#else
			Console.WriteLine(e.ToString());
#endif
		}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
		static string FormatWatcher(object watcher) {
			if ( watcher == null ) {
				return "<null>";
			}
			if ( watcher is UnityEngine.Object unityObj ) {
				return unityObj ? $"{unityObj.name} ({unityObj.GetType().Name})" : $"<destroyed {unityObj.GetType().Name}>";
			}
			return watcher.GetType().Name;
		}

		static string FormatAction(Delegate action) {
			if ( action == null ) {
				return "<null>";
			}
			var method = action.Method;
			return method != null
				? $"{method.DeclaringType?.Name ?? "?"}.{method.Name}"
				: action.ToString();
		}
#endif
	}

	internal struct SubscriberEntry<TDelegate> where TDelegate : Delegate {
		public object Watcher;
#if UNITY_2017_1_OR_NEWER
		public UnityEngine.Object UnityWatcher;
		public UnityEngine.Object UnityTarget;
		public UnityEngine.Object[] ExtraUnityTargets;
		public bool HasUnityWatcher;
		public bool HasUnityTarget;
#endif
		public TDelegate Action;
		public ulong Token;
		public bool Removed;
	}

	internal struct SubscriberList<TDelegate> where TDelegate : Delegate {
		SubscriberEntry<TDelegate>[] _entries;
		int _count;
		int _fireDepth;
		int _pendingRemovals;
		ulong _nextToken;

		public int ActiveCount => _count - _pendingRemovals;
		public int PendingRemovalCount => _pendingRemovals;
		public int FireDepth => _fireDepth;
		public bool IsEmpty => _count == 0;

		public EventSubscription Subscribe(
			ISubscriptionOwner owner,
			object watcher,
			TDelegate action,
			int initialCapacity,
			string eventName) {
			if ( action == null ) {
				EventDiagnostics.LogNullAction(eventName, watcher);
				return default;
			}
			EventDiagnostics.AssertMainThread(eventName);

#if UNITY_2017_1_OR_NEWER
			UnityEngine.Object unityWatcher = null;
			var hasUnityWatcher = false;
			if ( watcher is UnityEngine.Object uw ) {
				if ( !uw ) {
					EventDiagnostics.LogDestroyedSubscribe(eventName, watcher, action);
					return default;
				}
				unityWatcher = uw;
				hasUnityWatcher = true;
			}

			UnityEngine.Object unityTarget = null;
			var hasUnityTarget = false;
			UnityEngine.Object[] extraUnityTargets = null;

			var invocationList = action.GetInvocationList();
			if ( invocationList.Length == 1 ) {
				var target = action.Target;
				if ( target is UnityEngine.Object ut && !ReferenceEquals(ut, watcher) ) {
					if ( !ut ) {
						EventDiagnostics.LogDestroyedSubscribe(eventName, watcher, action);
						return default;
					}
					unityTarget = ut;
					hasUnityTarget = true;
				}
			} else {
				List<UnityEngine.Object> extraList = null;
				for ( var i = 0; i < invocationList.Length; i++ ) {
					var target = invocationList[i].Target;
					if ( !(target is UnityEngine.Object ut) ) {
						continue;
					}
					if ( !ut ) {
						EventDiagnostics.LogDestroyedSubscribe(eventName, watcher, action);
						return default;
					}
					if ( ReferenceEquals(ut, watcher) || ReferenceEquals(ut, unityTarget) ) {
						continue;
					}
					if ( !hasUnityTarget ) {
						unityTarget = ut;
						hasUnityTarget = true;
					} else {
						extraList ??= new List<UnityEngine.Object>(2);
						if ( !extraList.Contains(ut) ) {
							extraList.Add(ut);
						}
					}
				}
				if ( extraList != null ) {
					extraUnityTargets = extraList.ToArray();
				}
			}
#endif

			for ( var i = 0; i < _count; i++ ) {
				if ( _entries[i].Removed ) {
					continue;
				}
#if UNITY_2017_1_OR_NEWER
				if ( IsDestroyedUnityEntry(i) ) {
					continue;
				}
#endif
				if ( ReferenceEquals(_entries[i].Watcher, watcher) && Equals(_entries[i].Action, action) ) {
					var existingToken = _entries[i].Token;
					EventDiagnostics.LogDuplicateSubscribe(eventName, watcher, action);
					return new EventSubscription(owner, existingToken);
				}
			}

			EnsureCapacity(_count + 1, initialCapacity);

			if ( ++_nextToken == 0 ) {
				_nextToken = 1;
			}
			var token = _nextToken;

			_entries[_count++] = new SubscriberEntry<TDelegate> {
				Watcher = watcher,
#if UNITY_2017_1_OR_NEWER
				UnityWatcher = unityWatcher,
				UnityTarget = unityTarget,
				ExtraUnityTargets = extraUnityTargets,
				HasUnityWatcher = hasUnityWatcher,
				HasUnityTarget = hasUnityTarget,
#endif
				Action = action,
				Token = token,
				Removed = false,
			};

			return new EventSubscription(owner, token);
		}

		public bool Unsubscribe(TDelegate action, string eventName) {
			if ( action == null || _count == 0 ) {
				return false;
			}
			EventDiagnostics.AssertMainThread(eventName);

			for ( var i = 0; i < _count; i++ ) {
				if ( !_entries[i].Removed && Equals(_entries[i].Action, action) ) {
					RemoveAtIndex(i);
					return true;
				}
			}
			return false;
		}

		public bool UnsubscribeByToken(ulong token, string eventName) {
			if ( token == 0 || _count == 0 ) {
				return false;
			}
			EventDiagnostics.AssertMainThread(eventName);

			for ( var i = 0; i < _count; i++ ) {
				if ( !_entries[i].Removed && _entries[i].Token == token ) {
					RemoveAtIndex(i);
					return true;
				}
			}
			return false;
		}

		public int UnsubscribeAll(object watcher, string eventName) {
			if ( watcher == null || _count == 0 ) {
				return 0;
			}
			EventDiagnostics.AssertMainThread(eventName);

			var removed = 0;
			for ( var i = _count - 1; i >= 0; i-- ) {
				if ( !_entries[i].Removed && ReferenceEquals(_entries[i].Watcher, watcher) ) {
					RemoveAtIndex(i);
					removed++;
				}
			}
			return removed;
		}

		public void Clear() {
			if ( _count == 0 ) {
				return;
			}
			if ( _fireDepth == 0 ) {
				Array.Clear(_entries, 0, _count);
				_count = 0;
				_pendingRemovals = 0;
				return;
			}
			for ( var i = 0; i < _count; i++ ) {
				if ( !_entries[i].Removed ) {
					_entries[i].Removed = true;
					_pendingRemovals++;
				}
			}
		}

		public int PurgeDestroyedWatchers(string eventName) {
#if UNITY_2017_1_OR_NEWER
			if ( _count == 0 ) {
				return 0;
			}
			var removed = 0;
			for ( var i = _count - 1; i >= 0; i-- ) {
				if ( !_entries[i].Removed && IsDestroyedUnityEntry(i) ) {
					var watcher = _entries[i].Watcher;
					var deadAction = _entries[i].Action;
					RemoveAtIndex(i);
					removed++;
					EventDiagnostics.LogZombieRemoved(eventName, watcher, deadAction);
					if ( i > _count ) {
						i = _count;
					}
				}
			}
			return removed;
#else
			return 0;
#endif
		}

		public int BeginFire(string eventName) {
			EventDiagnostics.AssertMainThread(eventName);
			var count = _count;
			if ( count == 0 ) {
				return 0;
			}
			_fireDepth++;
			return count;
		}

		public bool TryGetActiveAction(int index, string eventName, out TDelegate action) {
			if ( _entries[index].Removed ) {
				action = null;
				return false;
			}
#if UNITY_2017_1_OR_NEWER
			if ( IsDestroyedUnityEntry(index) ) {
				var watcher = _entries[index].Watcher;
				var deadAction = _entries[index].Action;
				_entries[index].Removed = true;
				_pendingRemovals++;
				action = null;
				EventDiagnostics.LogZombieRemoved(eventName, watcher, deadAction);
				return false;
			}
#endif
			action = _entries[index].Action;
			return action != null;
		}

		public void EndFire() {
			_fireDepth--;
			if ( _fireDepth == 0 && _pendingRemovals > 0 ) {
				Compact();
			}
		}

		public void CollectSubscribers(List<SubscriberSnapshot> output) {
			if ( output == null || _count == 0 ) {
				return;
			}
			for ( var i = 0; i < _count; i++ ) {
				var action = _entries[i].Action;
				var method = action?.Method;
#if UNITY_2017_1_OR_NEWER
				var isDestroyed = IsDestroyedUnityEntry(i);
				UnityEngine.Object unityObj = null;
				if ( !isDestroyed ) {
					if ( _entries[i].HasUnityWatcher ) {
						unityObj = _entries[i].UnityWatcher;
					} else if ( _entries[i].HasUnityTarget ) {
						unityObj = _entries[i].UnityTarget;
					}
				}
#else
				const bool isDestroyed = false;
#endif
				output.Add(new SubscriberSnapshot(
					_entries[i].Watcher,
#if UNITY_2017_1_OR_NEWER
					unityObj,
#endif
					method != null ? method.Name : "null",
					method != null && method.DeclaringType != null ? method.DeclaringType.Name : "null",
					_entries[i].Removed,
					isDestroyed
				));
			}
		}

#if UNITY_2017_1_OR_NEWER
		bool IsDestroyedUnityEntry(int index) {
			if ( (_entries[index].HasUnityWatcher && !_entries[index].UnityWatcher)
				|| (_entries[index].HasUnityTarget && !_entries[index].UnityTarget) ) {
				return true;
			}
			var extras = _entries[index].ExtraUnityTargets;
			if ( extras != null ) {
				for ( var i = 0; i < extras.Length; i++ ) {
					if ( !extras[i] ) {
						return true;
					}
				}
			}
			return false;
		}
#endif

		void EnsureCapacity(int required, int initialCapacity) {
			if ( _entries == null ) {
				var cap = initialCapacity > required ? initialCapacity : required;
				_entries = new SubscriberEntry<TDelegate>[cap];
				return;
			}
			if ( required <= _entries.Length ) {
				return;
			}
			var newCapacity = _entries.Length * 2;
			if ( newCapacity < required ) {
				newCapacity = required;
			}
			var next = new SubscriberEntry<TDelegate>[newCapacity];
			Array.Copy(_entries, next, _count);
			_entries = next;
		}

		void RemoveAtIndex(int index) {
			if ( _fireDepth == 0 ) {
				var tailCount = _count - index - 1;
				if ( tailCount > 0 ) {
					Array.Copy(_entries, index + 1, _entries, index, tailCount);
				}
				_entries[--_count] = default;
			} else if ( !_entries[index].Removed ) {
				_entries[index].Removed = true;
				_pendingRemovals++;
			}
		}

		void Compact() {
			var write = 0;
			for ( var read = 0; read < _count; read++ ) {
				if ( !_entries[read].Removed ) {
					if ( write != read ) {
						_entries[write] = _entries[read];
					}
					write++;
				}
			}
			if ( write < _count ) {
				Array.Clear(_entries, write, _count - write);
				_count = write;
			}
			_pendingRemovals = 0;
		}
	}
}
