using System;

namespace SMGCore.Parser {
	public class BoolTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(bool);

		public override string SerializeValue(object value, string format) {
			var boolean = (bool)value;
			return boolean.ToStringLowerCase();
		}
	}
}
