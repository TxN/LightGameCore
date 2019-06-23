public sealed class ScenePersistence : MonoSingleton<ScenePersistence> {
    public PersistentDataHolder Data { get; private set; } = new PersistentDataHolder();

	protected override void Awake() {
		base.Awake();
		DontDestroyOnLoad(gameObject);
	}

	public void ClearData() {
		Data = new PersistentDataHolder();
	}
}
