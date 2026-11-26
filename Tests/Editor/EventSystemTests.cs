using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Utils;

namespace SMGCore.EventSys.Tests {
	public sealed class EventSystemTests {
		struct TestEvent {
			public int Value;
		}

		struct SecondTestEvent {
			public string Label;
		}

		sealed class DummyBehaviour : MonoBehaviour {
			public int CallCount;
			public int LastValue;

			public void OnTestEvent(TestEvent e) {
				CallCount++;
				LastValue = e.Value;
			}

			public void OnInt(int value) {
				CallCount++;
				LastValue = value;
			}

			public void OnPing() {
				CallCount++;
			}
		}

		sealed class DummyScriptableObject : ScriptableObject {
			public int CallCount;

			public void OnTestEvent(TestEvent e) {
				CallCount++;
			}
		}

		static int _sharedStaticCalls;
		static void SharedStaticHandler(int _) => _sharedStaticCalls++;

		readonly List<UnityEngine.Object> _createdUnityObjects = new List<UnityEngine.Object>();

		[SetUp]
		public void SetUp() {
			EventManager.ResetAll();
			EventManager.DebugLogsEnabled = false;
			_sharedStaticCalls = 0;
		}

		[TearDown]
		public void TearDown() {
			EventManager.ResetAll();
			for ( var i = _createdUnityObjects.Count - 1; i >= 0; i-- ) {
				var obj = _createdUnityObjects[i];
				if ( obj ) {
					UnityEngine.Object.DestroyImmediate(obj);
				}
			}
			_createdUnityObjects.Clear();
		}

		T Track<T>(T obj) where T : UnityEngine.Object {
			_createdUnityObjects.Add(obj);
			return obj;
		}

		[Test]
		public void EventBus_DeliversInSubscriptionOrder_AndHandlesEmptyFire() {
			var bus = new EventBus<int>();
			var untyped = new EventBus();
			Assert.DoesNotThrow(() => bus.Fire(42));
			Assert.DoesNotThrow(() => untyped.Fire());

			var order = new List<int>();
			bus.Subscribe(this, v => order.Add(v + 1));
			bus.Subscribe(this, v => order.Add(v + 2));
			bus.Subscribe(this, v => order.Add(v + 3));

			bus.Fire(10);
			CollectionAssert.AreEqual(new[] { 11, 12, 13 }, order);
			Assert.AreEqual(3, bus.SubscriberCount);
		}

		[Test]
		public void EventManager_DeliversDefaultAndParameterizedEvents_InOrder() {
			var received = new List<int>();
			EventManager.Subscribe<TestEvent>(this, e => received.Add(e.Value * 10));
			EventManager.Subscribe<TestEvent>(this, e => received.Add(e.Value * 10 + 1));

			EventManager.Fire<TestEvent>();
			EventManager.Fire(new TestEvent { Value = 5 });

			CollectionAssert.AreEqual(new[] { 0, 1, 50, 51 }, received);
		}

		[Test]
		public void DoubleUnsubscribe_ThenResubscribe_DoesNotLoseSubscription() {
			var bus = new EventBus<int>();
			var busCalls = 0;
			Action<int> busHandler = _ => busCalls++;

			bus.Subscribe(this, busHandler);
			bus.Unsubscribe(busHandler);
			bus.Unsubscribe(busHandler);
			bus.Subscribe(this, busHandler);
			bus.Fire(1);
			Assert.AreEqual(1, busCalls);

			var emCalls = 0;
			Action<TestEvent> emHandler = _ => emCalls++;

			EventManager.Subscribe(this, emHandler);
			EventManager.Unsubscribe(emHandler);
			EventManager.Unsubscribe(emHandler);
			EventManager.Subscribe(this, emHandler);
			EventManager.Fire(new TestEvent { Value = 7 });
			Assert.AreEqual(1, emCalls);
		}

		[Test]
		public void DuplicateSubscribe_DeduplicatesAndReturnsValidToken() {
			var bus = new EventBus<int>();
			var calls = 0;
			Action<int> handler = _ => calls++;

			var sub1 = bus.Subscribe(this, handler);
			var sub2 = bus.Subscribe(this, handler);

			Assert.IsTrue(sub1.IsValid);
			Assert.IsTrue(sub2.IsValid);
			Assert.AreEqual(sub1, sub2);
			Assert.AreEqual(1, bus.SubscriberCount);

			bus.Fire(1);
			Assert.AreEqual(1, calls);

			sub2.Dispose();
			Assert.AreEqual(0, bus.SubscriberCount);
			bus.Fire(2);
			Assert.AreEqual(1, calls);
		}

