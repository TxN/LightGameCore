using UnityEngine;
using UnityEngine.UI;

using EventSys;

using TMPro;

public sealed class LocaleText : MonoBehaviour {
	public string Id = string.Empty;

	TMP_Text _tmpComponent  = null;
	Text     _textComponent = null;

	void Awake() {
		_tmpComponent  = GetComponent<TMP_Text>();
		_textComponent = GetComponent<Text>();
	}

	void Start() {
		UpdateText();
	}

	void OnEnable() {
		EventManager.Subscribe <Event_LanguageChanged>(this, OnLanguageChanged);
	}

	void OnDisable() {
		EventManager.Unsubscribe<Event_LanguageChanged>(OnLanguageChanged);
	}

	void OnLanguageChanged(Event_LanguageChanged e) {
		UpdateText();
	}

	public void UpdateText() {
		if ( string.IsNullOrEmpty(Id) ) {
			return;
		}
		if ( _tmpComponent ) {
			_tmpComponent.text = LocalizationController.Instance.Translate(Id);
		}
		if ( _textComponent ) {
			_textComponent.text = LocalizationController.Instance.Translate(Id);
		}
	}
}
