using System;
using System.Globalization;

namespace SMGCore.Parser {
	public sealed class DoubleTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(double);

		public override object ParseValue(string value, string format = null) {
			if ( double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ) {
				return result;
			}
			return 0f;
		}
	}
}
