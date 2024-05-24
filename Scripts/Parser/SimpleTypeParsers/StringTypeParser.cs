using System;

namespace SMGCore.Parser {
	public sealed class StringTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(string);

		public override object ParseValue(string value, string format = null) => value; // pass-through
	}
}
