using System;
using System.Globalization;

namespace SMGCore.Parser {
	public class FloatTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(float);

		public override string SerializeValue(object value, string format) =>
					((float) value).ToString(CultureInfo.InvariantCulture);
	}
}
