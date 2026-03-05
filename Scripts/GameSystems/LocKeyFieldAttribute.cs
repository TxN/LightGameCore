using System;

namespace SMGCore {
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class LocKeyFieldAttribute : Attribute {
        public string BaseKey { get; set; }

        public LocKeyFieldAttribute(string baseKey = "") {
            BaseKey = baseKey;
        }
    }
}

