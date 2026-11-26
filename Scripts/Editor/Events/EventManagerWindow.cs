using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SMGCore.EventSys.Editor {
	public sealed class EventManagerWindow : EditorWindow {
		sealed class SubscriberView {
			public int Index;
			public string WatcherLabel;
			public string MethodLabel;
			public UnityEngine.Object UnityObject;
			public bool IsNullOwner;
			public bool IsDestroyedUnityObject;
			public bool IsPendingRemoval;
		}

		sealed class ChannelView {
			public Type EventType;
			public string Name;
			public int ActiveCount;
			public int NullOrDestroyedCount;
			public int PendingRemovalCount;
			public int FireDepth;
			public bool Expanded;
			public readonly List<SubscriberView> Subscribers = new List<SubscriberView>(16);
		}

		readonly List<EventChannelInfo> _channelBuffer = new List<EventChannelInfo>(64);
		readonly List<SubscriberSnapshot> _subscriberBuffer = new List<SubscriberSnapshot>(128);
		readonly List<ChannelView> _views = new List<ChannelView>(64);
		readonly Dictionary<Type, bool> _expandedState = new Dictionary<Type, bool>();
		readonly Dictionary<Type, string> _typeNameCache = new Dictionary<Type, string>();

		Vector2 _scrollPos;
		string _searchFilter = string.Empty;
		bool _issuesOnly;
		bool _autoRefresh = true;
		float _autoRefreshInterval = 0.5f;
		double _nextRefreshTime;

		bool _pendingRefresh;
		bool _pendingPurge;
		bool _pendingLog;
		bool _pendingClearFilter;

		int _totalEvents;
		int _totalActiveSubscribers;
		int _totalNullOrDestroyed;
		int _totalPendingRemoval;

		[MenuItem("Tools/Events/EventManager Inspector")]
		public static void ShowWindow() {
			var window = GetWindow<EventManagerWindow>("EventManager");
			window.minSize = new Vector2(480f, 300f);
			window._pendingRefresh = true;
			window.Repaint();
		}

		void OnEnable() {
			EditorApplication.update += OnEditorUpdate;
			_pendingRefresh = true;
		}

		void OnDisable() {
			EditorApplication.update -= OnEditorUpdate;
			_views.Clear();
			_channelBuffer.Clear();
			_subscriberBuffer.Clear();
		}

		void OnEditorUpdate() {
			if ( !_autoRefresh ) {
				return;
			}
			var now = EditorApplication.timeSinceStartup;
			if ( now < _nextRefreshTime ) {
				return;
			}
			_nextRefreshTime = now + Mathf.Max(0.05f, _autoRefreshInterval);
			if ( !EditorApplication.isPlaying && _views.Count == 0 && EventManager.ChannelCount == 0 ) {
				return;
			}
			_pendingRefresh = true;
			Repaint();
		}

		void OnGUI() {
			if ( Event.current.type == EventType.Layout ) {
				ApplyPendingActions();
			}
			DrawToolbar();
			DrawSummary();
			DrawFilters();
			DrawChannels();
		}

		void ApplyPendingActions() {
			if ( _pendingClearFilter ) {
				_pendingClearFilter = false;
				_searchFilter = string.Empty;
				GUI.FocusControl(null);
			}
			if ( _pendingPurge ) {
				_pendingPurge = false;
				var removed = EventManager.PurgeDestroyedWatchers();
				_pendingRefresh = true;
				Debug.Log($"[EventManagerWindow] Purged {removed} destroyed subscriber(s).");
			}
			if ( _pendingRefresh || _pendingLog ) {
				var shouldLog = _pendingLog;
				_pendingRefresh = false;
				_pendingLog = false;
				RefreshSnapshot();
				if ( shouldLog ) {
					LogCurrentSnapshotToConsole();
				}
			}
		}

		void DrawToolbar() {
			using ( new EditorGUILayout.HorizontalScope(EditorStyles.toolbar) ) {
				if ( GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(64f)) ) {
					_pendingRefresh = true;
					Repaint();
				}
				if ( GUILayout.Button("Purge Destroyed", EditorStyles.toolbarButton, GUILayout.Width(112f)) ) {
					_pendingPurge = true;
					Repaint();
				}
				if ( GUILayout.Button("Log Snapshot", EditorStyles.toolbarButton, GUILayout.Width(96f)) ) {
					_pendingLog = true;
					Repaint();
				}

				GUILayout.FlexibleSpace();

				_autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(48f));
				EditorGUI.BeginDisabledGroup(!_autoRefresh);
				_autoRefreshInterval = Mathf.Clamp(
					EditorGUILayout.FloatField(_autoRefreshInterval, EditorStyles.toolbarTextField, GUILayout.Width(42f)),
					0.05f,
					10f
				);
				EditorGUI.EndDisabledGroup();
			}
		}

		void DrawSummary() {
			using ( new EditorGUILayout.HorizontalScope(EditorStyles.helpBox) ) {
				EditorGUILayout.LabelField($"Channels: {_totalEvents}", GUILayout.Width(100f));
				EditorGUILayout.LabelField($"Active: {_totalActiveSubscribers}", GUILayout.Width(100f));

				var prevColor = GUI.color;
				if ( _totalNullOrDestroyed > 0 ) {
					GUI.color = new Color(1f, 0.45f, 0.45f);
				}
				EditorGUILayout.LabelField($"Null/Destroyed: {_totalNullOrDestroyed}", GUILayout.Width(135f));
				GUI.color = prevColor;

				EditorGUILayout.LabelField($"Pending: {_totalPendingRemoval}", GUILayout.Width(90f));
				GUILayout.FlexibleSpace();
				EventManager.DebugLogsEnabled = EditorGUILayout.ToggleLeft("Debug Logs", EventManager.DebugLogsEnabled, GUILayout.Width(90f));
			}
		}

		void DrawFilters() {
			using ( new EditorGUILayout.HorizontalScope() ) {
				EditorGUILayout.LabelField("Filter", GUILayout.Width(40f));
				_searchFilter = EditorGUILayout.TextField(_searchFilter);
				if ( GUILayout.Button("Clear", GUILayout.Width(48f)) ) {
					_pendingClearFilter = true;
					Repaint();
				}
				_issuesOnly = GUILayout.Toggle(_issuesOnly, "Issues Only", "Button", GUILayout.Width(88f));
			}
		}

		void DrawChannels() {
			using ( var scroll = new EditorGUILayout.ScrollViewScope(_scrollPos) ) {
				_scrollPos = scroll.scrollPosition;

				if ( _views.Count == 0 ) {
					EditorGUILayout.HelpBox("No registered EventManager channels.", MessageType.Info);
					return;
				}

				var hasFilter = !string.IsNullOrEmpty(_searchFilter);
				for ( var i = 0; i < _views.Count; i++ ) {
					var view = _views[i];
					if ( _issuesOnly && view.NullOrDestroyedCount == 0 && view.PendingRemovalCount == 0 ) {
						continue;
					}
					if ( hasFilter && !MatchesFilter(view, _searchFilter) ) {
						continue;
					}

					DrawChannel(view);
				}
			}
		}

		void DrawChannel(ChannelView view) {
			var header = $"{view.Name}  (active: {view.ActiveCount}, total: {view.Subscribers.Count}";
			if ( view.NullOrDestroyedCount > 0 ) {
				header += $", issues: {view.NullOrDestroyedCount}";
			}
			if ( view.PendingRemovalCount > 0 ) {
				header += $", pending: {view.PendingRemovalCount}";
			}
			if ( view.FireDepth > 0 ) {
				header += $", fireDepth: {view.FireDepth}";
			}
			header += ")";

			var prevColor = GUI.color;
			if ( view.NullOrDestroyedCount > 0 ) {
				GUI.color = new Color(1f, 0.65f, 0.65f);
			}
			var expanded = EditorGUILayout.Foldout(view.Expanded, header, true);
			GUI.color = prevColor;

			if ( expanded != view.Expanded ) {
				view.Expanded = expanded;
				if ( view.EventType != null ) {
					_expandedState[view.EventType] = expanded;
				}
			}

			if ( !expanded ) {
				return;
			}

			EditorGUI.indentLevel++;
			for ( var s = 0; s < view.Subscribers.Count; s++ ) {
				var sub = view.Subscribers[s];
				using ( new EditorGUILayout.HorizontalScope() ) {
					var label = $"#{sub.Index} {sub.WatcherLabel} -> {sub.MethodLabel}";
					if ( sub.IsNullOwner ) {
						label += sub.IsDestroyedUnityObject ? " [DESTROYED]" : " [NULL]";
					}
					if ( sub.IsPendingRemoval ) {
						label += " [PENDING]";
					}

					var subColor = GUI.color;
					if ( sub.IsNullOwner ) {
						GUI.color = new Color(1f, 0.5f, 0.5f);
					} else if ( sub.IsPendingRemoval ) {
						GUI.color = new Color(1f, 0.85f, 0.45f);
					}
					EditorGUILayout.LabelField(label);
					GUI.color = subColor;

					if ( !sub.IsDestroyedUnityObject && sub.UnityObject != null && sub.UnityObject ) {
						if ( GUILayout.Button("Ping", GUILayout.Width(44f)) ) {
							EditorGUIUtility.PingObject(sub.UnityObject);
						}
					}
				}
			}
			EditorGUI.indentLevel--;
		}

		void RefreshSnapshot() {
			_views.Clear();
			_totalEvents = 0;
			_totalActiveSubscribers = 0;
			_totalNullOrDestroyed = 0;
			_totalPendingRemoval = 0;

			EventManager.CollectChannels(_channelBuffer);
			for ( var i = 0; i < _channelBuffer.Count; i++ ) {
				var info = _channelBuffer[i];
				var view = new ChannelView {
					EventType = info.EventType,
					Name = info.Name,
					ActiveCount = info.ActiveCount,
					PendingRemovalCount = info.PendingRemovalCount,
					FireDepth = info.FireDepth,
					Expanded = info.EventType != null && _expandedState.TryGetValue(info.EventType, out var exp) && exp,
				};

				EventManager.CollectChannelSubscribersAt(i, _subscriberBuffer);
				for ( var s = 0; s < _subscriberBuffer.Count; s++ ) {
					var subView = BuildSubscriberView(s, _subscriberBuffer[s]);
					view.Subscribers.Add(subView);
					if ( subView.IsNullOwner ) {
						view.NullOrDestroyedCount++;
					}
				}

				_views.Add(view);
				_totalEvents++;
				_totalActiveSubscribers += view.ActiveCount;
				_totalNullOrDestroyed += view.NullOrDestroyedCount;
				_totalPendingRemoval += view.PendingRemovalCount;
			}

			_channelBuffer.Clear();
			_subscriberBuffer.Clear();
			_views.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
		}

		SubscriberView BuildSubscriberView(int index, SubscriberSnapshot snap) {
			var view = new SubscriberView {
				Index = index,
				MethodLabel = $"{snap.MethodDeclaringTypeName}.{snap.MethodName}",
				UnityObject = snap.UnityObject,
				IsPendingRemoval = snap.IsPendingRemoval,
				IsDestroyedUnityObject = snap.IsDestroyedUnityObject,
			};

			if ( snap.IsDestroyedUnityObject ) {
				view.IsNullOwner = true;
				view.UnityObject = null;
				view.WatcherLabel = "<destroyed UnityEngine.Object>";
				return view;
			}

			var watcher = snap.Watcher;
			if ( watcher == null ) {
				view.IsNullOwner = true;
				view.WatcherLabel = "<null>";
				return view;
			}

			if ( watcher is UnityEngine.Object unityObj ) {
				if ( !unityObj ) {
					view.IsNullOwner = true;
					view.IsDestroyedUnityObject = true;
					view.UnityObject = null;
					view.WatcherLabel = "<destroyed UnityEngine.Object>";
					return view;
				}
				view.WatcherLabel = $"{unityObj.name} ({GetTypeName(unityObj.GetType())})";
				return view;
			}

			view.WatcherLabel = GetTypeName(watcher.GetType());
			return view;
		}

		string GetTypeName(Type type) {
			if ( type == null ) {
				return "null";
			}
			if ( !_typeNameCache.TryGetValue(type, out var name) ) {
				name = type.FullName ?? type.Name;
				_typeNameCache.Add(type, name);
			}
			return name;
		}

		static bool MatchesFilter(ChannelView view, string filter) {
			if ( view.Name != null && view.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ) {
				return true;
			}
			for ( var i = 0; i < view.Subscribers.Count; i++ ) {
				var sub = view.Subscribers[i];
				if ( (sub.WatcherLabel != null && sub.WatcherLabel.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
					(sub.MethodLabel != null && sub.MethodLabel.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0) ) {
					return true;
				}
			}
			return false;
		}

		void LogCurrentSnapshotToConsole() {
			var sb = new StringBuilder(2048);
			sb.Append("EventManager snapshot: channels=").Append(_totalEvents)
				.Append(", activeSubscribers=").Append(_totalActiveSubscribers)
				.Append(", nullOrDestroyed=").Append(_totalNullOrDestroyed)
				.Append(", pendingRemoval=").Append(_totalPendingRemoval)
				.AppendLine();

			for ( var i = 0; i < _views.Count; i++ ) {
				var view = _views[i];
				sb.Append('[').Append(view.Name).Append("] active=").Append(view.ActiveCount)
					.Append(", total=").Append(view.Subscribers.Count)
					.Append(", nullOrDestroyed=").Append(view.NullOrDestroyedCount)
					.Append(", pendingRemoval=").Append(view.PendingRemovalCount)
					.Append(", fireDepth=").Append(view.FireDepth)
					.AppendLine();
				for ( var s = 0; s < view.Subscribers.Count; s++ ) {
					var sub = view.Subscribers[s];
					sb.Append("  #").Append(sub.Index).Append(' ')
						.Append(sub.WatcherLabel)
						.Append(" -> ").Append(sub.MethodLabel);
					if ( sub.IsNullOwner ) {
						sb.Append(sub.IsDestroyedUnityObject ? " [DESTROYED]" : " [NULL]");
					}
					if ( sub.IsPendingRemoval ) {
						sb.Append(" [PENDING_REMOVAL]");
					}
					sb.AppendLine();
				}
			}

			Debug.Log(sb.ToString());
		}
	}
}
