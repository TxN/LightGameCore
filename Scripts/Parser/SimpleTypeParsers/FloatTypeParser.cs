using System;
using System.Globalization;

namespace SMGCore.Parser {
	public sealed class FloatTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(float);

		public override object ParseValue(string value, string format = null) {
			if ( float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ) {
				return result;
			}
			return 0f;
		}		
	}
}
