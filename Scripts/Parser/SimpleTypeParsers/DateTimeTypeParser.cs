using System;
using System.Globalization;

namespace SMGCore.Parser {
	public sealed class DateTimeTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(DateTime);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return DateTime.MinValue;
			}

			if ( DateTime.TryParseExact(value, "yyyy-MM-dd HH-mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ) {
				return result;
			}
			return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result) ? result : DateTime.MinValue;
		}
	}
}
