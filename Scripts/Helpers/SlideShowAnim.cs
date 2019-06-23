
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public sealed class SlideShowAnim : MonoBehaviour {
	public List<Sprite> Sprites = new List<Sprite>();
	public List<float>  Delays  = new List<float>();
	public bool         Loop    = true;

	float          _time          = 0f;
	float          _nextFrameTime = 0f;
	int            _frameIndex    = 0;
	SpriteRenderer _renderer      = null;
	Image          _image         = null;

	void Start() {
		_renderer = GetComponent<SpriteRenderer>();
		_image = GetComponent<Image>();
		_nextFrameTime = Time.time + Delays[_frameIndex];
		if ( _renderer ) {
			_renderer.sprite = Sprites[0];
		}
		if ( _image ) {
			_image.sprite = Sprites[0];
		}

		_frameIndex++;
	}

	public void Play() {
		_frameIndex = 0;
	}

	void Update() {
		_time += Time.deltaTime;
		if ( Time.time > _nextFrameTime ) {
			if ( _renderer ) {
				_renderer.sprite = Sprites[_frameIndex];
			}
			if ( _image ) {
				_image.sprite = Sprites[_frameIndex];
			}
			_nextFrameTime = Time.time + Delays[_frameIndex];
			_frameIndex++;
			if ( _frameIndex >= Sprites.Count && Loop ) {
				_frameIndex = 0;
			} else if ( _frameIndex >= Sprites.Count ) {
				_frameIndex = Sprites.Count - 1;
			}
		}
	}
}
