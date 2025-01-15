using System;
using System.Globalization;

namespace SMGCore.Parser {
	public sealed class UshortTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(ushort);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return 0;
			}

			ushort.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
			return result;
		}
	}
}
