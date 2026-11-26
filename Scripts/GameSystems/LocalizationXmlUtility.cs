#if UNITY_2017_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using UnityEngine;

namespace SMGCore {
	public static class LocalizationXmlUtility {
		const string IndentUnit = "  ";

		public static void CreateNewLocaleEntry(string path, string value, XmlDocument loc) {
			if ( loc == null ) {
				Debug.LogError("LocalizationXmlUtility.CreateNewLocaleEntry: document is null");
				return;
			}

			if ( !TryParsePathSegments(path, out var segments) ) {
				Debug.LogError($"LocalizationXmlUtility.CreateNewLocaleEntry: invalid path '{path}'");
				return;
			}

			XmlNode currentNode = loc.DocumentElement;
			if ( currentNode == null ) {
				Debug.LogError("LocalizationXmlUtility.CreateNewLocaleEntry: document has no root element");
				return;
			}

			var safeValue = NormalizeAttributeNewlines(value);
			var existingNode = FindNodeBySegments(currentNode, segments);
			if ( existingNode != null ) {
				Debug.LogWarning($"LocalizationXmlUtility.CreateNewLocaleEntry: node {string.Join(".", segments)} already exists, updating value");
				SetOrCreateTextAttribute(loc, existingNode, safeValue);
				return;
			}

			for ( var i = 0; i < segments.Length - 1; i++ ) {
				var nextNode = FindChildElement(currentNode, segments[i]);
				if ( nextNode == null ) {
					nextNode = loc.CreateElement(segments[i]);
					AppendElementWithIndent(loc, currentNode, nextNode, i + 1);
				}
				currentNode = nextNode;
			}

			var leafName = segments[segments.Length - 1];
			var element = loc.CreateElement(leafName);
			SetOrCreateTextAttribute(loc, element, safeValue);
			AppendElementWithIndent(loc, currentNode, element, segments.Length);
		}

		public static void SavePreservingFormat(XmlDocument doc, string fullPath, bool useCrLf = true) {
			if ( doc == null || string.IsNullOrEmpty(fullPath) ) {
				return;
			}

			var omitDeclaration = doc.FirstChild == null || doc.FirstChild.NodeType != XmlNodeType.XmlDeclaration;
			var settings = new XmlWriterSettings {
				Encoding = new UTF8Encoding(false),
				OmitXmlDeclaration = omitDeclaration,
				NewLineChars = useCrLf ? "\r\n" : "\n",
				NewLineHandling = NewLineHandling.Replace
			};

			using ( var writer = XmlWriter.Create(fullPath, settings) ) {
				doc.Save(writer);
			}
		}

		static string NormalizeAttributeNewlines(string value) {
			if ( string.IsNullOrEmpty(value) ) {
				return string.Empty;
			}
			if ( value.IndexOf('\r') < 0 ) {
				return value;
			}
			return value.Replace("\r\n", "\n").Replace("\r", "\n");
		}

		static bool TryParsePathSegments(string path, out string[] segments) {
			segments = null;
			if ( string.IsNullOrEmpty(path) ) {
				return false;
			}

			var trimmed = path.StartsWith("root.", StringComparison.Ordinal)
				? path.Substring("root.".Length)
				: path;
			if ( string.IsNullOrEmpty(trimmed) ) {
				return false;
			}

			var parts = trimmed.Split('.');
			for ( var i = 0; i < parts.Length; i++ ) {
				if ( string.IsNullOrEmpty(parts[i]) ) {
					return false;
				}
				try {
					XmlConvert.VerifyName(parts[i]);
				} catch ( XmlException ) {
					return false;
				}
			}

			segments = parts;
			return true;
		}

		static XmlElement FindChildElement(XmlNode parent, string name) {
			if ( parent == null ) {
				return null;
			}
			for ( var node = parent.FirstChild; node != null; node = node.NextSibling ) {
				if ( node is XmlElement element && string.Equals(element.Name, name, StringComparison.Ordinal) ) {
					return element;
				}
			}
			return null;
		}

		static XmlElement FindNodeBySegments(XmlNode root, string[] segments) {
			XmlElement current = null;
			var parent = root;
			for ( var i = 0; i < segments.Length; i++ ) {
				current = FindChildElement(parent, segments[i]);
				if ( current == null ) {
					return null;
				}
				parent = current;
			}
			return current;
		}

		static void SetOrCreateTextAttribute(XmlDocument doc, XmlElement element, string value) {
			var attr = element.Attributes["text"];
			if ( attr == null ) {
				attr = doc.CreateAttribute("text");
				element.Attributes.Append(attr);
			}
			attr.Value = value;
		}

		static void AppendElementWithIndent(XmlDocument doc, XmlNode parent, XmlElement child, int childDepth) {
			if ( !doc.PreserveWhitespace ) {
				parent.AppendChild(child);
				return;
			}

			var childIndent = BuildIndent(childDepth);
			var parentIndent = BuildIndent(childDepth - 1);
			var lastChild = parent.LastChild;

			if ( lastChild != null && lastChild.NodeType == XmlNodeType.Whitespace ) {
				var ws = doc.CreateWhitespace("\n" + childIndent);
				parent.InsertBefore(ws, lastChild);
				parent.InsertBefore(child, lastChild);
			} else {
				parent.AppendChild(doc.CreateWhitespace("\n" + childIndent));
				parent.AppendChild(child);
				parent.AppendChild(doc.CreateWhitespace("\n" + parentIndent));
			}
		}

