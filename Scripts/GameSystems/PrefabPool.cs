#if UNITY_2017_1_OR_NEWER
using System.Collections.Generic;
using UnityEngine;

namespace SMGCore {
	public class PrefabPool<T> : BasePool where T : PoolItem {

		protected string PresenterPrefabPath = "";

		protected GameObject _prefab = null;
		Queue<T> _readyObjects = new Queue<T>();

		public virtual void Init() {
			_prefab = Resources.Load<GameObject>(PresenterPrefabPath);
			if ( _prefab == null ) {
				Debug.LogErrorFormat("PrefabPool: cannot load prefab from resources. Path: {0}", PresenterPrefabPath);
			}
		}

		public virtual T Get() {
			if ( _readyObjects.Count > 0 ) {
				var item = _readyObjects.Dequeue();
				item.gameObject.SetActive(true);
				return item;
			}
			return GetNew();
		}

		public virtual void Return(T item) {
			item.DeInit();
			item.gameObject.SetActive(false);
			_readyObjects.Enqueue(item);
		}

		protected virtual T GetNew() {
			var instance = Object.Instantiate(_prefab);
			var c = instance.GetComponent<T>();
			c.gameObject.SetActive(true);
			return c;
		}

		public override PoolItem GetGeneric() {
			return Get();
		}

		public override void ReturnGeneric(PoolItem item) {
			Return((T)item);
		}

		public override PoolItem GetNewGeneric() {
			return GetNew();
		}
	}

	public interface IPoolItem {
		void DeInit();
	}
}
#endif