		[Test]
		public void Subscribe_SharedDelegateAcrossWatchers_IsolatesWatchersAndIgnoresZombieEntries() {
			var bus = new EventBus<int>();
			var goA = Track(new GameObject("DeadA"));
			var compA = goA.AddComponent<DummyBehaviour>();
			var goB = Track(new GameObject("LiveB"));
			var compB = goB.AddComponent<DummyBehaviour>();

			bus.Subscribe(compA, SharedStaticHandler);
			UnityEngine.Object.DestroyImmediate(goA);

			// Subscribing a live watcher with the same static delegate must NOT match compA's zombie slot.
			var subB = bus.Subscribe(compB, SharedStaticHandler);
			Assert.IsTrue(subB.IsValid);

			LogAssert.Expect(LogType.Warning, new Regex("Removed destroyed UnityEngine\\.Object"));
			bus.Fire(1);
			Assert.AreEqual(1, _sharedStaticCalls);
			Assert.AreEqual(1, bus.SubscriberCount);

			// Two live watchers sharing the same static delegate get distinct tokens.
			var watcherC = new object();
			var subC = bus.Subscribe(watcherC, SharedStaticHandler);
			Assert.AreNotEqual(subB, subC);
			Assert.AreEqual(2, bus.SubscriberCount);

			bus.UnsubscribeAll(compB);
			Assert.AreEqual(1, bus.SubscriberCount);

			bus.Fire(2);
			Assert.AreEqual(2, _sharedStaticCalls);
		}

		[Test]
		public void Subscribe_DestroyedObjectWithUnpurgedZombieEntry_IsRejected() {
			var go = Track(new GameObject("DeadDuplicate"));
			var comp = go.AddComponent<DummyBehaviour>();
			var bus = new EventBus<int>();

			bus.Subscribe(comp, comp.OnInt);
			UnityEngine.Object.DestroyImmediate(go);

			LogAssert.Expect(LogType.Warning, new Regex("already destroyed UnityEngine\\.Object"));
			var secondSub = bus.Subscribe(comp, comp.OnInt);
			Assert.IsFalse(secondSub.IsValid);
		}

		[Test]
		public void MulticastDelegate_DestroyedNonLastTarget_IsDetectedOnSubscribeAndFire() {
			var go1 = Track(new GameObject("Multi1"));
			var comp1 = go1.AddComponent<DummyBehaviour>();
			var plainCalled = 0;

			Action multicast = comp1.OnPing;
			multicast += () => plainCalled++;

			var bus = new EventBus();
			bus.Subscribe(this, multicast);

			UnityEngine.Object.DestroyImmediate(go1);

			LogAssert.Expect(LogType.Warning, new Regex("Removed destroyed UnityEngine\\.Object"));
			bus.Fire();
			Assert.AreEqual(0, comp1.CallCount);
			Assert.AreEqual(0, plainCalled);
			Assert.AreEqual(0, bus.SubscriberCount);

			// Already destroyed non-last target must also be rejected at Subscribe time.
			LogAssert.Expect(LogType.Warning, new Regex("already destroyed UnityEngine\\.Object"));
			var sub = bus.Subscribe(this, multicast);
			Assert.IsFalse(sub.IsValid);
		}

		[Test]
		public void SubscribeDuringFire_DoesNotReceiveInFlightEvent_ReceivesNext() {
			var bus = new EventBus<int>();
			var secondCalls = new List<int>();
			var added = false;

			bus.Subscribe(this, v => {
				if ( !added ) {
					added = true;
					bus.Subscribe(this, x => secondCalls.Add(x));
				}
			});

			bus.Fire(1);
			CollectionAssert.IsEmpty(secondCalls);

			bus.Fire(2);
			CollectionAssert.AreEqual(new[] { 2 }, secondCalls);
		}

