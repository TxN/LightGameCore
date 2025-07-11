using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace SMGCore.Utils {
	public static class ReflectionUtility {

		static Type[] _cachedTypes;
		static Dictionary<Type, ConstructorInfo[]> _constructorCache;
		static Dictionary<string, Type> _typeByFullName;
		static Dictionary<Type,Type[]> _subclassCache;
		static readonly Dictionary<(Type, Type), FieldInfo[]> _fieldsWithAttributeCache = new Dictionary<(Type, Type), FieldInfo[]>();

		public static Type[] GetAllTypes() {
			if ( _cachedTypes == null ) {
				_cachedTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).ToArray();
				_subclassCache = null;
			}
			return _cachedTypes;
		}

		public static Type[] GetAllTypesNonCached(List<string> assemblyNames = null) {
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			if (
				(assemblyNames == null) || (assemblyNames.Count == 0) ||
				((assemblyNames.Count == 1) && (assemblyNames[0] == "Assembly-CSharp")) ) {
				return assemblies.SelectMany(a => a.GetTypes()).ToArray();
			}

			var set = new HashSet<string>();
			foreach ( var assemblyName in assemblyNames ) {
				set.Add(assemblyName);
			}

			var selectedAssemblies = new List<Assembly>(assemblyNames.Count);
			foreach ( var asm in assemblies ) {
				if ( set.Contains(asm.GetName().Name) ) {
					selectedAssemblies.Add(asm);
				}
			}
			return selectedAssemblies.SelectMany(s => s.GetTypes()).ToArray();
		}

		public static Dictionary<string, Type> GetTypeCache() {
			if ( _typeByFullName != null ) {
				return _typeByFullName;
			}
			_typeByFullName = new Dictionary<string, Type>();
			var types = GetAllTypes();
			foreach ( var t in types ) {
				var name = t.FullName;
#if UNITY_EDITOR
				if ( _typeByFullName.ContainsKey(name) ) {
					UnityEngine.Debug.LogError($"ReflectionUtility: duplicate types found: {t.FullName}");
				}
#endif
				_typeByFullName.Add(name, t);
			}
			return _typeByFullName;
		}

		public static Type GetTypeByFullName(string name) {
			if ( string.IsNullOrEmpty(name) ) {
				return null;
			}
			var types = GetTypeCache();
			if ( types.ContainsKey(name) ) {
				return types[name];
			}
			return null;
		}

		public static Type GetSubclassType(Type baseClass, string subclassName) {
			if ( baseClass == null || string.IsNullOrEmpty(subclassName) ) {
				return null;
			}
			var subclasses = GetSubclasses(baseClass);
			if ( subclasses == null || subclasses.Length == 0 ) {
				return null;
			}
			foreach ( var sc in subclasses ) {
				if ( sc.Name == subclassName || sc.FullName == subclassName ) {
					return sc;
				}
			}
			return null;
		}

		public static object CreateSubclass(Type baseClass, string subclassName) {
			var scType = GetSubclassType(baseClass, subclassName);
			if ( scType == null ) {
				return null;
			}
			return CreateObjectWithActivator(scType);
		}

		public static Type[] GetSubclasses(Type type) {
			if ( type == null ) {
				return null;
			}
			if ( _subclassCache == null ) {
				_subclassCache = new Dictionary<Type, Type[]>();
			}
			if ( _subclassCache.ContainsKey(type) ) {
				return _subclassCache[type];
			}

			var types = GetAllTypes().Where(p => p.IsSubclassOf(type)).ToArray();
			_subclassCache.Add(type, types);
			return types;
		}

		public static FieldInfo[] GetFieldsWithAttribute(Type objectType, Type attributeType) {
			if ( objectType == null || attributeType == null ) {
				return Array.Empty<FieldInfo>();
			}

			var cacheKey = (objectType, attributeType);

			if ( _fieldsWithAttributeCache.TryGetValue(cacheKey, out var cachedFields) ) {
				return cachedFields;
			}

			var fields = objectType
				.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
				.Where(field => Attribute.IsDefined(field, attributeType))
				.ToArray();

			_fieldsWithAttributeCache[cacheKey] = fields;
			return fields;
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

		public static object CreateSubclassWithParametrizedConstructor(Type baseType, string subclassName, object[] constructorParams) {
			if ( baseType == null || string.IsNullOrEmpty(subclassName) ) {
				return null;
			}
			var sc = GetSubclassType(baseType, subclassName);
			if ( sc == null ) {
				return null;
			}
			return CreateWithParametrizedConstructor(sc, constructorParams);
		}

		public static object CreateWithParametrizedConstructor(Type type, object[] parameters) {
			if ( type == null ) {
				return null;
			}
			var types = new Type[parameters.Length];
			for ( int i = 0; i< parameters.Length; i++ ) {
				types[i] = parameters[i].GetType();
			}
			var c = type.GetConstructor(types);
			if ( c == null ) {
				return null;
			}
			return c.Invoke(parameters);
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
#if UNITY_2017_1_OR_NEWER
					UnityEngine.Debug.LogException( ex );
#else
					System.Console.WriteLine( ex.ToString() );
#endif
					return null;
				}
			} else {
				// Method with the specified name not found
#if UNITY_2017_1_OR_NEWER
				UnityEngine.Debug.LogWarning($"ReflectionUtility.CallMethod: Method '{methodName}' not found.");
#endif
				return null;
			}
		}
	}
}
