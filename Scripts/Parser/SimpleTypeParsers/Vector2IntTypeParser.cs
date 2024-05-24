using System;
using System.Globalization;
using UnityEngine;

namespace SMGCore.Parser {
	public class Vector2IntTypeParser : SimpleTypeParser {
		public override Type GetParserType() => typeof(Vector2Int);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return Vector2Int.zero;
			}
			var split = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
			if ( split.Length < 2 ) {
				return Vector2Int.zero;
			}
			int.TryParse(split[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultX);
			int.TryParse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var resultY);
			return new Vector2Int(resultX, resultY);
		}
	}
}
