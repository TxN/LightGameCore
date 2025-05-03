#if UNITY_2017_1_OR_NEWER
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SMGCore {
	public class ArbitraryPrefabPool : BasePool {
		PoolItem  _prefab     = null;
		Transform _itemHolder = null;

		[ShowInInspector]
		public int FreeInstancesCount => _freeInstances?.Count ?? 0;

		Queue<PoolItem> _freeInstances;
		public ArbitraryPrefabPool(PoolItem prefab, int initialCapacity, Transform itemHolder) {
			_prefab = prefab;
			_itemHolder = itemHolder;
			FillInstances(initialCapacity);
		}

		public override PoolItem GetGeneric() {
			if ( _freeInstances.Count > 0 ) {
				var item = _freeInstances.Dequeue();
				item.transform.SetParent(null);
				item.gameObject.SetActive(true);
				return item;
			}
			return GetNewGeneric();
		}

		public PoolItem GetSourcePrefab => _prefab;

		public override PoolItem GetNewGeneric() {
			var item = CreateItem();
			item.transform.SetParent(null);
			item.gameObject.SetActive(true);
			return item;
		}

		public override void ReturnGeneric(PoolItem item) {
			if ( !item ) {
				return;
			}
			item.DeInit();
			item.transform.SetParent(_itemHolder);
			item.gameObject.SetActive(false);
			_freeInstances.Enqueue(item);
		}

		void FillInstances(int initialCapacity) {
			_freeInstances = new Queue<PoolItem>(initialCapacity);
			for ( int i = 0; i < initialCapacity; i++ ) {
				_freeInstances.Enqueue(CreateItem());
				
			}
		}

		PoolItem CreateItem() {
			var newObj = Object.Instantiate(_prefab.gameObject, _itemHolder, false);
			var comp = newObj.GetComponent<PoolItem>();
			newObj.SetActive(false);
			return comp;
		}
	}
}

#endif