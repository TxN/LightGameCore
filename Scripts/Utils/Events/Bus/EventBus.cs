using System;

namespace Utils {
	public abstract class BaseEventBus {
		protected abstract HandlerBase Handler { get; }

		public abstract void FixWatchers();
		public abstract void CleanUp();

		protected void TryRegister() =>	EventBusManager.Instance.TryRegister(this);

		protected void TryUnregister() {
			if ( Handler.Watchers.Count > 0 ) {
				return;
			}
			if ( EventBusManager.Exists ) {
				EventBusManager.Instance.TryUnregister(this);
			}
		}
	}

	public sealed class EventBus<T> : BaseEventBus {
		readonly Handler<T> _handler = new Handler<T>();

		protected override HandlerBase Handler => _handler;
		public int ActionsCount => _handler.ActionsCount;
		public void Subscribe(object watcher, Action<T> action) {
			TryRegister();
			_handler.Subscribe(watcher, action);
		}

		public void Unsubscribe(Action<T> action) =>
			_handler.Unsubscribe(action);

		public void Fire(T arg) {
			_handler.Fire(arg);
			TryUnregister();
		}

		public override void FixWatchers() {
			_handler.FixWatchers();
			TryUnregister();
		}

		public override void CleanUp() {
			_handler.CleanUp();
			TryUnregister();
		}

		public override string ToString() => typeof(T).Name;
	}

	public sealed class EventBus : BaseEventBus {
		readonly UntypedHandler _handler = new UntypedHandler();

		protected override HandlerBase Handler => _handler;

		public void Subscribe(object watcher, Action action) {
			TryRegister();
			_handler.Subscribe(watcher, action);
		}

		public void Unsubscribe(Action action) =>
			_handler.Unsubscribe(action);

		public void Fire() {
			_handler.Fire();
			TryUnregister();
		}

		public override void FixWatchers() {
			_handler.FixWatchers();
			TryUnregister();
		}

		public override void CleanUp() {
			_handler.CleanUp();
			TryUnregister();
		}

		public override string ToString() => "Untyped";
	}
}