		static string BuildIndent(int depth) {
			if ( depth <= 0 ) {
				return string.Empty;
			}
			var sb = new StringBuilder(depth * IndentUnit.Length);
			for ( var i = 0; i < depth; i++ ) {
				sb.Append(IndentUnit);
			}
			return sb.ToString();
		}

#if UNITY_EDITOR
		sealed class CachedLocaleFile {
			public DateTime LastWriteUtc;
			public readonly LocalizationTable Table = new LocalizationTable();
		}

		static readonly Dictionary<SystemLanguage, CachedLocaleFile> _editorFileCache = new Dictionary<SystemLanguage, CachedLocaleFile>();

		public static void ClearEditorCache() {
			_editorFileCache.Clear();
		}

		public static string GetAssetRelativeLocalePath(SystemLanguage language) {
			var resourcePath = string.Format(LocalizationController.LocFileFormatPath, language);
			return "Assets/Resources/" + resourcePath + ".xml";
		}

		public static DateTime GetLocaleFileLastWriteUtc(SystemLanguage language) {
			var assetPath = GetAssetRelativeLocalePath(language);
			return File.Exists(assetPath) ? File.GetLastWriteTimeUtc(assetPath) : DateTime.MinValue;
		}

		public static void UpdateEntry(string path, string newValue, SystemLanguage language, bool create = false) {
			if ( !TryParsePathSegments(path, out var segments) ) {
				Debug.LogError($"LocalizationXmlUtility.UpdateEntry: invalid path '{path}'");
				return;
			}

			var assetPath = GetAssetRelativeLocalePath(language);
			if ( !File.Exists(assetPath) ) {
				Debug.LogError($"LocalizationXmlUtility.UpdateEntry: Cannot find locale file '{assetPath}' for {language}");
				return;
			}

			string rawText;
			try {
				rawText = File.ReadAllText(assetPath);
			} catch ( Exception e ) {
				Debug.LogError($"LocalizationXmlUtility.UpdateEntry: Cannot read locale file '{assetPath}' for {language}: {e.Message}");
				return;
			}

			var useCrLf = rawText.IndexOf("\r\n", StringComparison.Ordinal) >= 0;

			var loc = new XmlDocument { PreserveWhitespace = true };
			try {
				loc.LoadXml(rawText);
			} catch ( Exception e ) {
				Debug.LogError($"LocalizationXmlUtility.UpdateEntry: Cannot parse locale file '{assetPath}' for {language}: {e.Message}");
				return;
			}

			var root = loc.DocumentElement;
			var node = FindNodeBySegments(root, segments);
			if ( node == null ) {
				if ( create ) {
					CreateNewLocaleEntry(path, newValue, loc);
					SavePreservingFormat(loc, assetPath, useCrLf);
					_editorFileCache.Remove(language);
					UnityEditor.AssetDatabase.Refresh();
					return;
				}
				Debug.LogError($"LocalizationXmlUtility.UpdateEntry: Cannot find node with path {path} in locale {language}");
				return;
			}

			var safeValue = NormalizeAttributeNewlines(newValue);
			var attr = node.Attributes["text"];
			if ( attr == null ) {
				if ( create ) {
					SetOrCreateTextAttribute(loc, node, safeValue);
				} else {
					Debug.LogError($"LocalizationXmlUtility.UpdateEntry: Cannot find text attribute in node with path {path} in locale {language}");
					return;
				}
			} else {
				attr.Value = safeValue;
			}

			SavePreservingFormat(loc, assetPath, useCrLf);
			_editorFileCache.Remove(language);
			UnityEditor.AssetDatabase.Refresh();
		}

		public static string GetEntryFromFile(string path, SystemLanguage language) {
			if ( string.IsNullOrEmpty(path) ) {
				return "!!!";
			}

			var assetPath = GetAssetRelativeLocalePath(language);
			if ( !File.Exists(assetPath) ) {
				Debug.LogError($"LocalizationXmlUtility.GetEntryFromFile: Cannot load locale file for {language}");
				return "!!!";
			}

			DateTime lastWriteUtc;
			try {
				lastWriteUtc = File.GetLastWriteTimeUtc(assetPath);
			} catch ( Exception e ) {
				Debug.LogWarning($"LocalizationXmlUtility.GetEntryFromFile: Cannot stat locale file '{assetPath}': {e.Message}");
				return "!!!";
			}

			if ( !_editorFileCache.TryGetValue(language, out var cached) ) {
				cached = new CachedLocaleFile();
				_editorFileCache[language] = cached;
			}

			if ( cached.LastWriteUtc != lastWriteUtc ) {
				string xmlText;
				try {
					xmlText = File.ReadAllText(assetPath);
				} catch ( Exception e ) {
					Debug.LogWarning($"LocalizationXmlUtility.GetEntryFromFile: Cannot read locale file '{assetPath}': {e.Message}");
					return "!!!";
				}
				if ( !cached.Table.LoadFromXml(xmlText, out _) ) {
					Debug.LogError($"LocalizationXmlUtility.GetEntryFromFile: Cannot parse locale file for {language}");
					cached.LastWriteUtc = DateTime.MinValue;
					return "!!!";
				}
				cached.LastWriteUtc = lastWriteUtc;
			}

			return cached.Table.TryGet(path, out var text) ? text : "!!!";
		}
#endif
	}
}
#endif
