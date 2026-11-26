using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_2017_1_OR_NEWER
using UnityEngine;
using UnityEngine.SceneManagement;
#endif

namespace SMGCore.EventSys {
	public readonly struct EventChannelInfo {
		public readonly Type EventType;
		public readonly string Name;
		public readonly int ActiveCount;
		public readonly int PendingRemovalCount;
		public readonly int FireDepth;

		public EventChannelInfo(Type eventType, string name, int activeCount, int pendingRemovalCount, int fireDepth) {
			EventType = eventType;
			Name = name;
			ActiveCount = activeCount;
			PendingRemovalCount = pendingRemovalCount;
			FireDepth = fireDepth;
		}
	}

	public static class EventManager {
		internal interface IEventChannel {
			Type EventType { get; }
			string Name { get; }
			int ActiveCount { get; }
			int PendingRemovalCount { get; }
			int FireDepth { get; }
			int UnsubscribeAll(object watcher);
			int PurgeDestroyedWatchers();
			void ClearAndUnregister();
			void CollectSubscribers(List<SubscriberSnapshot> output);
		}

		sealed class EventChannel<T> : IEventChannel, ISubscriptionOwner where T : struct {
			const int InitialCapacity = 4;
			static readonly string ChannelName = typeof(T).FullName ?? typeof(T).Name;
			static readonly string ShortName = typeof(T).Name;

			public static readonly EventChannel<T> Instance = new EventChannel<T>();

			SubscriberList<Action<T>> _subscribers;
			bool _registered;

			public Type EventType => typeof(T);
			public string Name => ChannelName;
			public int ActiveCount => _subscribers.ActiveCount;
			public int PendingRemovalCount => _subscribers.PendingRemovalCount;
			public int FireDepth => _subscribers.FireDepth;

			public EventSubscription Subscribe(object watcher, Action<T> action) {
				var sub = _subscribers.Subscribe(this, watcher, action, InitialCapacity, ShortName);
				if ( sub.IsValid && !_registered ) {
					_registered = true;
					RegisterChannel(this);
				}
				return sub;
			}

			public void Unsubscribe(Action<T> action) =>
				_subscribers.Unsubscribe(action, ShortName);

			public int UnsubscribeAll(object watcher) =>
				_subscribers.UnsubscribeAll(watcher, ShortName);

			public void Fire(T args) {
				var count = _subscribers.BeginFire(ShortName);
				if ( count == 0 ) {
					return;
				}
				try {
					for ( var i = 0; i < count; i++ ) {
						if ( !_subscribers.TryGetActiveAction(i, ShortName, out var action) ) {
							continue;
						}
						try {
							action.Invoke(args);
						} catch ( Exception e ) {
							EventDiagnostics.LogException(e);
						}
					}
				} finally {
					_subscribers.EndFire();
				}
			}

			public int PurgeDestroyedWatchers() =>
				_subscribers.PurgeDestroyedWatchers(ShortName);

			public void ClearAndUnregister() {
				_subscribers.Clear();
				_registered = false;
			}

			public void CollectSubscribers(List<SubscriberSnapshot> output) =>
				_subscribers.CollectSubscribers(output);

			bool ISubscriptionOwner.UnsubscribeByToken(ulong token) =>
				_subscribers.UnsubscribeByToken(token, ShortName);
		}

		static readonly List<IEventChannel> _registeredChannels = new List<IEventChannel>(64);

		public static int ChannelCount => _registeredChannels.Count;

		public static bool DebugLogsEnabled {
			get => EventDiagnostics.DebugLogsEnabled;
			set => EventDiagnostics.DebugLogsEnabled = value;
		}

#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		static void InitEditorHooks() {
			EventDiagnostics.CaptureMainThread();
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		static void OnPlayModeStateChanged(PlayModeStateChange change) {
			if ( change == PlayModeStateChange.ExitingEditMode ) {
				// Runs before all [RuntimeInitializeOnLoadMethod(SubsystemRegistration)] callbacks.
				ResetAll();
			} else if ( change == PlayModeStateChange.EnteredEditMode ) {
				SceneManager.sceneUnloaded -= OnSceneUnloaded;
				ResetAll();
			}
		}
#endif

#if UNITY_2017_1_OR_NEWER
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void InitRuntimeState() {
			EventDiagnostics.CaptureMainThread();
#if !UNITY_EDITOR
			ResetAll();
#endif
			SceneManager.sceneUnloaded -= OnSceneUnloaded;
			SceneManager.sceneUnloaded += OnSceneUnloaded;
		}

		static void OnSceneUnloaded(Scene scene) {
			PurgeDestroyedWatchers();
		}
#endif

		public static EventSubscription Subscribe<T>(object watcher, Action<T> action) where T : struct =>
			EventChannel<T>.Instance.Subscribe(watcher, action);

		public static void Unsubscribe<T>(Action<T> action) where T : struct =>
			EventChannel<T>.Instance.Unsubscribe(action);

		public static int UnsubscribeAll(object watcher) {
			if ( watcher == null ) {
				return 0;
			}
			var removed = 0;
			for ( var i = 0; i < _registeredChannels.Count; i++ ) {
				removed += _registeredChannels[i].UnsubscribeAll(watcher);
			}
			return removed;
		}

		public static void Fire<T>() where T : struct =>
			EventChannel<T>.Instance.Fire(default);

		public static void Fire<T>(T args) where T : struct =>
			EventChannel<T>.Instance.Fire(args);

		public static int PurgeDestroyedWatchers() {
			var removed = 0;
			for ( var i = 0; i < _registeredChannels.Count; i++ ) {
				removed += _registeredChannels[i].PurgeDestroyedWatchers();
			}
			return removed;
		}

		public static void ResetAll() {
			for ( var i = 0; i < _registeredChannels.Count; i++ ) {
				_registeredChannels[i].ClearAndUnregister();
			}
			_registeredChannels.Clear();
		}

		public static void CollectChannels(List<EventChannelInfo> output) {
			if ( output == null ) {
				return;
			}
			output.Clear();
			for ( var i = 0; i < _registeredChannels.Count; i++ ) {
				var ch = _registeredChannels[i];
				output.Add(new EventChannelInfo(
					ch.EventType,
					ch.Name,
					ch.ActiveCount,
					ch.PendingRemovalCount,
					ch.FireDepth
				));
			}
		}

		public static void CollectChannelSubscribersAt(int channelIndex, List<SubscriberSnapshot> output) {
			if ( output == null ) {
				return;
			}
			output.Clear();
			if ( channelIndex < 0 || channelIndex >= _registeredChannels.Count ) {
				return;
			}
			_registeredChannels[channelIndex].CollectSubscribers(output);
		}

		public static void CollectChannelSubscribers(Type eventType, List<SubscriberSnapshot> output) {
			if ( eventType == null || output == null ) {
				return;
			}
			output.Clear();
			for ( var i = 0; i < _registeredChannels.Count; i++ ) {
				var ch = _registeredChannels[i];
				if ( ch.EventType == eventType ) {
					ch.CollectSubscribers(output);
					return;
				}
			}
		}

		static void RegisterChannel(IEventChannel channel) {
			_registeredChannels.Add(channel);
		}
	}
}
