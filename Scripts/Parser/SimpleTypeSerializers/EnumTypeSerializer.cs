using System;

namespace SMGCore.Parser {
	public class EnumTypeSerializer<T> : SimpleTypeSerializer where T: Enum {
		public override Type GetSerializerType() => typeof(T);

		public override string SerializeValue(object value, string format) {
			var e = (Enum)value;
			return e.ToString();
		}
	}
}
