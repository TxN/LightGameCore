#if UNITY_2017_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using UnityEngine;

namespace SMGCore {
	public sealed class LocalizationConfig {
		public const string DefaultConfigResourcePath = "Configs/LocalizationConfig";
		public const SystemLanguage FallbackDefaultLanguage = SystemLanguage.English;

		readonly List<SystemLanguage> _supportedLanguages;
		readonly Dictionary<SystemLanguage, SystemLanguage> _aliases;

		public SystemLanguage DefaultLanguage { get; }
		public IReadOnlyList<SystemLanguage> SupportedLanguages => _supportedLanguages;

		public LocalizationConfig(
			SystemLanguage defaultLanguage,
			IEnumerable<SystemLanguage> supportedLanguages,
			IReadOnlyDictionary<SystemLanguage, SystemLanguage> aliases = null) {
			_supportedLanguages = new List<SystemLanguage>();

			if ( supportedLanguages != null ) {
				foreach ( var lang in supportedLanguages ) {
					if ( !_supportedLanguages.Contains(lang) ) {
						_supportedLanguages.Add(lang);
					}
				}
			}

			if ( _supportedLanguages.Count == 0 ) {
				_supportedLanguages.Add(defaultLanguage);
				DefaultLanguage = defaultLanguage;
			} else if ( !ContainsSupportedLanguage(defaultLanguage) ) {
				Debug.LogWarningFormat(
					"LocalizationConfig: Default language '{0}' is not in SupportedLanguages. Using '{1}' as default.",
					defaultLanguage,
					_supportedLanguages[0]);
				DefaultLanguage = _supportedLanguages[0];
			} else {
				DefaultLanguage = defaultLanguage;
			}

			if ( aliases != null && aliases.Count > 0 ) {
				_aliases = new Dictionary<SystemLanguage, SystemLanguage>(aliases.Count);
				foreach ( var kv in aliases ) {
					_aliases[kv.Key] = kv.Value;
				}
			}
		}

		public bool ContainsSupportedLanguage(SystemLanguage language) {
			for ( var i = 0; i < _supportedLanguages.Count; i++ ) {
				if ( _supportedLanguages[i] == language ) {
					return true;
				}
			}
			return false;
		}

		public SystemLanguage NormalizeAlias(SystemLanguage language) {
			if ( _aliases != null && _aliases.TryGetValue(language, out var mapped) ) {
				return mapped;
			}
			return language;
		}

		public static LocalizationConfig CreateFallback(SystemLanguage defaultLanguage = FallbackDefaultLanguage) {
			return new LocalizationConfig(defaultLanguage, new[] { defaultLanguage });
		}

		public static LocalizationConfig ParseXml(string xmlText, out bool parsedOk) {
			parsedOk = false;
			if ( string.IsNullOrEmpty(xmlText) ) {
				return CreateFallback();
			}

			var defaultLanguage = FallbackDefaultLanguage;
			var hasExplicitDefault = false;
			var languages = new List<SystemLanguage>(8);
			Dictionary<SystemLanguage, SystemLanguage> aliases = null;

			var settings = new XmlReaderSettings {
				IgnoreComments = true,
				IgnoreWhitespace = true,
				IgnoreProcessingInstructions = true
			};

			try {
				using ( var stringReader = new StringReader(xmlText) )
				using ( var reader = XmlReader.Create(stringReader, settings) ) {
					if ( reader.MoveToContent() != XmlNodeType.Element || reader.Name != "root" ) {
						Debug.LogWarning("LocalizationConfig: Root element <root> not found.");
						return CreateFallback();
					}

					var defaultAttr = reader.GetAttribute("default");
					if ( !string.IsNullOrEmpty(defaultAttr) ) {
						if ( TryParseLanguage(defaultAttr, out var parsedDefault) ) {
							defaultLanguage = parsedDefault;
							hasExplicitDefault = true;
						} else {
							Debug.LogWarningFormat("LocalizationConfig: Invalid default language '{0}', using '{1}'.", defaultAttr, defaultLanguage);
						}
					}

					if ( !reader.IsEmptyElement ) {
						while ( reader.Read() ) {
							if ( reader.NodeType != XmlNodeType.Element || reader.Name != "language" ) {
								continue;
							}

							var nameAttr = reader.GetAttribute("name");
							if ( !TryParseLanguage(nameAttr, out var lang) ) {
								Debug.LogWarningFormat("LocalizationConfig: Unsupported language name '{0}' in config.", nameAttr);
								continue;
							}

							if ( languages.Contains(lang) ) {
								Debug.LogWarningFormat("LocalizationConfig: Duplicate language '{0}' in config.", lang);
							} else {
								languages.Add(lang);
							}

							var aliasesAttr = reader.GetAttribute("aliases");
							if ( !string.IsNullOrEmpty(aliasesAttr) ) {
								ParseAliases(aliasesAttr, lang, ref aliases);
							}
						}
					}
				}
			} catch ( Exception e ) {
				Debug.LogWarningFormat("LocalizationConfig: Failed to parse XML config: {0}", e.Message);
				return CreateFallback();
			}

			if ( languages.Count == 0 ) {
				Debug.LogWarning("LocalizationConfig: Config contains no valid languages. Using fallback.");
				return CreateFallback(defaultLanguage);
			}

			if ( !hasExplicitDefault && !languages.Contains(defaultLanguage) ) {
				defaultLanguage = languages[0];
			}

			parsedOk = true;
			return new LocalizationConfig(defaultLanguage, languages, aliases);
		}

		internal static bool TryParseLanguage(string name, out SystemLanguage language) {
			language = default;
			if ( string.IsNullOrEmpty(name) ) {
				return false;
			}
			// Reject numeric strings (e.g. "999") that Enum.TryParse would otherwise accept.
			if ( !char.IsLetter(name[0]) ) {
				return false;
			}
			return Enum.TryParse(name, false, out language) && Enum.IsDefined(typeof(SystemLanguage), language);
		}

		static void ParseAliases(string aliasesAttr, SystemLanguage targetLanguage, ref Dictionary<SystemLanguage, SystemLanguage> aliases) {
			var start = 0;
			var length = aliasesAttr.Length;
			while ( start <= length ) {
				var comma = aliasesAttr.IndexOf(',', start);
				var end = comma >= 0 ? comma : length;
				var token = aliasesAttr.Substring(start, end - start).Trim();
				if ( token.Length > 0 ) {
					if ( TryParseLanguage(token, out var aliasLang) ) {
						aliases ??= new Dictionary<SystemLanguage, SystemLanguage>();
						aliases[aliasLang] = targetLanguage;
					} else {
						Debug.LogWarningFormat("LocalizationConfig: Unsupported alias '{0}' for language '{1}'.", token, targetLanguage);
					}
				}
				if ( comma < 0 ) {
					break;
				}
				start = comma + 1;
			}
		}
	}
}
#endif
