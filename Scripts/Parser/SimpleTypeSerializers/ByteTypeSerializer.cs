using System;

namespace SMGCore.Parser {
	public class ByteTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(byte);

		public override string SerializeValue(object value, string format) => value.ToString();
	}
}
