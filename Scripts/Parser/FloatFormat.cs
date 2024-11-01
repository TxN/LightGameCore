using System;
using System.Globalization;

namespace SMGCore.Parser {
	public static class FloatFormat {
		const int DefaultRound = 4;

		public static float ParseValue(string value, string format) {
			if ( string.IsNullOrEmpty(value) ) {
				return 0f;
			}

			float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result);
			return (float)Math.Round(result, ParseRoundFormat(format));
		}

		public static string SerializeValue(float value, string format) {
			var roundedValue = Math.Round(value, ParseRoundFormat(format));
			return roundedValue.ToString(CultureInfo.InvariantCulture);
		}

		static int ParseRoundFormat(string format = null) {
			if ( !string.IsNullOrEmpty(format) && int.TryParse(format, out var roundFormatValue) && (roundFormatValue >= 0) ) {
				return roundFormatValue;
			}

			return DefaultRound;
		}
	}
}