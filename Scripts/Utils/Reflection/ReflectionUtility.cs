using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace SMGCore.Utils {
	public static class ReflectionUtility {

		static Type[] _cachedTypes;
		static Dictionary<Type, ConstructorInfo[]> _constructorCache;

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

		/// <summary>
		/// Для создания объекта, у которого нет конструктора без параметров
		/// Внимание! Созданные таким образом объекты могут быть некорректно инициализированы, это стоит учитывать.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static object CreateWithDefaultParameters(Type type) {
			if ( type == null ) {
				return null;
			}
			var c = GetConstructors(type);
			if ( c == null || c.Length == 0 ) {
				return null;
			}
			var cn = c[0].GetParameters();
			var args = new object[cn.Length];
			var result = c[0].Invoke(args);
			return result;
		}

		public static ConstructorInfo[] GetConstructors(Type type) {
			if ( _constructorCache == null ) {
				_constructorCache = new Dictionary<Type, ConstructorInfo[]>();
			}
			if ( type == null ) {
				return null;
			}
			if ( _constructorCache.ContainsKey( type ) ) {
				return _constructorCache[ type ];
			}
			var c = type.GetConstructors();
			_constructorCache[ type ] = c;
			return c;
		}

		public static object GetPropertyValue(object obj, PropertyInfo propertyInfo ) {
			if ( obj == null || propertyInfo == null ) {
				return default;
			}
			return propertyInfo.GetValue(obj);
		}

		public static T GetPropertyValue<T>(object obj, string propertyName) {
			if ( obj == null || string.IsNullOrEmpty(propertyName) ) {
				return default;
			}
			var propertyInfo = obj.GetType().GetProperty(propertyName);
			if ( propertyInfo == null ) {
				return default;
			}
			return (T)propertyInfo.GetValue(obj);
		}

		public static void SetPropertyValue(object obj, string propertyName, object value) {
			if ( obj == null || string.IsNullOrEmpty(propertyName) ) {
				return;
			}
			var propertyInfo = obj.GetType().GetProperty(propertyName);
			if ( propertyInfo == null ) {
				return;
			}
			propertyInfo.SetValue(obj, value, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
		}

		public static void SetPropertyValue(PropertyInfo propInfo, object obj, object value) {
			if ( obj == null || propInfo == null ) {
				return;
			}
			propInfo.SetValue(obj, value, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
		}

		public static object GetFieldValue(object obj, string fieldName) {
			if ( obj == null || string.IsNullOrEmpty(fieldName) ) {
				return null;
			}
			var fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic );
			if ( fieldInfo == null ) {
				return null;
			}
			return fieldInfo.GetValue(obj);
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
