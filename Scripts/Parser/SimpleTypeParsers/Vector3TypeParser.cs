using System;
using System.Globalization;
using UnityEngine;

namespace SMGCore.Parser {
	public class Vector3TypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(Vector3);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return Vector3.zero;
			}
			var split = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
			if ( split.Length < 3 ) {
				return Vector3.zero;
			}
			float.TryParse(split[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultX);
			float.TryParse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultY);
			float.TryParse(split[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultZ);
			return new Vector3(resultX, resultY, resultZ);
		}
	}
}
