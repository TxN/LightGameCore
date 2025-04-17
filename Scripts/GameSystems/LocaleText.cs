#if UNITY_2017_1_OR_NEWER
using UnityEngine;
using UnityEngine.UI;

using SMGCore.EventSys;

using TMPro;

namespace SMGCore {
	[DisallowMultipleComponent]
	public sealed class LocaleText : MonoBehaviour {
		const float IncreasedSymbolSpacing = 5f;
		public string Id = string.Empty;

		public SystemLanguage EditorLanguage = SystemLanguage.Russian;

		TMP_Text _tmpComponent = null;
		Text _textComponent = null;

		float _defaultSymbolSpacing = 0f;

		void Awake() {
			_tmpComponent = GetComponent<TMP_Text>();
			_textComponent = GetComponent<Text>();
			if ( _tmpComponent ) {
				_defaultSymbolSpacing = _tmpComponent.characterSpacing;
			}
		}

		void Start() {
			UpdateText();
			EventManager.Subscribe<Event_LanguageChanged>(this, OnLanguageChanged);
		}


		void OnDestroy() {
			EventManager.Unsubscribe<Event_LanguageChanged>(OnLanguageChanged);
		}

		void OnLanguageChanged(Event_LanguageChanged e) {
			UpdateText();
		}

		public void UpdateText() {
			if ( string.IsNullOrEmpty(Id) ) {
				return;
			}
			var currentLanguage = LocalizationController.Instance.CurrentLanguage;
			if ( LocalizationController.IsHieroglyphicLanguage(currentLanguage) ) {
				_tmpComponent.characterSpacing = Mathf.Max(_defaultSymbolSpacing, IncreasedSymbolSpacing);
			} else {
				_tmpComponent.characterSpacing = _defaultSymbolSpacing;
			}
			if ( _tmpComponent ) {
				_tmpComponent.text = LocalizationController.Instance.Translate(Id);
			}
			if ( _textComponent ) {
				_textComponent.text = LocalizationController.Instance.Translate(Id);
			}
		}

		public void UpdateText(string id) {
			Id = id;
			if ( string.IsNullOrEmpty(id) ) {
				if ( _tmpComponent ) {
					_tmpComponent.text = LocalizationController.Instance.Translate(Id);
					_tmpComponent.characterSpacing = _defaultSymbolSpacing;
				}
				if ( _textComponent ) {
					_textComponent.text = LocalizationController.Instance.Translate(Id);
				}
			} else {
				UpdateText();
			}
		}

#if ODIN_INSPECTOR
		[Sirenix.OdinInspector.Button]
#endif
		public void UpdateValueInCurrentLocFile() {
			if ( string.IsNullOrEmpty(Id) ) {
				return;
			}
			string newText = null;
			if ( GetComponent<TMP_Text>() ) {
				newText = GetComponent<TMP_Text>().text;
			} else if ( GetComponent<Text>() ) {
				newText = GetComponent<Text>().text;
			}
			if ( newText == null ) {
				return;
			}
			LocalizationController.UpdateEntry(Id, newText, EditorLanguage, true);
			Debug.Log("Updated");
		}

#if ODIN_INSPECTOR
		[Sirenix.OdinInspector.Button]
#endif
		public void LoadValueFromLocFile() {
			var text = LocalizationController.GetEntryFromFile(Id, EditorLanguage);
			if ( string.IsNullOrEmpty(text) ) {
				return;
			}
			var tmp_text = GetComponent<TMP_Text>();
			if ( tmp_text ) {
				tmp_text.text = text;
			}
			var textComponent = GetComponent<Text>();
			if ( textComponent ) {
				textComponent.text = text;
			}
		}
		
	}
}
#endif