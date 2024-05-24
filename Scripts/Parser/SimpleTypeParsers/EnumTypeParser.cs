using System;

namespace SMGCore.Parser {
	public sealed class EnumTypeParser<T> : SimpleTypeParser where T : Enum {
		public override Type GetParserType() => typeof(T);

		public override object ParseValue(string value, string format = null) {
			if ( string.IsNullOrEmpty(value) ) {
				return default(T);
			}
#if UNITY_2017_3_OR_NEWER
			try {
				return Enum.Parse(typeof(T), value);
			} catch ( Exception e ) {
				UnityEngine.Debug.LogException(e);
				return default(T);
			}
#else
			return Enum.Parse(typeof(T), value);
#endif
		}
	}
}
