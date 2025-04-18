#if UNITY_2017_1_OR_NEWER
using System.Collections.Generic;
using System.Xml;

using UnityEngine;

using SMGCore.EventSys;
using SMGCore.Utils.Xml;
using System.IO;
using System.Linq;

namespace SMGCore {
	public sealed class LocalizationController : MonoSingleton<LocalizationController> {

		public const SystemLanguage DefaultLanguage   = SystemLanguage.English;
		public const string         LocFileFormatPath = "Configs/Locale_{0}";
		public const string 		StreamingAssetsPath = "Configs/";
		public const string         EmptyLocString    = "!{0}!";

		Dictionary<string, TranslationNode> _currentLocale = new Dictionary<string, TranslationNode>();

		public static readonly List<SystemLanguage> SupportedLanguages = new List<SystemLanguage> {
		SystemLanguage.English,
		SystemLanguage.Russian,
		SystemLanguage.Chinese,
		};
		bool _isInitialized = false;

		Dictionary<string, SystemLanguage> _allLanguages = new Dictionary<string, SystemLanguage>();

		List<SystemLanguage> _streamingAssetsLanguages = new List<SystemLanguage>();

		public static bool LocaleLoadFailed { get; private set; }

		public static bool IsHieroglyphicLanguage(SystemLanguage language) {
			return language == SystemLanguage.Chinese || language == SystemLanguage.Japanese || language == SystemLanguage.Korean || language == SystemLanguage.ChineseSimplified || language == SystemLanguage.ChineseTraditional;
		}

		public HashSet<char> GetAllSymbolsFromCurrentLocale() {
			var symbols = new HashSet<char>();
			foreach ( var kv in _currentLocale ) {
				if ( string.IsNullOrEmpty(kv.Value.Text) ) {
					continue;
				}
				foreach ( var c in kv.Value.Text ) {
					symbols.Add(c);
				}
			}
			return symbols;
		}

		SystemLanguage FixLanguage(SystemLanguage lang) {
			if ( _streamingAssetsLanguages.Contains(lang) ) {
				return lang;
			}
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
				Initialize();
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
			Initialize();
			Load();
			DontDestroyOnLoad(gameObject);
		}

		public void Initialize() {
			if ( _isInitialized ) {
				return;
			}
			_isInitialized = true;
			_allLanguages.Clear();
			foreach ( var lang in System.Enum.GetValues(typeof(SystemLanguage)) ) {
				if ( !_allLanguages.ContainsKey(lang.ToString()) ) {
					_allLanguages.Add(lang.ToString(), (SystemLanguage)lang);
				}
			}
			var streamingAssetsLocalePath = Path.Combine(Application.streamingAssetsPath, StreamingAssetsPath);
			if ( !Directory.Exists(streamingAssetsLocalePath) ) {
				return;
			}
			var files = GetAllLanguageFilesFromStreamingAssets();
			foreach ( var file in files ) {
				var lang = file.Substring(file.LastIndexOf('_') + 1, file.LastIndexOf('.') - file.LastIndexOf('_') - 1);
				if ( _allLanguages.ContainsKey(lang) ) {
					Debug.LogFormat("LocalizationController: Found language file {0}", lang);
					_streamingAssetsLanguages.Add(_allLanguages[lang]);
				} else {
					Debug.LogWarningFormat("LocalizationController: Found language file {0} but it is not supported", lang);
				}
			}
		}

