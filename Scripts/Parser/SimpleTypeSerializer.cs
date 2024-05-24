using System;

namespace SMGCore.Parser {
	public abstract class SimpleTypeSerializer {
		public abstract Type GetSerializerType();
		public abstract string SerializeValue(object value, string format);
	}
}
