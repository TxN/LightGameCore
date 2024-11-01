using System;
using System.Globalization;
using UnityEngine;

namespace SMGCore.Parser {
	public class Vector3TypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(Vector3);

		public override string SerializeValue(object value, string format) {
			var cast = (Vector3)value;
			var x = cast.x.ToString(CultureInfo.InvariantCulture);
			var y = cast.y.ToString(CultureInfo.InvariantCulture);
			var z = cast.z.ToString(CultureInfo.InvariantCulture);
			return $"{x};{y};{z}";
		}
	}
}
