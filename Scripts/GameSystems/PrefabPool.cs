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
				return _readyObjects.Dequeue();
			}
			return GetNew();
		}

		public virtual void Return(T item) {
			item.DeInit();
			_readyObjects.Enqueue(item);
		}

		protected virtual T GetNew() {
			var instance = Object.Instantiate(_prefab);
			var c = instance.GetComponent<T>();
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