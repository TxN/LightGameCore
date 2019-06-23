using UnityEngine;

public sealed class TimedDestroy : MonoBehaviour {
	public void Activate(float delay) {
		Invoke("Kill", delay);
	}

	void Kill() {
		Destroy(this.gameObject);
	}
}
