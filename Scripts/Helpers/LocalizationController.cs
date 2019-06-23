using System.Collections.Generic;
using System.Xml;
using UnityEngine;

using EventSys;
using Utils.Xml;

public sealed class LocalizationController : MonoSingleton<LocalizationController> {

	public const SystemLanguage DefaultLanguage   = SystemLanguage.English;
	public const string         LocFileFormatPath = "Configs/Locale_{0}";

	Dictionary<string, string> _currentLocale = new Dictionary<string, string>();

	public static readonly List<SystemLanguage> SupportedLanguages = new List<SystemLanguage> {
		SystemLanguage.English,
		SystemLanguage.Russian,
	};

	public static SystemLanguage FixLanguage(SystemLanguage lang) {
		return SupportedLanguages.Contains(lang) ? lang : DefaultLanguage;
	}

	public SystemLanguage CurrentLanguage {
		get {
			var val = PlayerPrefs.GetString("Locale");
			if ( string.IsNullOrEmpty(val) ) {
				CurrentLanguage = FixLanguage(Application.systemLanguage);
			}
			var result = SystemLanguage.English;
			System.Enum.TryParse(val, out result);
			return result;
		}

		private set {
			PlayerPrefs.SetString("Locale", value.ToString());
		}
	}

	protected override void Awake() {
		base.Awake();
		Load();
		DontDestroyOnLoad(gameObject);
	}

	public string Translate(string id) {
		_currentLocale.TryGetValue(id, out string result);
		return string.IsNullOrEmpty(result) ? string.Format("!{0}!", id.Substring(id.LastIndexOf('.') + 1)) : result;
	}

	public void ChangeLanguage(SystemLanguage language) {
		CurrentLanguage = language;
		Load();
		EventManager.Fire(new Event_LanguageChanged() { Language = language });
	}

	void Load() {
		if ( _currentLocale != null ) {
			_currentLocale.Clear();
		} else {
			_currentLocale = new Dictionary<string, string>(512);
		}
		var loc = XmlUtils.LoadXmlDocumentFromAssets(string.Format(LocFileFormatPath, CurrentLanguage));
		if ( loc == null ) {
			Debug.LogWarningFormat("LocalizationController: Cannnot load locale {0}", CurrentLanguage);
			loc = XmlUtils.LoadXmlDocumentFromAssets(string.Format(LocFileFormatPath, DefaultLanguage));
		}
		if ( loc == null ) {
			Debug.LogError("LocalizationController: Cannnot load default locale.");
			return;
		}

		var root = loc.SelectSingleNode("root");
		foreach ( XmlNode item in root ) {
			LoadTranslationNode(item, string.Empty, _currentLocale);
		}
		Debug.LogFormat("Locale load finished. Loaded {0} elements.", _currentLocale.Count);
	}

	static void LoadTranslationNode(XmlNode node, string prefix, IDictionary<string, string> dict) {
		var text       = node.GetAttrValue("text", string.Empty);
		var new_prefix = string.IsNullOrEmpty(prefix)
			? node.Name
			: string.Format("{0}.{1}", prefix, node.Name);
		if ( !string.IsNullOrEmpty(text) ) {
			if ( !dict.ContainsKey(new_prefix) ) {
				dict.Add(new_prefix, text);
			} else {
				dict[new_prefix] = text;
				Debug.LogErrorFormat("Duplicate entries in loc file for '{0}'", new_prefix);
			}
		}
		foreach ( XmlNode child in node ) {
			LoadTranslationNode(child, new_prefix, dict);
		}
	}
}

public struct Event_LanguageChanged { public SystemLanguage Language; }