		[Test]
		public void ArrayGrowthDuringFire_IteratesSafelyOverResizedBuffer() {
			var bus = new EventBus<int>();
			var originalCalls = new List<int>();
			var dynamicSum = 0;

			bus.Subscribe(this, v => {
				originalCalls.Add(v);
				if ( v == 1 ) {
					for ( var i = 1; i <= 12; i++ ) {
						var weight = i;
						bus.Subscribe(this, _ => dynamicSum += weight);
					}
				}
			});
			bus.Subscribe(this, v => originalCalls.Add(v * 10));

			bus.Fire(1);
			CollectionAssert.AreEqual(new[] { 1, 10 }, originalCalls);
			Assert.AreEqual(0, dynamicSum);
			Assert.AreEqual(14, bus.SubscriberCount);

			bus.Fire(2);
			CollectionAssert.AreEqual(new[] { 1, 10, 2, 20 }, originalCalls);
			Assert.AreEqual(78, dynamicSum);
		}

		[Test]
		public void UnsubscribeLaterSubscriberDuringFire_SkipsItImmediately() {
			var bus = new EventBus<int>();
			var calledSecond = false;
			Action<int> second = _ => calledSecond = true;

			bus.Subscribe(this, _ => bus.Unsubscribe(second));
			bus.Subscribe(this, second);

			bus.Fire(1);
			Assert.IsFalse(calledSecond);
			Assert.AreEqual(1, bus.SubscriberCount);
		}

		[Test]
		public void UnsubscribeAndResubscribeSameDelegateDuringFire_PreservesSubscriptionForNextFire() {
			var calls = new List<int>();
			Action<TestEvent> second = null;
			second = e => calls.Add(e.Value);

			EventManager.Subscribe<TestEvent>(this, e => {
				if ( e.Value == 1 ) {
					EventManager.Unsubscribe(second);
					EventManager.Unsubscribe(second);
					EventManager.Subscribe(this, second);
				}
			});
			EventManager.Subscribe(this, second);

			EventManager.Fire(new TestEvent { Value = 1 });
			CollectionAssert.IsEmpty(calls);

			EventManager.Fire(new TestEvent { Value = 2 });
			CollectionAssert.AreEqual(new[] { 2 }, calls);
		}

		[Test]
		public void NestedFire_IsReentrantAndCompactsOnOuterExit() {
			var bus = new EventBus<int>();
			var log = new List<int>();
			Action<int> selfRemoving = null;
			selfRemoving = v => {
				log.Add(v);
				if ( v == 1 ) {
					bus.Unsubscribe(selfRemoving);
					bus.Fire(2);
				}
			};

			bus.Subscribe(this, selfRemoving);
			bus.Subscribe(this, v => log.Add(v * 100));

			bus.Fire(1);
			CollectionAssert.AreEqual(new[] { 1, 200, 100 }, log);
			Assert.AreEqual(1, bus.SubscriberCount);
		}

		[Test]
		public void ClearDuringFire_StopsRemainingSubscribersAndAllowsResubscribe() {
			var bus = new EventBus();
			var secondCalled = false;
			var newCalls = 0;

			bus.Subscribe(this, () => {
				bus.Clear();
				bus.Subscribe(this, () => newCalls++);
			});
			bus.Subscribe(this, () => secondCalled = true);

			bus.Fire();
			Assert.IsFalse(secondCalled);
			Assert.AreEqual(0, newCalls);
			Assert.AreEqual(1, bus.SubscriberCount);

			bus.Fire();
			Assert.AreEqual(1, newCalls);
		}

		[Test]
		public void UnsubscribeAll_And_PurgeDestroyedWatchers_DuringFire_CompactCleanly() {
			var bus = new EventBus<int>();
			var watcherToDrop = new object();
			var go = Track(new GameObject("DeadMidFire"));
			var comp = go.AddComponent<DummyBehaviour>();
			var droppedCalled = false;
			var survivorCalls = 0;

			bus.Subscribe(this, _ => {
				bus.UnsubscribeAll(watcherToDrop);
				bus.PurgeDestroyedWatchers();
			});
			bus.Subscribe(watcherToDrop, _ => droppedCalled = true);
			bus.Subscribe(comp, comp.OnInt);
			bus.Subscribe(this, _ => survivorCalls++);

			UnityEngine.Object.DestroyImmediate(go);

			LogAssert.Expect(LogType.Warning, new Regex("Removed destroyed UnityEngine\\.Object"));
			bus.Fire(1);

			Assert.IsFalse(droppedCalled);
			Assert.AreEqual(0, comp.CallCount);
			Assert.AreEqual(1, survivorCalls);
			Assert.AreEqual(2, bus.SubscriberCount);
		}

