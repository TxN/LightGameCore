using System;
using System.Linq;
using System.Reflection;

namespace SMGCore.Utils {
	public static class ReflectionUtility {

		static Type[] _cachedTypes;

		public static Type[] GetAllTypes() {
			if ( _cachedTypes == null ) {
				_cachedTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).ToArray();
			}
			return _cachedTypes;
		}

		public static Type[] GetSubclasses(Type type) {
			var types = GetAllTypes().Where(p => p.IsSubclassOf(type)).ToArray();
			return types;
		}

		public static object CreateObjectWithActivator(Type type) {
			return Activator.CreateInstance(type);
		}

		public static void SetPropertyValue(object obj, string propertyName, object value) {
			if ( obj == null || string.IsNullOrEmpty(propertyName) ) {
				return;
			}
			var propertyInfo = obj.GetType().GetProperty(propertyName);
			if ( propertyInfo == null ) {
				return;
			}
			propertyInfo.SetValue(obj, value);
		}

		public static object CallMethod(object sourceObject, string methodName, params object[] args) {
			Type type = sourceObject.GetType();
			MethodInfo method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

			if ( method != null ) {
				try {
					object result = method.Invoke(sourceObject, args);
					return result;
				} catch ( Exception ex ) {
					UnityEngine.Debug.LogException( ex );
					return null;
				}
			} else {
				// Method with the specified name not found
				UnityEngine.Debug.LogWarning($"ReflectionUtility.CallMethod: Method '{methodName}' not found.");
				return null;
			}
		}
	}
}
