using System;
using SMGCore.EventSys;

namespace Utils {
	public sealed class EventBus<T> : ISubscriptionOwner {
		const int InitialCapacity = 2;
		static readonly string EventName = typeof(T).Name;

		SubscriberList<Action<T>> _subscribers;

		public int SubscriberCount => _subscribers.ActiveCount;

		// Keep a single Subscribe method (no overloads) so BaseTerminal.RegisterEventHandler reflection stays unambiguous.
		public EventSubscription Subscribe(object watcher, Action<T> action) =>
			_subscribers.Subscribe(this, watcher, action, InitialCapacity, EventName);

		public void Unsubscribe(Action<T> action) =>
			_subscribers.Unsubscribe(action, EventName);

		public int UnsubscribeAll(object watcher) =>
			_subscribers.UnsubscribeAll(watcher, EventName);

		public void Fire(T arg) {
			var count = _subscribers.BeginFire(EventName);
			if ( count == 0 ) {
				return;
			}
			try {
				for ( var i = 0; i < count; i++ ) {
					if ( !_subscribers.TryGetActiveAction(i, EventName, out var action) ) {
						continue;
					}
					try {
						action.Invoke(arg);
					} catch ( Exception e ) {
						EventDiagnostics.LogException(e);
					}
				}
			} finally {
				_subscribers.EndFire();
			}
		}

		public void Clear() => _subscribers.Clear();

		public int PurgeDestroyedWatchers() => _subscribers.PurgeDestroyedWatchers(EventName);

		bool ISubscriptionOwner.UnsubscribeByToken(ulong token) =>
			_subscribers.UnsubscribeByToken(token, EventName);

		public override string ToString() => EventName;
	}

	public sealed class EventBus : ISubscriptionOwner {
		const int InitialCapacity = 2;
		const string EventName = "EventBus";

		SubscriberList<Action> _subscribers;

		public int SubscriberCount => _subscribers.ActiveCount;

		public EventSubscription Subscribe(object watcher, Action action) =>
			_subscribers.Subscribe(this, watcher, action, InitialCapacity, EventName);

		public void Unsubscribe(Action action) =>
			_subscribers.Unsubscribe(action, EventName);

		public int UnsubscribeAll(object watcher) =>
			_subscribers.UnsubscribeAll(watcher, EventName);

		public void Fire() {
			var count = _subscribers.BeginFire(EventName);
			if ( count == 0 ) {
				return;
			}
			try {
				for ( var i = 0; i < count; i++ ) {
					if ( !_subscribers.TryGetActiveAction(i, EventName, out var action) ) {
						continue;
					}
					try {
						action.Invoke();
					} catch ( Exception e ) {
						EventDiagnostics.LogException(e);
					}
				}
			} finally {
				_subscribers.EndFire();
			}
		}

		public void Clear() => _subscribers.Clear();

		public int PurgeDestroyedWatchers() => _subscribers.PurgeDestroyedWatchers(EventName);

		bool ISubscriptionOwner.UnsubscribeByToken(ulong token) =>
			_subscribers.UnsubscribeByToken(token, EventName);

		public override string ToString() => "Untyped";
	}
}
