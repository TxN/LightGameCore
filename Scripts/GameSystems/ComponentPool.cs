#if UNITY_2017_1_OR_NEWER
using UnityEngine;

namespace SMGCore {
	public sealed class ComponentPool<T> : PrefabPool<T> where T : PoolItem {
		public T Template;

		public override void Init() {
			if ( !Template ) {
				Debug.LogError("ComponentPool: template object is null");
				return;
			}
			_prefab = Template.gameObject;
			_prefab.SetActive(false);
		}
	}
}
#endif