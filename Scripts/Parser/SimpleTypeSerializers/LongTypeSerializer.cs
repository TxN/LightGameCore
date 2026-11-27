using System;
using System.Globalization;

namespace SMGCore.Parser {
	public class LongTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(long);

		public override string SerializeValue(object value, string format) => ((long)value).ToString(CultureInfo.InvariantCulture);
	}
}
