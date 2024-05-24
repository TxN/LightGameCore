using System;

namespace SMGCore.Parser {
	public class FloatTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(float);

		public override string SerializeValue(object value, string format) =>
			FloatFormat.SerializeValue((float)value, format);
	}
}
