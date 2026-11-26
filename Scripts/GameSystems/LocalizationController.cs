#if UNITY_2017_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using UnityEngine;

using SMGCore.EventSys;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("SMGCore.Localization.Tests")]
#endif

namespace SMGCore {
	public static class LocalizationController {
		public const string LocFileFormatPath   = "Configs/Locale_{0}";
		public const string StreamingAssetsPath = "Configs/";
		public const string EmptyLocString      = LocalizationTable.EmptyLocString;
		public const string DraftAttribute      = LocalizationTable.DraftAttribute;

		const string LocalePrefsKey = "Locale";

		static readonly LocalizationTable _baseTable = new LocalizationTable();
		static readonly Dictionary<string, string> _dynamicEntries = new Dictionary<string, string>(StringComparer.Ordinal);
		static readonly List<SystemLanguage> _streamingAssetsLanguages = new List<SystemLanguage>();
		static readonly List<SystemLanguage> _supportedLanguagesCache = new List<SystemLanguage>();

		static LocalizationConfig _config;
		static LocalizationConfig _overrideConfig;
		static bool _environmentInitialized;
		static bool _languageResolved;
		static bool _isLocaleLoaded;
		static SystemLanguage _currentLanguage = LocalizationConfig.FallbackDefaultLanguage;
		static SystemLanguage _loadedLanguage = LocalizationConfig.FallbackDefaultLanguage;
		static bool _localeLoadFailed;
		static bool _currentLocaleIsDraft;

#if UNITY_EDITOR
		static DateTime _cachedConfigFileWriteUtc = DateTime.MinValue;

		internal static Func<SystemLanguage, string> ResourceXmlLoaderOverride;
		internal static Func<SystemLanguage, string> StreamingXmlLoaderOverride;
		internal static Action<List<SystemLanguage>> StreamingScannerOverride;
#endif

		public static SystemLanguage DefaultLanguage {
			get {
				EnsureConfigLoaded();
				return _config.DefaultLanguage;
			}
		}

		public static IReadOnlyList<SystemLanguage> SupportedLanguages {
			get {
				EnsureConfigLoaded();
				return _config.SupportedLanguages;
			}
		}

		public static SystemLanguage CurrentLanguage {
			get {
				EnsureInitialized();
				return _currentLanguage;
			}
		}

		public static SystemLanguage LoadedLanguage {
			get {
				EnsureLocaleLoaded();
				return _loadedLanguage;
			}
		}

		public static bool LocaleLoadFailed {
			get {
				EnsureLocaleLoaded();
				return _localeLoadFailed;
			}
		}

		/// <summary>
		/// True when the loaded locale file declares itself unfinished: <c>&lt;root draft="true"&gt;</c>.
		/// The UI uses it to warn the player that the translation is a work in progress. An absent
		/// attribute means a final locale, so the files that predate the attribute need no edit.
		/// </summary>
		public static bool CurrentLocaleIsDraft {
			get {
				EnsureLocaleLoaded();
				return _currentLocaleIsDraft;
			}
		}

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		static void InitEditorHooks() {
			UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange change) {
			if ( change == UnityEditor.PlayModeStateChange.ExitingEditMode ||
			     change == UnityEditor.PlayModeStateChange.EnteredEditMode ) {
				Reset();
				LocalizationXmlUtility.ClearEditorCache();
			}
		}
#endif

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void InitRuntimeState() {
#if !UNITY_EDITOR
			Reset();
#endif
		}

		public static void Reset() {
			_config = null;
			_overrideConfig = null;
			_environmentInitialized = false;
			_languageResolved = false;
			_isLocaleLoaded = false;
			_currentLanguage = LocalizationConfig.FallbackDefaultLanguage;
			_loadedLanguage = LocalizationConfig.FallbackDefaultLanguage;
			_baseTable.Clear();
			_dynamicEntries.Clear();
			_streamingAssetsLanguages.Clear();
			_supportedLanguagesCache.Clear();
			_localeLoadFailed = false;
			_currentLocaleIsDraft = false;
#if UNITY_EDITOR
			_cachedConfigFileWriteUtc = DateTime.MinValue;
			ResourceXmlLoaderOverride = null;
			StreamingXmlLoaderOverride = null;
			StreamingScannerOverride = null;
#endif
		}