		[Test]
		public void EventManager_ResetAllDuringFire_WithResubscribe_MaintainsValidState() {
			var secondCalled = false;
			var newSubCalls = 0;

			EventManager.Subscribe<TestEvent>(this, _ => {
				EventManager.ResetAll();
				EventManager.Subscribe<TestEvent>(this, e => newSubCalls += e.Value);
			});
			EventManager.Subscribe<TestEvent>(this, _ => secondCalled = true);

			EventManager.Fire(new TestEvent { Value = 5 });
			Assert.IsFalse(secondCalled);
			Assert.AreEqual(0, newSubCalls);

			var channels = new List<EventChannelInfo>();
			EventManager.CollectChannels(channels);
			Assert.AreEqual(1, channels.Count);
			Assert.AreEqual(1, channels[0].ActiveCount);

			EventManager.Fire(new TestEvent { Value = 7 });
			Assert.AreEqual(7, newSubCalls);
		}

		[Test]
		public void StaleEventSubscription_DisposeAfterClearOrReset_DoesNotRemoveNewSubscriber() {
			var bus = new EventBus<int>();
			var staleSub = bus.Subscribe(this, _ => { });
			bus.Clear();

			var newCalls = 0;
			bus.Subscribe(this, _ => newCalls++);
			staleSub.Dispose();

			bus.Fire(1);
			Assert.AreEqual(1, newCalls);
			Assert.AreEqual(1, bus.SubscriberCount);
		}

		[Test]
		public void LogMessageReceived_ReentrancyDuringZombiePurge_DoesNotRecurseOrCorruptCount() {
			var bus = new EventBus<int>();
			var go = Track(new GameObject("LogReentryZombie"));
			var comp = go.AddComponent<DummyBehaviour>();
			var survivorCalls = 0;

			bus.Subscribe(comp, comp.OnInt);
			bus.Subscribe(this, _ => survivorCalls++);
			UnityEngine.Object.DestroyImmediate(go);

			var reentered = 0;
			Application.LogCallback onLog = (condition, stackTrace, type) => {
				if ( type == LogType.Warning && condition.Contains("Removed destroyed UnityEngine.Object") ) {
					reentered++;
					bus.PurgeDestroyedWatchers();
					bus.Clear();
				}
			};

			Application.logMessageReceived += onLog;
			try {
				LogAssert.Expect(LogType.Warning, new Regex("Removed destroyed UnityEngine\\.Object"));
				bus.Fire(1);
			} finally {
				Application.logMessageReceived -= onLog;
			}

			Assert.AreEqual(1, reentered);
			Assert.AreEqual(0, survivorCalls);
			Assert.AreEqual(0, bus.SubscriberCount);
		}

		[Test]
		public void ExceptionInSubscriber_DoesNotPreventRemainingSubscribers() {
			var busLog = new List<int>();
			var bus = new EventBus<int>();
			bus.Subscribe(this, _ => throw new InvalidOperationException("boom-bus"));
			bus.Subscribe(this, v => busLog.Add(v));
			bus.Subscribe(this, v => busLog.Add(v + 1));

			LogAssert.Expect(LogType.Exception, new Regex("boom-bus"));
			bus.Fire(10);
			CollectionAssert.AreEqual(new[] { 10, 11 }, busLog);

			var emLog = new List<int>();
			EventManager.Subscribe<TestEvent>(this, _ => throw new InvalidOperationException("boom-em"));
			EventManager.Subscribe<TestEvent>(this, e => emLog.Add(e.Value));
			EventManager.Subscribe<TestEvent>(this, e => emLog.Add(e.Value + 1));

			LogAssert.Expect(LogType.Exception, new Regex("boom-em"));
			EventManager.Fire(new TestEvent { Value = 20 });
			CollectionAssert.AreEqual(new[] { 20, 21 }, emLog);
		}

