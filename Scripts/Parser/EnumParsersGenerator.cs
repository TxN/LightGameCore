using System.IO;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using SMGCore.Utils;
using System.Linq;
using System.Text;
using System;

namespace SMGCore.Parser {
	public static class EnumParsersGenerator {
		const string ResultFilePath = "Assets/Scripts/Utils/EnumParsers.cs";

		[MenuItem("Tools/Utils/Update enum parsers list")]
		public static void UpdateEnumTypeParsersList() {
			CreateParsersList(new List<string>() { "SubGame", "SMGCore" }, new List<string>() { "Assembly-CSharp" },
				ResultFilePath, "SMGCore.Parser");
			AssetDatabase.Refresh();
		}

		public static void CreateParsersList(List<string> allowedNamespaces, List<string> allowedAssemblies, string path, string resNamespace) {
			var types = ReflectionUtility.GetAllTypesNonCached(allowedAssemblies);
			AssetDatabase.DeleteAsset(path);
			GenerateGenericParsersList(resNamespace, path, types, allowedNamespaces);
			AssetDatabase.Refresh();
			Debug.Log("EnumParsers.cs update complete.");
		}

		public static void GenerateGenericParsersList(string ownNamespace, string filepath, Type[] allTypes, List<string> allowedNamespaces) {

			var codeFileString = @"using System.Collections.Generic;

using SMGCore.Parser;

namespace {0} {{
	public sealed class EnumParsers {{
		public static List<SimpleTypeParser> GetParsers() {{
			var parsers = new List<SimpleTypeParser>(256);
{1}
			return parsers;
		}}

		public static List<SimpleTypeSerializer> GetSerializers() {{
			var serializers = new List<SimpleTypeSerializer>(256);
{2}
			return serializers;
		}}
	}}
}}";
			var sb = new StringBuilder(32768);
			var sb2 = new StringBuilder(32768);
			var enums = allTypes.Where(t => t.IsSubclassOf(typeof(Enum)));
			foreach ( var t in enums ) {
				var serializerType = typeof(EnumTypeSerializer<>).MakeGenericType(t);
				if ( (t.Namespace != null) && !serializerType.ContainsGenericParameters ) {
					if ( t.IsVisible && allowedNamespaces.Any(p => t.Namespace.StartsWith(p)) ) {
						sb.Append($"			serializers.Add(new EnumTypeSerializer<{t.FullName?.Replace("+", ".")}>());\n");
					}
				}
				var parserType = typeof(EnumTypeParser<>).MakeGenericType(t);
				if ( (t.Namespace != null) && !parserType.ContainsGenericParameters ) {
					if ( t.IsVisible && allowedNamespaces.Any(p => t.Namespace.StartsWith(p)) ) {
						sb2.Append($"			parsers.Add(new EnumTypeParser<{t.FullName?.Replace("+", ".")}>());\n");
					}
				}
			}
			var file = File.CreateText(filepath);
			using ( file ) {
				file.Write(codeFileString, ownNamespace, sb2, sb);
				file.Close();
			}
		}


		[MenuItem("Tools/Utils/create parsers")]
		static void CreateParsers() {
			GenerateParserPair("TaskType", "SMGCore.Parser", "SMGCore");

			AssetDatabase.Refresh();
		}

		static void GenerateParserPair(string className, string classNamespace, string usingNamespace) {
			GenerateEnumParser(className, classNamespace, usingNamespace);
			GenerateEnumSerializer(className, classNamespace, usingNamespace);
		}

		static void GenerateEnumParser(string className, string classNamespace, string usingNamespace) {
			const string parserDir = "Assets/Scripts/Utils/TypeParsers/{0}TypeParser.cs";

			var codeFileString = @"using System;
using System.Collections.Generic;

using System;

using {2};

namespace {0} {{
	public sealed class {1}TypeParser :  SimpleTypeParser {{
		public override Type GetParserType() {{
			return typeof({1});
		}}

		public override object ParseValue(string value, string format = null) {{
			if ( string.IsNullOrEmpty(value) ) {{
				return default({1});
			}}

			{1}Factory.TryParse(value, out var result);
			return result;
		}}

	}}
}}";

			var result = string.Format(codeFileString, classNamespace, className, usingNamespace);

			if ( !string.IsNullOrEmpty(result) ) {
				File.WriteAllText(string.Format(parserDir, className), result);
			}
		}

		static void GenerateEnumSerializer(string className, string classNamespace, string usingNamespace) {
			const string serializerDir = "Assets/Scripts/Utils/TypeSerializers/{0}TypeSerializer.cs";

			var codeFileString = @"using System;
using System.Collections.Generic;

using System;

using {2};

namespace {0} {{
	public sealed class {1}TypeSerializer :  SimpleTypeSerializer {{
		public override Type GetSerializerType() {{
			return typeof({1});
		}}

		public override string SerializeValue(object value, string format) {{
			var result = {1}Factory.ConvertToString( ({1}) value);
			return result;
		}}

	}}
}}";

			var result = string.Format(codeFileString, classNamespace, className, usingNamespace);

			if ( !string.IsNullOrEmpty(result) ) {
				File.WriteAllText(string.Format(serializerDir, className), result);
			}
		}
	}
}
