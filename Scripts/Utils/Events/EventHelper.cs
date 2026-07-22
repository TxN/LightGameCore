#if UNITY_2017_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SMGCore.EventSys {
	public sealed class EventHelper : MonoBehaviour {

		[Serializable]
		public sealed class SubscriberInfo {
			public int Index;
			public string WatcherLabel = string.Empty;
			public string WatcherTypeName = string.Empty;
			public string MethodName = string.Empty;
			public string MethodDeclaringType = string.Empty;
			public UnityEngine.Object UnityWatcher;
			public bool IsNullOwner;
			public bool IsDestroyedUnityObject;
			public bool IsPendingRemoval;
		}

		[Serializable]
		public sealed class EventData {
			public string Name = string.Empty;
			public int SubscriberCount;
			public int NullOrDestroyedCount;
			public int PendingRemovalCount;
			public int FireDepth;
			public List<SubscriberInfo> Subscribers = new List<SubscriberInfo>(16);

			[NonSerialized]
			public Type Type;

			public EventData(Type type) {
				Type = type;
				Name = type != null ? type.FullName : string.Empty;
			}
		}

		[Header("Summary")]
		public int TotalEvents;
		public int TotalSubscribers;
		public int TotalNullOrDestroyed;
		public int TotalPendingRemoval;

		[Header("Options")]
		[Tooltip("Rebuild subscriber snapshot every AutoFillInterval seconds while playing.")]
		public bool AutoFillEnabled;

		[Min(0.05f)]
		public float AutoFillInterval = 0.5f;

		[Header("Events (subscription order = list order)")]
		public List<EventData> Events = new List<EventData>(100);

		readonly Dictionary<Type, string> _typeCache = new Dictionary<Type, string>();
		readonly List<SubscriberSnapshot> _snapshotBuffer = new List<SubscriberSnapshot>(128);
		readonly List<Type> _presentTypes = new List<Type>(64);
		float _cleanupTimer;
		float _autoFillTimer;

		void Awake() {
			DontDestroyOnLoad(gameObject);
		}

		void OnEnable() {
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		void OnDisable() {
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
			EventManager.Instance.CheckHandlersOnLoad();
		}

		void Update() {
			TryCleanUp();
			if ( !AutoFillEnabled ) {
				return;
			}
			_autoFillTimer += Time.unscaledDeltaTime;
			if ( _autoFillTimer < AutoFillInterval ) {
				return;
			}
			_autoFillTimer = 0f;
			Fill();
		}

		void TryCleanUp() {
			if ( _cleanupTimer > EventManager.CleanUpInterval ) {
				EventManager.Instance.CleanUp();
				_cleanupTimer = 0;
			} else {
				_cleanupTimer += Time.deltaTime;
			}
		}

		[ContextMenu("Fill")]
		public void Fill() {
			_presentTypes.Clear();
			TotalEvents = 0;
			TotalSubscribers = 0;
			TotalNullOrDestroyed = 0;
			TotalPendingRemoval = 0;

			var handlerIter = EventManager.Instance.Handlers.GetEnumerator();
			while ( handlerIter.MoveNext() ) {
				var pair = handlerIter.Current;
				_presentTypes.Add(pair.Key);
				var eventData = GetOrCreateEventData(pair.Key);
				FillEvent(pair.Value, eventData);
				TotalEvents++;
				TotalSubscribers += eventData.SubscriberCount;
				TotalNullOrDestroyed += eventData.NullOrDestroyedCount;
				TotalPendingRemoval += eventData.PendingRemovalCount;
			}

			for ( var i = Events.Count - 1; i >= 0; i-- ) {
				var eventData = Events[i];
				if ( eventData.Type == null || !_presentTypes.Contains(eventData.Type) ) {
					Events.RemoveAt(i);
				}
			}

			Events.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
		}

		[ContextMenu("Clear Snapshot")]
		public void ClearSnapshot() {
			Events.Clear();
			TotalEvents = 0;
			TotalSubscribers = 0;
			TotalNullOrDestroyed = 0;
			TotalPendingRemoval = 0;
		}

		[ContextMenu("Fix Null Watchers")]
		public void FixNullWatchers() {
			EventManager.Instance.CheckHandlersOnLoad();
			Fill();
		}

		[ContextMenu("Log Snapshot To Console")]
		public void LogSnapshotToConsole() {
			Fill();
			var sb = new StringBuilder(2048);
			sb.Append("EventManager snapshot: events=").Append(TotalEvents)
				.Append(", subscribers=").Append(TotalSubscribers)
				.Append(", nullOrDestroyed=").Append(TotalNullOrDestroyed)
				.Append(", pendingRemoval=").Append(TotalPendingRemoval)
				.AppendLine();

			for ( var e = 0; e < Events.Count; e++ ) {
				var eventData = Events[e];
				sb.Append('[').Append(eventData.Name).Append("] count=")
					.Append(eventData.SubscriberCount)
					.Append(", nullOrDestroyed=").Append(eventData.NullOrDestroyedCount)
					.Append(", pendingRemoval=").Append(eventData.PendingRemovalCount)
					.Append(", fireDepth=").Append(eventData.FireDepth)
					.AppendLine();
				for ( var i = 0; i < eventData.Subscribers.Count; i++ ) {
					var sub = eventData.Subscribers[i];
					sb.Append("  #").Append(sub.Index).Append(' ')
						.Append(sub.WatcherLabel)
						.Append(" -> ").Append(sub.MethodDeclaringType).Append('.').Append(sub.MethodName);
					if ( sub.IsNullOwner ) {
						sb.Append(sub.IsDestroyedUnityObject ? " [DESTROYED]" : " [NULL]");
					}
					if ( sub.IsPendingRemoval ) {
						sb.Append(" [PENDING_REMOVAL]");
					}
					sb.AppendLine();
				}
			}

			Debug.Log(sb.ToString(), this);
		}

		void FillEvent(HandlerBase handler, EventData data) {
			data.Subscribers.Clear();
			data.NullOrDestroyedCount = 0;
			data.FireDepth = handler.FireDepth;
			data.PendingRemovalCount = handler.PendingRemovalCount;

			_snapshotBuffer.Clear();
			handler.CollectSubscribers(_snapshotBuffer);

			for ( var i = 0; i < _snapshotBuffer.Count; i++ ) {
				var snap = _snapshotBuffer[i];
				var info = CreateSubscriberInfo(i, snap);
				data.Subscribers.Add(info);
				if ( info.IsNullOwner ) {
					data.NullOrDestroyedCount++;
				}
			}

			data.SubscriberCount = data.Subscribers.Count;
		}

		SubscriberInfo CreateSubscriberInfo(int index, SubscriberSnapshot snap) {
			var info = new SubscriberInfo {
				Index = index,
				MethodName = snap.MethodName ?? string.Empty,
				MethodDeclaringType = snap.MethodDeclaringTypeName ?? string.Empty,
				IsPendingRemoval = snap.IsPendingRemoval,
			};

			var watcher = snap.Watcher;
			if ( watcher == null ) {
				info.IsNullOwner = true;
				info.WatcherLabel = "<null>";
				info.WatcherTypeName = "null";
				return info;
			}

			if ( watcher is UnityEngine.Object unityObj ) {
				info.UnityWatcher = unityObj;
				if ( !unityObj ) {
					info.IsNullOwner = true;
					info.IsDestroyedUnityObject = true;
					info.WatcherLabel = "<destroyed UnityEngine.Object>";
					info.WatcherTypeName = "UnityEngine.Object";
					return info;
				}

				info.WatcherTypeName = GetTypeNameFromCache(unityObj.GetType());
				info.WatcherLabel = unityObj.name + " (" + info.WatcherTypeName + ")";
				return info;
			}

			info.WatcherTypeName = GetTypeNameFromCache(watcher.GetType());
			info.WatcherLabel = info.WatcherTypeName;
			return info;
		}

		EventData GetOrCreateEventData(Type type) {
			for ( var i = 0; i < Events.Count; i++ ) {
				if ( Events[i].Type == type ) {
					return Events[i];
				}
			}
			var created = new EventData(type);
			Events.Add(created);
			return created;
		}

		string GetTypeNameFromCache(Type type) {
			if ( type == null ) {
				return "null";
			}
			if ( !_typeCache.TryGetValue(type, out var name) ) {
				name = type.FullName ?? type.Name;
				_typeCache.Add(type, name);
			}
			return name;
		}
	}
}
#endif
