using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

#if UNITY_2017_1_OR_NEWER
using UnityEngine;
#endif

namespace SMGCore {
	public sealed class LocalizationTable {
		public const string DraftAttribute = "draft";
		public const string EmptyLocString = "!{0}!";

		const int InitialCapacity = 4096;

		readonly Dictionary<string, string> _entries = new Dictionary<string, string>(InitialCapacity, StringComparer.Ordinal);
		readonly List<string> _pathSegments = new List<string>(8);
		readonly StringBuilder _keyBuilder = new StringBuilder(128);

		public int Count => _entries.Count;
		public bool IsDraft { get; private set; }

		public void Clear() {
			_entries.Clear();
			IsDraft = false;
		}

		public bool LoadFromXml(string xmlText, out int duplicateCount) {
			duplicateCount = 0;
			_entries.Clear();
			IsDraft = false;
			_pathSegments.Clear();

			if ( string.IsNullOrEmpty(xmlText) ) {
				return false;
			}

			var settings = new XmlReaderSettings {
				IgnoreComments = true,
				IgnoreWhitespace = true,
				IgnoreProcessingInstructions = true
			};

			try {
				using ( var stringReader = new StringReader(xmlText) )
				using ( var reader = XmlReader.Create(stringReader, settings) ) {
					if ( reader.MoveToContent() != XmlNodeType.Element || reader.Name != "root" ) {
						return false;
					}

					var draftAttr = reader.GetAttribute(DraftAttribute);
					IsDraft = string.Equals(draftAttr, "true", StringComparison.OrdinalIgnoreCase);

					if ( reader.IsEmptyElement ) {
						return true;
					}

					while ( reader.Read() ) {
						if ( reader.NodeType == XmlNodeType.Element ) {
							var isEmpty = reader.IsEmptyElement;
							_pathSegments.Add(reader.Name);

							var text = reader.GetAttribute("text");
							if ( !string.IsNullOrEmpty(text) ) {
								var key = BuildKey();
								if ( _entries.ContainsKey(key) ) {
									_entries[key] = text;
									duplicateCount++;
#if UNITY_2017_1_OR_NEWER
									Debug.LogErrorFormat("Duplicate entries in loc file for '{0}'", key);
#endif
								} else {
									_entries.Add(key, text);
								}
							}

							if ( isEmpty ) {
								_pathSegments.RemoveAt(_pathSegments.Count - 1);
							}
						} else if ( reader.NodeType == XmlNodeType.EndElement ) {
							if ( _pathSegments.Count > 0 ) {
								_pathSegments.RemoveAt(_pathSegments.Count - 1);
							}
						}
					}
				}

				return true;
			} catch ( Exception e ) {
#if UNITY_2017_1_OR_NEWER
				Debug.LogWarningFormat("LocalizationTable: Failed to parse locale XML: {0}", e);
#endif
				Clear();
				return false;
			} finally {
				_pathSegments.Clear();
			}
		}

		public bool TryGet(string id, out string text) {
			if ( string.IsNullOrEmpty(id) ) {
				text = null;
				return false;
			}
			return _entries.TryGetValue(id, out text);
		}

		public bool ContainsKey(string id) {
			return !string.IsNullOrEmpty(id) && _entries.ContainsKey(id);
		}

		public string GetOrMissing(string id) {
			if ( string.IsNullOrEmpty(id) ) {
				return string.Empty;
			}
			if ( _entries.TryGetValue(id, out var text) && !string.IsNullOrEmpty(text) ) {
				return text;
			}
			return FormatMissingKey(id);
		}

		public void GetChildIds(string startsWith, List<string> output) {
			if ( output == null ) {
				return;
			}
			output.Clear();
			var prefix = startsWith ?? string.Empty;
			foreach ( var kv in _entries ) {
				if ( kv.Key.StartsWith(prefix, StringComparison.Ordinal) ) {
					output.Add(kv.Key);
				}
			}
		}

		public void CollectSymbols(HashSet<char> output) {
			if ( output == null ) {
				return;
			}
			foreach ( var kv in _entries ) {
				var text = kv.Value;
				if ( string.IsNullOrEmpty(text) ) {
					continue;
				}
				for ( var i = 0; i < text.Length; i++ ) {
					output.Add(text[i]);
				}
			}
		}

		public static string FormatMissingKey(string id) {
			if ( string.IsNullOrEmpty(id) ) {
				return string.Empty;
			}
			var dotIndex = id.LastIndexOf('.');
			var segment = dotIndex >= 0 ? id.Substring(dotIndex + 1) : id;
			return string.Format(EmptyLocString, segment);
		}

		string BuildKey() {
			var count = _pathSegments.Count;
			if ( count == 1 ) {
				return _pathSegments[0];
			}
			_keyBuilder.Clear();
			_keyBuilder.Append(_pathSegments[0]);
			for ( var i = 1; i < count; i++ ) {
				_keyBuilder.Append('.');
				_keyBuilder.Append(_pathSegments[i]);
			}
			return _keyBuilder.ToString();
		}
	}
}
