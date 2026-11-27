using System;
using System.Globalization;

namespace SMGCore.Parser {
	public class UintTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(uint);

		public override string SerializeValue(object value, string format) => ((uint)value).ToString(CultureInfo.InvariantCulture);
	}
}
