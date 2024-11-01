using System;
using System.Globalization;
using UnityEngine;

namespace SMGCore.Parser {
	public class Vector4TypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(Vector4);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return Vector4.zero;
			}
			var split = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
			if ( split.Length < 4 ) {
				return Vector4.zero;
			}
			float.TryParse(split[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultX);
			float.TryParse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultY);
			float.TryParse(split[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultZ);
			float.TryParse(split[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultW);
			return new Vector4(resultX, resultY, resultZ, resultW);
		}
	}
}
