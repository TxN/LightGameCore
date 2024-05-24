using System;

namespace SMGCore.Parser {
	public sealed class BoolTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(bool);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return false;
			}

			bool.TryParse(value, out var result);
			return result;
		}
	}
}
