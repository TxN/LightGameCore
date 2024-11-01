using System;
using System.Globalization;
using UnityEngine;

namespace SMGCore.Parser {
	public class QuaternionTypeSerizalizer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(Quaternion);

		public override string SerializeValue(object value, string format) {
			var cast = (Quaternion)value;
			var x = cast.x.ToString(CultureInfo.InvariantCulture);
			var y = cast.y.ToString(CultureInfo.InvariantCulture);
			var z = cast.z.ToString(CultureInfo.InvariantCulture);
			var w = cast.w.ToString(CultureInfo.InvariantCulture);
			return $"{x};{y};{z};{w}";
		}
	}
}
