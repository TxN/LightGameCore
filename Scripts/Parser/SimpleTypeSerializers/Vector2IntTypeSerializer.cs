using System;
using System.Globalization;
using UnityEngine;

namespace SMGCore.Parser {
	public class Vector2IntTypeSerializer : SimpleTypeSerializer {
		public override Type GetSerializerType() => typeof(Vector2Int);

		public override string SerializeValue(object value, string format) {
			var cast = (Vector2Int)value;
			return $"{cast.x.ToString(CultureInfo.InvariantCulture)}, {cast.y.ToString(CultureInfo.InvariantCulture)}";
		}
	}
}
