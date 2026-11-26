#if UNITY_2017_1_OR_NEWER
using UnityEngine;
using UnityEngine.UI;

using SMGCore.EventSys;

using TMPro;

namespace SMGCore {
	[DisallowMultipleComponent]
	public sealed class LocaleText : MonoBehaviour {
		const float IncreasedSymbolSpacing = 5f;
		
		[LocKeyField]
		public string Id = string.Empty;

		public SystemLanguage EditorLanguage = SystemLanguage.Russian;

		public bool ForceRebuildLayout = false;

		TMP_Text _tmpComponent = null;
		Text _textComponent = null;

		float _defaultSymbolSpacing = 0f;

		RectTransform _rectTransform = null;

		public void SetText(string text) {
			if ( _tmpComponent ) {
				_tmpComponent.text = text;
			}
			if ( _textComponent ) {
				_textComponent.text = text;
			}
		}

		void Awake() {
			_tmpComponent = GetComponent<TMP_Text>();
			_textComponent = GetComponent<Text>();
			_rectTransform = GetComponent<RectTransform>();
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
			var loadedLanguage = LocalizationController.LoadedLanguage;
			var translated = LocalizationController.Trans(Id);
			if ( _tmpComponent ) {
				_tmpComponent.SetText(translated);
				if ( LocalizationController.IsHieroglyphicLanguage(loadedLanguage) ) {
					_tmpComponent.characterSpacing = Mathf.Max(_defaultSymbolSpacing, IncreasedSymbolSpacing);
				} else {
					_tmpComponent.characterSpacing = _defaultSymbolSpacing;
				}
				_tmpComponent.ForceMeshUpdate();
			}
			if ( _textComponent ) {
				_textComponent.text = translated;
			}
			if ( ForceRebuildLayout && _rectTransform ) {
				LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
			}
		}

		public void UpdateText(string id) {
			Id = id;
			if ( string.IsNullOrEmpty(id) ) {
				if ( _tmpComponent ) {
					_tmpComponent.text = string.Empty;
					_tmpComponent.characterSpacing = _defaultSymbolSpacing;
				}
				if ( _textComponent ) {
					_textComponent.text = string.Empty;
				}
				if ( ForceRebuildLayout && _rectTransform ) {
					LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
				}
			} else {
				UpdateText();
			}
		}

#if UNITY_EDITOR
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
			LocalizationXmlUtility.UpdateEntry(Id, newText, EditorLanguage, true);
			Debug.Log("Updated");
		}

#if ODIN_INSPECTOR
		[Sirenix.OdinInspector.Button]
#endif
		public void LoadValueFromLocFile() {
			var text = LocalizationXmlUtility.GetEntryFromFile(Id, EditorLanguage);
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
#endif
		
	}
}
#endif