		[Test]
		public void UntypedEventBus_Parity_UnsubscribeExceptionsNestingAndZombiePurge() {
			var bus = new EventBus();
			var go = Track(new GameObject("UntypedZombie"));
			var comp = go.AddComponent<DummyBehaviour>();
			var calls = 0;
			Action handler = () => calls++;

			var sub1 = bus.Subscribe(this, handler);
			var sub2 = bus.Subscribe(this, handler);
			Assert.AreEqual(sub1, sub2);
			Assert.AreEqual(1, bus.SubscriberCount);

			bus.Unsubscribe(handler);
			Assert.AreEqual(0, bus.SubscriberCount);

			bus.Subscribe(comp, comp.OnPing);
			bus.Subscribe(this, () => throw new InvalidOperationException("boom-untyped"));
			bus.Subscribe(this, () => calls++);
			UnityEngine.Object.DestroyImmediate(go);

			LogAssert.Expect(LogType.Warning, new Regex("Removed destroyed UnityEngine\\.Object"));
			LogAssert.Expect(LogType.Exception, new Regex("boom-untyped"));
			bus.Fire();

			Assert.AreEqual(1, calls);
			Assert.AreEqual(2, bus.SubscriberCount);

			Assert.AreEqual(2, bus.UnsubscribeAll(this));
			Assert.AreEqual(0, bus.SubscriberCount);
		}

		[Test]
		public void DestroyedMonoBehaviourWatcher_IsAutoPurgedOnFire() {
			var go = Track(new GameObject("TestWatcher"));
			var comp = go.AddComponent<DummyBehaviour>();
			var otherCalled = 0;

			EventManager.Subscribe<TestEvent>(comp, comp.OnTestEvent);
			EventManager.Subscribe<TestEvent>(this, _ => otherCalled++);

			UnityEngine.Object.DestroyImmediate(go);

			LogAssert.Expect(LogType.Warning, new Regex("Removed destroyed UnityEngine\\.Object"));
			EventManager.Fire(new TestEvent { Value = 99 });

			Assert.AreEqual(0, comp.CallCount);
			Assert.AreEqual(1, otherCalled);
		}

		[Test]
		public void DestroyedActionTarget_WithDifferentLiveWatcher_IsAutoPurgedOnFire() {
			var go = Track(new GameObject("TestTarget"));
			var comp = go.AddComponent<DummyBehaviour>();
			var bus = new EventBus<int>();
			var liveOwner = new object();

			bus.Subscribe(liveOwner, comp.OnInt);
			UnityEngine.Object.DestroyImmediate(go);

			LogAssert.Expect(LogType.Warning, new Regex("Removed destroyed UnityEngine\\.Object"));
			bus.Fire(7);

			Assert.AreEqual(0, comp.CallCount);
			Assert.AreEqual(0, bus.SubscriberCount);
		}

		[Test]
		public void DestroyedScriptableObjectWatcher_IsPurgedByPurgeDestroyedWatchers() {
			var so = Track(ScriptableObject.CreateInstance<DummyScriptableObject>());
			EventManager.Subscribe<TestEvent>(so, so.OnTestEvent);

			UnityEngine.Object.DestroyImmediate(so);

			LogAssert.Expect(LogType.Warning, new Regex("Removed destroyed UnityEngine\\.Object"));
			var removed = EventManager.PurgeDestroyedWatchers();
			Assert.AreEqual(1, removed);
		}

		[Test]
		public void SubscribeWithAlreadyDestroyedUnityObject_IsRejected() {
			var go = Track(new GameObject("DeadBeforeSub"));
			var comp = go.AddComponent<DummyBehaviour>();
			UnityEngine.Object.DestroyImmediate(go);

			var bus = new EventBus();
			LogAssert.Expect(LogType.Warning, new Regex("already destroyed UnityEngine\\.Object"));
			var sub = bus.Subscribe(comp, comp.OnPing);

			Assert.IsFalse(sub.IsValid);
			Assert.AreEqual(0, bus.SubscriberCount);
		}

		[Test]
		public void EventSubscriptionScope_DisposesMixedSubscriptionsDeduplicatesAndHandlesReentrancy() {
			var bus = new EventBus<int>();
			var untyped = new EventBus();
			var count = 0;
			var secondCalled = false;

			var scope = new EventSubscriptionScope();
			Action<int> h = _ => count++;
			var subA = bus.Subscribe(this, h);
			scope.Add(subA);
			scope.Add(subA); // deduplicated
			Assert.AreEqual(1, scope.Count);

			scope.Add(untyped.Subscribe(this, () => {
				count++;
				scope.Clear();
			}));
			scope.Add(untyped.Subscribe(this, () => secondCalled = true));
			scope.Add(EventManager.Subscribe<TestEvent>(this, _ => count++));

			bus.Fire(1);
			EventManager.Fire(new TestEvent { Value = 1 });
			Assert.AreEqual(2, count);

			untyped.Fire(); // triggers scope.Clear() inside callback
			Assert.AreEqual(3, count);
			Assert.IsFalse(secondCalled);
			Assert.AreEqual(0, scope.Count);

			scope.Clear(); // idempotent
			bus.Fire(2);
			EventManager.Fire(new TestEvent { Value = 2 });
			Assert.AreEqual(3, count);
		}