		List<string> GetAllLanguageFilesFromStreamingAssets() {
			var files = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, StreamingAssetsPath), "Locale_*.xml", SearchOption.TopDirectoryOnly);
			return files.ToList();
		}

		public List<SystemLanguage> GetSupportedLanguages() {
			Initialize();
			var result = new List<SystemLanguage>();
			foreach ( var lang in SupportedLanguages ) {
				result.Add(lang);
			}
			foreach ( var lang in _streamingAssetsLanguages ) {
				if ( !result.Contains(lang) ) {
					result.Add(lang);
				}
			}
			return result;
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
			Initialize();
			if ( _currentLocale != null ) {
				_currentLocale.Clear();
			} else {
				_currentLocale = new Dictionary<string, TranslationNode>();
			}
			var hasStreamingAssetsLanguage = _streamingAssetsLanguages.Contains(CurrentLanguage);
			XmlDocument loc;
			if ( hasStreamingAssetsLanguage ) {
				loc = XmlUtils.LoadXmlDocumentFromStreamingAssets(string.Format(LocFileFormatPath, CurrentLanguage));
			} else {
				loc = XmlUtils.LoadXmlDocumentFromAssets(string.Format(LocFileFormatPath, CurrentLanguage));
			}
			
			if ( loc == null ) {				
				//Пробуем второй раз загрузить локаль (по какой-то неведомой причине, иногда с первого раза локали могут не подтянуться)
				if ( hasStreamingAssetsLanguage ) {
					loc = XmlUtils.LoadXmlDocumentFromStreamingAssets(string.Format(LocFileFormatPath, CurrentLanguage));
					if ( loc == null ) {
						Debug.LogErrorFormat("LocalizationController: Cannnot load locale {0} from streaming assets. Trying to fallback to resources", CurrentLanguage);
						if ( SupportedLanguages.Contains(CurrentLanguage)) {
							loc = XmlUtils.LoadXmlDocumentFromAssets(string.Format(LocFileFormatPath, CurrentLanguage));
						}
					}
				} else {
					loc = XmlUtils.LoadXmlDocumentFromAssets(string.Format(LocFileFormatPath, CurrentLanguage));
				}
				
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

		public static void CreateNewLocaleEntry(string path, string value, XmlDocument loc) {
			if (string.IsNullOrEmpty(path) || loc == null) {
				Debug.LogError("LocalizationController.CreateNewLocaleEntry: path or document is null");
				return;
			}

			if (!path.StartsWith("root.")) {
				path = "root." + path;
			}

			// Проверяем, существует ли уже такая нода
			var existingNode = loc.SelectSingleNode(path.Replace('.', '/'));
			if (existingNode != null) {
				Debug.LogWarning($"LocalizationController.CreateNewLocaleEntry: node {path} already exists, updating value");
				var attr = existingNode.Attributes["text"];
				if (attr != null) {
					attr.Value = value;
					return;
				}
			}

			var pathParts = path.Split('.');
			XmlNode currentNode = loc.DocumentElement; // root node

			// Создаем все отсутствующие родительские ноды
			for (int i = 1; i < pathParts.Length - 1; i++) {
				var nextNode = currentNode.SelectSingleNode(pathParts[i]);
				if (nextNode == null) {
					nextNode = loc.CreateElement(pathParts[i]);
					currentNode.AppendChild(nextNode);
				}
				currentNode = nextNode;
			}

			// Создаем конечную ноду
			var nodeName = pathParts[pathParts.Length - 1];
			var element = loc.CreateElement(nodeName);
			var textAttr = loc.CreateAttribute("text");
			textAttr.Value = value;
			element.Attributes.Append(textAttr);
			currentNode.AppendChild(element);
		}

		public static void UpdateEntry(string path, string newValue, SystemLanguage language, bool create = false) {
			var filePath = string.Format(LocFileFormatPath, language);
			var loc = XmlUtils.LoadXmlDocumentFromAssets(filePath);
			if ( loc == null ) {
				Debug.LogError($"LocalizationController.UpdateEntry: Cannot load locale file for {language}");
				return;
			}
			var node = loc.SelectSingleNode("root/" + path.Replace('.', '/'));
			if ( node == null ) {
				if ( create ) {
					CreateNewLocaleEntry(path, newValue, loc);
#if UNITY_EDITOR
					loc.Save("Assets/Resources/" + filePath + ".xml");
					UnityEditor.AssetDatabase.Refresh();
#endif
					return;
				}
				Debug.LogError($"LocalizationController.UpdateEntry: Cannot find node with path {path} in locale {language}");
				return;
			}
			var attr = node.Attributes["text"];
			if ( attr == null ) {
				Debug.LogError($"LocalizationController.UpdateEntry: Cannot find text attribute in node with path {path} in locale {language}");
				return;
			}
			attr.Value = newValue;
#if UNITY_EDITOR
			loc.Save("Assets/Resources/" + filePath + ".xml");
			UnityEditor.AssetDatabase.Refresh();
#endif
		}

		public static string GetEntryFromFile( string path, SystemLanguage language) {
			var filePath = string.Format(LocFileFormatPath, language);
			var loc = XmlUtils.LoadXmlDocumentFromAssets(filePath);
			if ( loc == null ) {
				Debug.LogError($"LocalizationController.GetEntryFromFile: Cannot load locale file for {language}");
				return "!!!";
			}
			var node = loc.SelectSingleNode("root/" + path.Replace('.', '/'));
			if ( node == null ) {
				Debug.LogError($"LocalizationController.GetEntryFromFile: Cannot find node with path {path} in locale {language}");
				return "!!!";
			}
			return node.GetAttrValue("text", "!!!");
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
#endif