using System;

namespace SMGCore.Parser {
	public class DateTimeTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(DateTime);

		public override string SerializeValue(object value, string format) {
			var val = (DateTime)value;
			if ( val.Year < 1900 ) {
				return null;
			}
			return val.ToString(string.IsNullOrEmpty(format) ? "yyyy-MM-dd HH-mm" : format);
		}
	}
}
