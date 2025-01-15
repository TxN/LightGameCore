using System;

namespace SMGCore.Parser {
	public class UshortTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(ushort);

		public override string SerializeValue(object value, string format) => value.ToString();
	}
}
