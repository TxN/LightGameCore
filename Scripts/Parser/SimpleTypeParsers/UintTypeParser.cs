using System;
using System.Globalization;

namespace SMGCore.Parser {
	public sealed class UintTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(uint);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return 0;
			}

			uint.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
			return result;
		}
	}
}
