using System;
using System.Globalization;

namespace SMGCore.Parser {
	public sealed class UlongTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(ulong);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return 0;
			}

			ulong.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
			return result;
		}
	}
}
