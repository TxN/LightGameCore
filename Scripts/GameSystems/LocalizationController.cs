using System.Collections.Generic;
using System.Xml;

using UnityEngine;

using SMGCore.EventSys;
using SMGCore.Utils.Xml;

namespace SMGCore {
	public sealed class LocalizationController : MonoSingleton<LocalizationController> {

		public const SystemLanguage DefaultLanguage   = SystemLanguage.English;
		public const string         LocFileFormatPath = "Configs/Locale_{0}";
		public const string         EmptyLocString    = "!{0}!";

		Dictionary<string, TranslationNode> _currentLocale = new Dictionary<string, TranslationNode>();

		public static readonly List<SystemLanguage> SupportedLanguages = new List<SystemLanguage> {
		SystemLanguage.English,
		SystemLanguage.Russian,
	};

		public static bool LocaleLoadFailed { get; private set; }

		public static SystemLanguage FixLanguage(SystemLanguage lang) {
			return SupportedLanguages.Contains(lang) ? lang : DefaultLanguage;
		}

		public static string Trans(string id) {
			return Instance.Translate(id);
		}

		public static string TransFormat(string id, params string[] args) {
			return Instance.TranslateFormat(id, args);
		}

		public static bool CanTrans(string id) {
			return Instance.CanTranslate(id);
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
			if ( _instance != this ) {
				Debug.LogWarning($"Singleton {GetType().Name}, duplicate instance found");
				return;
			}
			Load();
			DontDestroyOnLoad(gameObject);
		}

		public string Translate(string id) {
			_currentLocale.TryGetValue(id, out TranslationNode result);
			if ( result == null ) {
				return string.Format(EmptyLocString, id.Substring(id.LastIndexOf('.') + 1));
			}

			return string.IsNullOrEmpty(result.Text) ? string.Format(EmptyLocString, id.Substring(id.LastIndexOf('.') + 1)) : result.Text;
		}

		public List<string> GetChildIds(string startsWith) {
			var list = new List<string>();

			foreach ( var kv in _currentLocale ) {
				if ( kv.Key.StartsWith(startsWith) ) {
					list.Add(kv.Key);
				}
			}

			return list;
		}

		public bool CanTranslate(string id) {
			if ( string.IsNullOrEmpty(id) ) {
				return false;
			}
			return _currentLocale.ContainsKey(id);
		}

		public string TranslateFormat(string id, params string[] args) {
			var str = Translate(id);
			return string.Format(str, args);
		}

		public TranslationNode GetTranslationNode(string id) {
			_currentLocale.TryGetValue(id, out TranslationNode result);
			return result;
		}

		public TranslationNode GetSameLevelNode(string sourceId, string targetName) {
			var basePath = sourceId.Substring(0, sourceId.LastIndexOf('.'));
			return GetTranslationNode($"{basePath}.{targetName}");
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
				_currentLocale = new Dictionary<string, TranslationNode>();
			}
			var loc = XmlUtils.LoadXmlDocumentFromAssets(string.Format(LocFileFormatPath, CurrentLanguage));
			if ( loc == null ) {
				
				//Пробуем второй раз загрузить локаль (по какой-то неведомой причине, иногда с первого раза локали могут не подтянуться)
				loc = XmlUtils.LoadXmlDocumentFromAssets(string.Format(LocFileFormatPath, CurrentLanguage));
				if ( loc == null ) {
					//Пробуем на худой конец подтянуть резервную
					loc = XmlUtils.LoadXmlDocumentFromAssets(string.Format(LocFileFormatPath, DefaultLanguage));
					Debug.LogWarningFormat("LocalizationController: Cannnot load locale {0}", CurrentLanguage);
					LocaleLoadFailed = true;
				} else {
					Debug.LogWarningFormat("LocalizationController: Locale {0} loaded after second attempt", CurrentLanguage);
				}				
			}
			if ( loc == null ) {
				LocaleLoadFailed = true;
				Debug.LogError("LocalizationController: Cannnot load default locale.");
				return;
			}

			var root = loc.SelectSingleNode("root");
			foreach ( XmlNode item in root ) {
				LoadTranslationNode(item, string.Empty, _currentLocale);
			}
			Debug.LogFormat("Locale load finished. Loaded {0} elements.", _currentLocale.Count);
			LocaleLoadFailed = false;
		}

		static void LoadTranslationNode(XmlNode node, string prefix, IDictionary<string, TranslationNode> dict) {
			var text = node.GetAttrValue("text", string.Empty);
			var new_prefix = string.IsNullOrEmpty(prefix)
				? node.Name
				: string.Format("{0}.{1}", prefix, node.Name);
			if ( !string.IsNullOrEmpty(text) ) {
				var n = LoadNode(node, new_prefix);
				if ( !dict.ContainsKey(new_prefix) ) {
					dict.Add(new_prefix, n);
				} else {
					dict[new_prefix] = n;
					Debug.LogErrorFormat("Duplicate entries in loc file for '{0}'", new_prefix);
				}
			}
			foreach ( XmlNode child in node ) {
				LoadTranslationNode(child, new_prefix, dict);
			}
		}

		static TranslationNode LoadNode(XmlNode node, string path) {
			var n = new TranslationNode {
				Path = path,
				Text = node.GetAttrValue("text", string.Empty),
				Duration = node.GetAttrValue("duration", 0f),
				NextLine = node.GetAttrValue("next", null)
			};
			return n;
		}
	}

	public sealed class TranslationNode {
		public string Path;
		public string Text;
		public string NextLine;
		public float  Duration = 3f;
	}

	public struct Event_LanguageChanged { public SystemLanguage Language; }
}
