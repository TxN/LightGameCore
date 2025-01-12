using System.Xml;

namespace SMGCore.Utils.Xml {
	public static class XmlElementExtensions {
	
		public static void AddAttrValue(this XmlElement elem, string name, string value) {
			var attr = elem.OwnerDocument.CreateAttribute(name);
			attr.Value = value;
			elem.Attributes.Append(attr);
		}
		
		public static void AddAttrValue(this XmlElement elem, string name, int value) {
			elem.AddAttrValue(name, value.ToString());
		}
		
		public static void AddAttrValue(this XmlElement elem, string name, uint value) {
			elem.AddAttrValue(name, value.ToString());
		}
		
		public static void AddAttrValue(this XmlElement elem, string name, long value) {
			elem.AddAttrValue(name, value.ToString());
		}
		
		public static void AddAttrValue(this XmlElement elem, string name, ulong value) {
			elem.AddAttrValue(name, value.ToString());
		}
		
		public static void AddAttrValue(this XmlElement elem, string name, bool value) {
			elem.AddAttrValue(name, value.ToString());
		}
		
		public static void AddAttrValue(this XmlElement elem, string name, float value) {
			elem.AddAttrValue(name, value.ToString());
		}
#if UNITY_2017_1_OR_NEWER
		public static void AddAttrValue(this XmlElement elem, string name, UnityEngine.SystemLanguage value) {
			elem.AddAttrValue(name, value.ToString());
		}
#endif
	}
}
