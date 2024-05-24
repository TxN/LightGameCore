using System;
using System.Globalization;

namespace SMGCore.Parser {
	public sealed class ByteTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(byte);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return 0;
			}

			int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
			if ( result > 255 || result < 0 ) {
				result = 0;
			}
			return (byte) result;
		}
	}
}