		public static void Configure(LocalizationConfig config) {
			_overrideConfig = config;
			_config = config;
			_environmentInitialized = false;
			_languageResolved = false;
			_isLocaleLoaded = false;
			_baseTable.Clear();
			_streamingAssetsLanguages.Clear();
			_supportedLanguagesCache.Clear();
			_localeLoadFailed = false;
			_currentLocaleIsDraft = false;
#if UNITY_EDITOR
			_cachedConfigFileWriteUtc = DateTime.MinValue;
#endif
		}

		public static bool IsHieroglyphicLanguage(SystemLanguage language) {
			return language == SystemLanguage.Chinese ||
			       language == SystemLanguage.Japanese ||
			       language == SystemLanguage.Korean ||
			       language == SystemLanguage.ChineseSimplified ||
			       language == SystemLanguage.ChineseTraditional;
		}

		public static void Initialize() {
			EnsureLocaleLoaded();
		}

		public static IReadOnlyList<SystemLanguage> GetSupportedLanguages() {
			EnsureEnvironmentInitialized();
			return _supportedLanguagesCache;
		}

		public static string Trans(string id) {
			if ( string.IsNullOrEmpty(id) ) {
				return string.Empty;
			}
			EnsureLocaleLoaded();
			if ( _dynamicEntries.TryGetValue(id, out var dynamicText) ) {
				return dynamicText;
			}
			return _baseTable.GetOrMissing(id);
		}

		public static bool TryTrans(string id, out string text) {
			if ( string.IsNullOrEmpty(id) ) {
				text = null;
				return false;
			}
			EnsureLocaleLoaded();
			if ( _dynamicEntries.TryGetValue(id, out text) ) {
				return true;
			}
			if ( _baseTable.TryGet(id, out text) && !string.IsNullOrEmpty(text) ) {
				return true;
			}
			text = null;
			return false;
		}

		public static bool CanTrans(string id) {
			if ( string.IsNullOrEmpty(id) ) {
				return false;
			}
			EnsureLocaleLoaded();
			return _dynamicEntries.ContainsKey(id) || _baseTable.ContainsKey(id);
		}

		public static string TransFormat(string id, string arg0) {
			var str = Trans(id);
			try {
				return string.Format(str, arg0);
			} catch ( FormatException e ) {
				LogFormatError(id, e);
				return str;
			}
		}

		public static string TransFormat(string id, string arg0, string arg1) {
			var str = Trans(id);
			try {
				return string.Format(str, arg0, arg1);
			} catch ( FormatException e ) {
				LogFormatError(id, e);
				return str;
			}
		}

		public static string TransFormat(string id, string arg0, string arg1, string arg2) {
			var str = Trans(id);
			try {
				return string.Format(str, arg0, arg1, arg2);
			} catch ( FormatException e ) {
				LogFormatError(id, e);
				return str;
			}
		}

		public static string TransFormat(string id, params string[] args) {
			var str = Trans(id);
			if ( args == null || args.Length == 0 ) {
				return str;
			}
			try {
				return string.Format(str, args);
			} catch ( FormatException e ) {
				LogFormatError(id, e);
				return str;
			}
		}

		public static void GetChildIds(string startsWith, List<string> output) {
			if ( output == null ) {
				return;
			}
			EnsureLocaleLoaded();
			_baseTable.GetChildIds(startsWith, output);
			if ( _dynamicEntries.Count == 0 ) {
				return;
			}
			var prefix = startsWith ?? string.Empty;
			foreach ( var kv in _dynamicEntries ) {
				if ( kv.Key.StartsWith(prefix, StringComparison.Ordinal) && !_baseTable.ContainsKey(kv.Key) ) {
					output.Add(kv.Key);
				}
			}
		}

		public static List<string> GetChildIds(string startsWith) {
			var list = new List<string>();
			GetChildIds(startsWith, list);
			return list;
		}

