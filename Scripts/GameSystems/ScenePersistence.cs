namespace SMGCore {
	public sealed class ScenePersistence : MonoSingleton<ScenePersistence> {
		public PersistentDataHolder Data { get; private set; } = null;

		protected override void Awake() {
			base.Awake();
			DontDestroyOnLoad(gameObject);
		}
		
		public void SetupHolder(PersistentDataHolder data) {
			Data = data;
		}

		public static T Get<T>() where T: PersistentDataHolder {
			if ( Instance.Data is T ) {
				return (T)Instance.Data;
			}
			return null;
		}
	}
}
