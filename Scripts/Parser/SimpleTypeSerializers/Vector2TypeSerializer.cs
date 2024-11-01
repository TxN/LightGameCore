using System;
using System.Globalization;
using UnityEngine;

namespace SMGCore.Parser {
	public class Vector2TypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(Vector2);
		public override string SerializeValue(object value, string format) {
			var cast = (Vector2)value;
			var x = cast.x.ToString(CultureInfo.InvariantCulture);
			var y = cast.y.ToString(CultureInfo.InvariantCulture);
			return $"{x};{y}";
		}
	}
}
