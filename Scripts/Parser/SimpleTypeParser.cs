using System;

namespace SMGCore.Parser {
	public abstract class SimpleTypeParser {
		public abstract Type GetParserType();
		public abstract object ParseValue(string value, string format = null);
	}
}
