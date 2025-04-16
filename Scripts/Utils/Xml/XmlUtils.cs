using System;
using System.IO;
using System.Xml;

using UnityEngine;

using JetBrains.Annotations;

namespace SMGCore.Utils.Xml {
	public static class XmlUtils {		
		[CanBeNull]
		public static XmlDocument LoadXmlDocumentFromText(string text) {
			return TextToXmlDocument(text);
		}
		
		public static XmlReader CreateXmlReader(string text) {
			var readerSettings = new XmlReaderSettings { IgnoreComments = true };
			return XmlReader.Create(new StringReader(text), readerSettings);
		}

		#if UNITY_2017_1_OR_NEWER
		[CanBeNull]
		public static XmlDocument LoadXmlDocumentFromAssets(string filename) {
			var xmlAsset = Resources.Load<TextAsset>(filename);
			return xmlAsset && !string.IsNullOrEmpty(xmlAsset.text)
				? LoadXmlDocumentFromText(xmlAsset.text)
				: null;
		}

		public static XmlDocument LoadXmlDocumentFromStreamingAssets(string filename) {
			if ( !filename.EndsWith(".xml") ) {
				filename = filename + ".xml";
			}
			var filePath = Path.Combine(Application.streamingAssetsPath, filename);
			if ( !File.Exists(filePath) ) {
				Debug.LogWarningFormat("LoadXmlDocumentFromStreamingAssets: File not found at path '{0}'", filePath);
				return null;
			}
			try {
				var fileContent = File.ReadAllText(filePath);
				return TextToXmlDocument(fileContent);
			} catch ( Exception e ) {
				Debug.LogWarningFormat("LoadXmlDocumentFromStreamingAssets: Failed to read or parse file '{0}'. Exception: {1}", filePath, e);
				return null;
			}
		}
		#endif
		
		[CanBeNull]
		static XmlDocument TextToXmlDocument(string text) {
			try {
				using ( var reader = CreateXmlReader(text) ) {
					var xmldoc = new XmlDocument();
					xmldoc.Load(reader);
					return xmldoc;
				}
			} catch ( Exception e ) {
#if UNITY_2017_1_OR_NEWER
				Debug.LogWarningFormat("TextToXmlDocument: exception: {0}", e);
#endif
				return null;
			}
		}
	}
}
