using System;
using System.Collections.Generic;

namespace SMGCore.EventSys {
	internal interface ISubscriptionOwner {
		bool UnsubscribeByToken(ulong token);
	}

	public readonly struct EventSubscription : IDisposable, IEquatable<EventSubscription> {
		readonly ISubscriptionOwner _owner;
		readonly ulong _token;

		internal EventSubscription(ISubscriptionOwner owner, ulong token) {
			_owner = owner;
			_token = token;
		}

		public bool IsValid => _owner != null && _token != 0;

		public void Dispose() {
			if ( _owner != null && _token != 0 ) {
				_owner.UnsubscribeByToken(_token);
			}
		}

		public bool Equals(EventSubscription other) =>
			ReferenceEquals(_owner, other._owner) && _token == other._token;

		public override bool Equals(object obj) =>
			obj is EventSubscription other && Equals(other);

		public override int GetHashCode() {
			unchecked {
				var hash = _owner != null ? _owner.GetHashCode() : 0;
				return (hash * 397) ^ _token.GetHashCode();
			}
		}

		public static bool operator ==(EventSubscription left, EventSubscription right) => left.Equals(right);
		public static bool operator !=(EventSubscription left, EventSubscription right) => !left.Equals(right);
	}

	public sealed class EventSubscriptionScope : IDisposable {
		List<EventSubscription> _subscriptions;

		public int Count => _subscriptions != null ? _subscriptions.Count : 0;

		public void Add(EventSubscription subscription) {
			if ( !subscription.IsValid ) {
				return;
			}
			_subscriptions ??= new List<EventSubscription>(4);
			for ( var i = 0; i < _subscriptions.Count; i++ ) {
				if ( _subscriptions[i].Equals(subscription) ) {
					return;
				}
			}
			_subscriptions.Add(subscription);
		}

		public void Clear() {
			if ( _subscriptions == null || _subscriptions.Count == 0 ) {
				return;
			}
			while ( _subscriptions.Count > 0 ) {
				var last = _subscriptions.Count - 1;
				var sub = _subscriptions[last];
				_subscriptions.RemoveAt(last);
				sub.Dispose();
			}
		}

		public void Dispose() => Clear();
	}
}
