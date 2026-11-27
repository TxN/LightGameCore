using System;
using System.Globalization;

namespace SMGCore.Parser {
	public class UshortTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(ushort);

		public override string SerializeValue(object value, string format) => ((ushort)value).ToString(CultureInfo.InvariantCulture);
	}
}
