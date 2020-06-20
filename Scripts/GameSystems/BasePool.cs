namespace SMGCore {
	public abstract class BasePool {
		public abstract PoolItem GetGeneric();
		public abstract void ReturnGeneric(PoolItem item);
		public abstract PoolItem GetNewGeneric();
	}
}