		public static void CollectSymbolsFromCurrentLocale(HashSet<char> output) {
			if ( output == null ) {
				return;
			}
			EnsureLocaleLoaded();
			_baseTable.CollectSymbols(output);
			if ( _dynamicEntries.Count == 0 ) {
				return;
			}
			foreach ( var kv in _dynamicEntries ) {
				var text = kv.Value;
				if ( string.IsNullOrEmpty(text) ) {
					continue;
				}
				for ( var i = 0; i < text.Length; i++ ) {
					output.Add(text[i]);
				}
			}
		}

		public static HashSet<char> GetAllSymbolsFromCurrentLocale() {
			var symbols = new HashSet<char>();
			CollectSymbolsFromCurrentLocale(symbols);
			return symbols;
		}

		public static void AddDynamicTranslation(string id, string text, bool overwrite = false) {
			if ( string.IsNullOrEmpty(id) ) {
				Debug.LogError("LocalizationController.AddDynamicTranslation: id is null or empty");
				return;
			}
			if ( string.IsNullOrEmpty(text) ) {
				if ( overwrite ) {
					_dynamicEntries.Remove(id);
				}
				return;
			}
			if ( !overwrite && _dynamicEntries.ContainsKey(id) ) {
				return;
			}
			_dynamicEntries[id] = text;
		}

		public static void RemoveDynamicTranslation(string id) {
			if ( string.IsNullOrEmpty(id) ) {
				return;
			}
			_dynamicEntries.Remove(id);
		}

		public static void ClearDynamicTranslations() {
			_dynamicEntries.Clear();
		}

		public static void ChangeLanguage(SystemLanguage language) {
			EnsureEnvironmentInitialized();
			var resolved = FixLanguage(language);
			_currentLanguage = resolved;
			_languageResolved = true;
			SaveLanguagePref(resolved);
			Load(resolved);
			EventManager.Fire(new Event_LanguageChanged { Language = _loadedLanguage, IsDraft = _currentLocaleIsDraft });
		}

		public static void Reload() {
			EnsureConfigLoaded();
			_environmentInitialized = true;
			ScanStreamingAssetsLanguages();
			RebuildSupportedLanguagesCache();
			if ( !_languageResolved ) {
				ResolveInitialLanguage();
			} else {
				_currentLanguage = FixLanguage(_currentLanguage);
			}
			Load(_currentLanguage);
			EventManager.Fire(new Event_LanguageChanged { Language = _loadedLanguage, IsDraft = _currentLocaleIsDraft });
		}

		static void EnsureConfigLoaded() {
			if ( _overrideConfig != null ) {
				_config = _overrideConfig;
				return;
			}

#if UNITY_EDITOR
			if ( _config != null && !Application.isPlaying ) {
				var assetPath = "Assets/Resources/" + LocalizationConfig.DefaultConfigResourcePath + ".xml";
				var currentWriteUtc = File.Exists(assetPath) ? File.GetLastWriteTimeUtc(assetPath) : DateTime.MinValue;
				if ( currentWriteUtc != _cachedConfigFileWriteUtc ) {
					_config = null;
					_environmentInitialized = false;
				}
			}
#endif

			if ( _config != null ) {
				return;
			}

#if UNITY_EDITOR
			var configAssetPath = "Assets/Resources/" + LocalizationConfig.DefaultConfigResourcePath + ".xml";
			_cachedConfigFileWriteUtc = File.Exists(configAssetPath) ? File.GetLastWriteTimeUtc(configAssetPath) : DateTime.MinValue;
#endif

			var asset = Resources.Load<TextAsset>(LocalizationConfig.DefaultConfigResourcePath);
			if ( asset ) {
				var xmlText = asset.text;
				Resources.UnloadAsset(asset);
				_config = LocalizationConfig.ParseXml(xmlText, out var parsedOk);
				if ( !parsedOk ) {
					Debug.LogWarningFormat("LocalizationController: Failed to parse '{0}', using fallback config.", LocalizationConfig.DefaultConfigResourcePath);
				}
			} else {
				Debug.LogWarningFormat("LocalizationController: Config '{0}' not found in Resources. Using fallback language '{1}'.", LocalizationConfig.DefaultConfigResourcePath, LocalizationConfig.FallbackDefaultLanguage);
				_config = LocalizationConfig.CreateFallback();
			}
		}

