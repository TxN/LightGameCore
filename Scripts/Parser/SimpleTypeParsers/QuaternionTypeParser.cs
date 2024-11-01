using System;
using System.Globalization;
using UnityEngine;

namespace SMGCore.Parser {
	public class QuaternionTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(Quaternion);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return Quaternion.identity;
			}
			var split = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
			if ( split.Length < 4 ) {
				return Quaternion.identity;
			}
			float.TryParse(split[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultX);
			float.TryParse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultY);
			float.TryParse(split[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultZ);
			float.TryParse(split[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultW);
			return new Quaternion(resultX, resultY, resultZ, resultW);
		}
	}
}
