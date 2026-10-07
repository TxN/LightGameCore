#if UNITY_2017_1_OR_NEWER
using UnityEngine;

namespace SMGCore {
	public abstract class PoolItem : MonoBehaviour, IPoolItem {
		internal bool IsInPool;

		public abstract void DeInit();
	}
}

#endif