		[Test]
		public void UnsubscribeAll_RemovesOnlyMatchingWatcherAcrossLambdasAndChannels() {
			var watcherA = new object();
			var watcherB = new object();
			var aCalls = 0;
			var bCalls = 0;

			var bus = new EventBus<int>();
			bus.Subscribe(watcherA, _ => aCalls++);
			bus.Subscribe(watcherA, _ => aCalls++);
			bus.Subscribe(watcherB, _ => bCalls++);

			EventManager.Subscribe<TestEvent>(watcherA, _ => aCalls++);
			EventManager.Subscribe<SecondTestEvent>(watcherA, _ => aCalls++);
			EventManager.Subscribe<TestEvent>(watcherB, _ => bCalls++);

			Assert.AreEqual(2, bus.UnsubscribeAll(watcherA));
			Assert.AreEqual(2, EventManager.UnsubscribeAll(watcherA));

			bus.Fire(1);
			EventManager.Fire(new TestEvent { Value = 1 });
			EventManager.Fire(new SecondTestEvent { Label = "x" });

			Assert.AreEqual(0, aCalls);
			Assert.AreEqual(2, bCalls);
		}

		[Test]
		public void NullAction_LogsErrorAndDoesNotBreakFire() {
			var bus = new EventBus<int>();
			LogAssert.Expect(LogType.Error, new Regex("Null action"));
			var sub = bus.Subscribe(this, null);
			Assert.IsFalse(sub.IsValid);
			Assert.AreEqual(0, bus.SubscriberCount);
			Assert.DoesNotThrow(() => bus.Fire(1));

			LogAssert.Expect(LogType.Error, new Regex("Null action"));
			var emSub = EventManager.Subscribe<TestEvent>(this, null);
			Assert.IsFalse(emSub.IsValid);
			Assert.DoesNotThrow(() => EventManager.Fire(new TestEvent { Value = 1 }));
		}

		[Test]
		public void CollectChannels_And_SubscribersSnapshot_AccurateDuringFire() {
			var channels = new List<EventChannelInfo>();
			var subs = new List<SubscriberSnapshot>();
			Action<TestEvent> second = _ => { };
			var observedFireDepth = -1;
			var observedPending = -1;
			var observedActive = -1;
			var observedPendingFlag = false;

			EventManager.Subscribe<TestEvent>(this, _ => {
				EventManager.Unsubscribe(second);
				EventManager.CollectChannels(channels);
				observedFireDepth = channels[0].FireDepth;
				observedPending = channels[0].PendingRemovalCount;
				observedActive = channels[0].ActiveCount;

				EventManager.CollectChannelSubscribersAt(0, subs);
				observedPendingFlag = subs[1].IsPendingRemoval;
			});
			EventManager.Subscribe(this, second);

			EventManager.Fire(new TestEvent { Value = 1 });

			Assert.AreEqual(1, observedFireDepth);
			Assert.AreEqual(1, observedPending);
			Assert.AreEqual(1, observedActive);
			Assert.IsTrue(observedPendingFlag);
		}

		[Test]
		public void EventManager_ResetAll_ClearsAndAllowsReRegistration() {
			var calls = 0;
			EventManager.Subscribe<TestEvent>(this, _ => calls++);

			var channels = new List<EventChannelInfo>();
			EventManager.CollectChannels(channels);
			Assert.AreEqual(1, channels.Count);

			EventManager.ResetAll();
			EventManager.CollectChannels(channels);
			Assert.AreEqual(0, channels.Count);

			EventManager.Fire(new TestEvent { Value = 1 });
			Assert.AreEqual(0, calls);

			EventManager.Subscribe<TestEvent>(this, _ => calls++);
			EventManager.CollectChannels(channels);
			Assert.AreEqual(1, channels.Count);

			EventManager.Fire(new TestEvent { Value = 2 });
			Assert.AreEqual(1, calls);
		}
	}
}
