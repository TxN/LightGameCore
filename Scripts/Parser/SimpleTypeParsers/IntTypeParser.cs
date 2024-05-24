using System;
using System.Globalization;

namespace SMGCore.Parser {
	public sealed class IntTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(int);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return 0;
			}

			int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
			return result;
		}
	}
}
