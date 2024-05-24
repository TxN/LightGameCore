using System;

namespace SMGCore.Parser {
	public sealed class FloatTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(float);

		public override object ParseValue(string value, string format = null) =>
			FloatFormat.ParseValue(value, format);
	}
}
