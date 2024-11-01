using System;
using System.Globalization;

namespace SMGCore.Parser {
	public class DoubleTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(double);

		public override string SerializeValue(object value, string format) =>
			((double)value).ToString(CultureInfo.InvariantCulture);
	}
}
