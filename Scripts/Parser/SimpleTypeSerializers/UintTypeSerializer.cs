using System;

namespace SMGCore.Parser {
	public class UintTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(uint);

		public override string SerializeValue(object value, string format) => value.ToString();
	}
}
