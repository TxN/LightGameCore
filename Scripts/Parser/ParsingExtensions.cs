using System.Globalization;

namespace SMGCore.Parser {
	public static class ParsingExtensions {
		public static bool TrySafeParseToInt(this string str, out int result) =>
			int.TryParse(
				str, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

		public static bool TrySafeParseToFloat(this string str, out float result) =>
			float.TryParse(
				str, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

		public static bool TrySafeParseToLong(this string str, out long result) =>
			long.TryParse(
				str, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
	}
}