		static void EnsureEnvironmentInitialized() {
			EnsureConfigLoaded();
			if ( _environmentInitialized ) {
				return;
			}
			_environmentInitialized = true;

			ScanStreamingAssetsLanguages();
			RebuildSupportedLanguagesCache();
		}

		static void EnsureInitialized() {
			EnsureEnvironmentInitialized();
			if ( _languageResolved ) {
				return;
			}
			ResolveInitialLanguage();
		}

		static void EnsureLocaleLoaded() {
			if ( _isLocaleLoaded ) {
				return;
			}
			EnsureInitialized();
			Load(_currentLanguage);
		}

		static void ScanStreamingAssetsLanguages() {
			_streamingAssetsLanguages.Clear();
#if UNITY_EDITOR
			if ( StreamingScannerOverride != null ) {
				StreamingScannerOverride(_streamingAssetsLanguages);
				return;
			}
#endif
			try {
				var streamingAssetsLocalePath = Path.Combine(Application.streamingAssetsPath, StreamingAssetsPath);
				if ( string.IsNullOrEmpty(streamingAssetsLocalePath) || !Directory.Exists(streamingAssetsLocalePath) ) {
					return;
				}
				var files = Directory.GetFiles(streamingAssetsLocalePath, "Locale_*.xml", SearchOption.TopDirectoryOnly);
				for ( var i = 0; i < files.Length; i++ ) {
					var fileName = Path.GetFileNameWithoutExtension(files[i]);
					if ( string.IsNullOrEmpty(fileName) || !fileName.StartsWith("Locale_", StringComparison.Ordinal) ) {
						continue;
					}
					var langName = fileName.Substring("Locale_".Length);
					if ( LocalizationConfig.TryParseLanguage(langName, out var parsed) ) {
						if ( !_streamingAssetsLanguages.Contains(parsed) ) {
							Debug.LogFormat("LocalizationController: Found language file {0}", langName);
							_streamingAssetsLanguages.Add(parsed);
						}
					} else {
						Debug.LogWarningFormat("LocalizationController: Found language file {0} but it is not supported", langName);
					}
				}
			} catch ( Exception e ) {
				Debug.LogWarningFormat("LocalizationController: Failed to scan StreamingAssets locales: {0}", e.Message);
			}
		}

		static void RebuildSupportedLanguagesCache() {
			_supportedLanguagesCache.Clear();
			var builtIn = _config.SupportedLanguages;
			for ( var i = 0; i < builtIn.Count; i++ ) {
				if ( !_supportedLanguagesCache.Contains(builtIn[i]) ) {
					_supportedLanguagesCache.Add(builtIn[i]);
				}
			}
			for ( var i = 0; i < _streamingAssetsLanguages.Count; i++ ) {
				if ( !_supportedLanguagesCache.Contains(_streamingAssetsLanguages[i]) ) {
					_supportedLanguagesCache.Add(_streamingAssetsLanguages[i]);
				}
			}
		}

		static void ResolveInitialLanguage() {
			_languageResolved = true;
			var saved = PlayerPrefs.GetString(LocalePrefsKey);
			if ( string.IsNullOrEmpty(saved) ) {
				_currentLanguage = FixLanguage(Application.systemLanguage);
				SaveLanguagePref(_currentLanguage);
				return;
			}
			if ( LocalizationConfig.TryParseLanguage(saved, out var parsed) ) {
				_currentLanguage = FixLanguage(parsed);
				return;
			}
			_currentLanguage = _config.DefaultLanguage;
		}

		static SystemLanguage FixLanguage(SystemLanguage lang) {
			if ( _streamingAssetsLanguages.Contains(lang) || _config.ContainsSupportedLanguage(lang) ) {
				return lang;
			}
			var normalized = _config.NormalizeAlias(lang);
			if ( _streamingAssetsLanguages.Contains(normalized) || _config.ContainsSupportedLanguage(normalized) ) {
				return normalized;
			}
			return _config.DefaultLanguage;
		}

		static void SaveLanguagePref(SystemLanguage language) {
			PlayerPrefs.SetString(LocalePrefsKey, language.ToString());
			PlayerPrefs.Save();
		}

