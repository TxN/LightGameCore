using System;
using System.Globalization;
using UnityEngine;

namespace SMGCore.Parser {
	public class Vector2TypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(Vector2);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return Vector2.zero;
			}
			var split = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
			if ( split.Length < 2 ) {
				return Vector2.zero;
			}
			float.TryParse(split[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultX);
			float.TryParse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultY);
			return new Vector2(resultX, resultY);
		}
	}
}
