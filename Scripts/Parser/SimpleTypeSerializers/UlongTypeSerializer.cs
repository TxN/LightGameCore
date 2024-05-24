using System;

namespace SMGCore.Parser {
	public class UlongTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(ulong);

		public override string SerializeValue(object value, string format) => value.ToString();
	}
}
