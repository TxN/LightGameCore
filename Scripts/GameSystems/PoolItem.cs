using UnityEngine;

namespace SMGCore {
	public abstract class PoolItem : MonoBehaviour, IPoolItem {
		public abstract void DeInit();
	}
}

