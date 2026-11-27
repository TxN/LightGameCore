using System;
using System.Globalization;

namespace SMGCore.Parser {
	public class IntTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(int);

		public override string SerializeValue(object value, string format) => ((int)value).ToString(CultureInfo.InvariantCulture);
	}
}