		static void Load(SystemLanguage targetLanguage) {
			_isLocaleLoaded = true;
			_baseTable.Clear();
			_currentLocaleIsDraft = false;

			if ( TryLoadLocaleTable(targetLanguage, out var isDraft) ) {
				_loadedLanguage = targetLanguage;
				_currentLocaleIsDraft = isDraft;
				_localeLoadFailed = false;
				if ( _currentLocaleIsDraft ) {
					Debug.LogWarningFormat("LocalizationController: Locale {0} is marked as a draft", _loadedLanguage);
				}
				Debug.LogFormat("Locale load finished ({0}). Loaded {1} elements.", _loadedLanguage, _baseTable.Count);
				return;
			}

			_localeLoadFailed = true;
			var fallback = _config.DefaultLanguage;
			if ( targetLanguage != fallback ) {
				Debug.LogWarningFormat("LocalizationController: Cannot load locale {0}. Falling back to {1}.", targetLanguage, fallback);
				if ( TryLoadLocaleTable(fallback, out var fallbackDraft) ) {
					_loadedLanguage = fallback;
					_currentLocaleIsDraft = fallbackDraft;
					if ( _currentLocaleIsDraft ) {
						Debug.LogWarningFormat("LocalizationController: Fallback locale {0} is marked as a draft", _loadedLanguage);
					}
					Debug.LogFormat("Fallback locale load finished ({0}). Loaded {1} elements.", _loadedLanguage, _baseTable.Count);
					return;
				}
			}

			_loadedLanguage = fallback;
			Debug.LogError("LocalizationController: Cannot load default locale.");
		}

		static bool TryLoadLocaleTable(SystemLanguage language, out bool isDraft) {
			isDraft = false;
			var hasStreaming = _streamingAssetsLanguages.Contains(language);
			if ( hasStreaming ) {
				var streamingText = ReadStreamingAssetsLocaleText(language);
				if ( !string.IsNullOrEmpty(streamingText) && _baseTable.LoadFromXml(streamingText, out _) ) {
					isDraft = _baseTable.IsDraft;
					return true;
				}

				Debug.LogErrorFormat("LocalizationController: Cannot load locale {0} from streaming assets. Trying to fallback to resources.", language);
				if ( !_config.ContainsSupportedLanguage(language) ) {
					return false;
				}
			}

			var xmlText = ReadResourcesLocaleText(language);
			if ( string.IsNullOrEmpty(xmlText) || !_baseTable.LoadFromXml(xmlText, out _) ) {
				return false;
			}

			isDraft = _baseTable.IsDraft;
			return true;
		}

		static string ReadResourcesLocaleText(SystemLanguage language) {
#if UNITY_EDITOR
			if ( ResourceXmlLoaderOverride != null ) {
				return ResourceXmlLoaderOverride(language);
			}
#endif
			var resourcePath = string.Format(LocFileFormatPath, language);
			var asset = Resources.Load<TextAsset>(resourcePath);
			if ( !asset ) {
				return null;
			}
			var xmlText = asset.text;
			Resources.UnloadAsset(asset);
			return xmlText;
		}

		static string ReadStreamingAssetsLocaleText(SystemLanguage language) {
#if UNITY_EDITOR
			if ( StreamingXmlLoaderOverride != null ) {
				return StreamingXmlLoaderOverride(language);
			}
#endif
			var fullPath = Path.Combine(Application.streamingAssetsPath, StreamingAssetsPath, $"Locale_{language}.xml");
			if ( !File.Exists(fullPath) ) {
				Debug.LogWarningFormat("LocalizationController: StreamingAssets file not found at '{0}'", fullPath);
				return null;
			}
			try {
				return File.ReadAllText(fullPath);
			} catch ( Exception e ) {
				Debug.LogWarningFormat("LocalizationController: Failed to read '{0}': {1}", fullPath, e.Message);
				return null;
			}
		}

		static void LogFormatError(string id, FormatException e) {
			Debug.LogErrorFormat("LocalizationController.TransFormat: Invalid format for key '{0}' (language {1}): {2}", id, _loadedLanguage, e.Message);
		}
	}

	/// <summary><see cref="IsDraft"/> mirrors <see cref="LocalizationController.CurrentLocaleIsDraft"/> for the new locale.</summary>
	public struct Event_LanguageChanged { public SystemLanguage Language; public bool IsDraft; }
}
#endif
