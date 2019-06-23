using UnityEngine;

using System;
using System.IO;
using System.Xml;

using JetBrains.Annotations;

namespace Utils.Xml {
	public static class XmlUtils {		
		[CanBeNull]
		public static XmlDocument LoadXmlDocumentFromText(string text) {
			return TextToXmlDocument(text);
		}
		
		public static XmlReader CreateXmlReader(string text) {
			var readerSettings = new XmlReaderSettings { IgnoreComments = true };
			return XmlReader.Create(new StringReader(text), readerSettings);
		}
		
		[CanBeNull]
		public static XmlDocument LoadXmlDocumentFromAssets(string filename) {
			var xmlAsset = Resources.Load<TextAsset>(filename);
			return xmlAsset && !string.IsNullOrEmpty(xmlAsset.text)
				? LoadXmlDocumentFromText(xmlAsset.text)
				: null;
		}
		
		[CanBeNull]
		static XmlDocument TextToXmlDocument(string text) {
			try {
				using ( var reader = CreateXmlReader(text) ) {
					var xmldoc = new XmlDocument();
					xmldoc.Load(reader);
					return xmldoc;
				}
			} catch ( Exception e ) {
				Debug.LogWarningFormat("TextToXmlDocument: exception: {0}", e);
				return null;
			}
		}
	}
}
