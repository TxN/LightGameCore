using System;
using System.Globalization;

namespace SMGCore.Parser {
	public class ByteTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(byte);

		public override string SerializeValue(object value, string format) => ((byte)value).ToString(CultureInfo.InvariantCulture);
	}
}
