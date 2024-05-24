using System;
using System.Globalization;

namespace SMGCore.Parser {
	public sealed class LongTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(long);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return 0;
			}

			long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
			return result;
		}
	}
}
