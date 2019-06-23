using UnityEngine;

public class TextureScroll : MonoBehaviour {
	public Vector2 Scroll = new Vector2(0.05f, 0.05f);
	Vector2 _offset = new Vector2(0f, 0f);

	Renderer _renderer = null;

	private void Awake() {
		_renderer = GetComponent<Renderer>();
	}

	void Update() {
		_offset += Scroll * Time.deltaTime;
		_renderer.material.SetTextureOffset("_MainTex", _offset);
	}
}
