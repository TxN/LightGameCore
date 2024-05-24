using System;

namespace SMGCore.Parser {
	public class StringTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(string);

		public override string SerializeValue(object value, string format) => (string)value;
	}
}
