using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

using NUnit.Framework;

using SMGCore;
using SMGCore.EventSys;

using UnityEngine;
using UnityEngine.TestTools;

namespace SMGCore.Localization.Tests {
	public sealed class LocalizationTests {
		const string LocalePrefsKey = "Locale";

		const string SampleEnglishXml = @"<root>
  <CommonUI>
    <Loading text=""Loading..."" />
    <Title text=""Submarine"" />
  </CommonUI>
  <Hints>
    <One text=""Hint 1"" />
    <Two text=""Hint 2"" />
  </Hints>
</root>";

		const string SampleRussianXml = @"<root>
  <CommonUI>
    <Loading text=""Загрузка..."" />
    <Title text=""Подлодка"" />
  </CommonUI>
</root>";

		const string SampleChineseDraftXml = @"<root draft=""true"">
  <CommonUI>
    <Loading text=""加载中..."" />
  </CommonUI>
</root>";

		bool _hadSavedLocalePref;
		string _savedLocalePref;

		[SetUp]
		public void SetUp() {
			_hadSavedLocalePref = PlayerPrefs.HasKey(LocalePrefsKey);
			_savedLocalePref = _hadSavedLocalePref ? PlayerPrefs.GetString(LocalePrefsKey) : null;
			LocalizationController.Reset();
			LocalizationXmlUtility.ClearEditorCache();
			EventManager.ResetAll();
		}

		[TearDown]
		public void TearDown() {
			LocalizationController.Reset();
			LocalizationXmlUtility.ClearEditorCache();
			EventManager.ResetAll();
			if ( _hadSavedLocalePref ) {
				PlayerPrefs.SetString(LocalePrefsKey, _savedLocalePref);
			} else {
				PlayerPrefs.DeleteKey(LocalePrefsKey);
			}
			PlayerPrefs.Save();
		}

		static void InstallInMemoryResources(Dictionary<SystemLanguage, string> map, Action<List<SystemLanguage>> streamingScanner = null, Dictionary<SystemLanguage, string> streamingMap = null) {
			LocalizationController.ResourceXmlLoaderOverride = lang => map != null && map.TryGetValue(lang, out var xml) ? xml : null;
			LocalizationController.StreamingScannerOverride = streamingScanner ?? (_ => { });
			LocalizationController.StreamingXmlLoaderOverride = lang => streamingMap != null && streamingMap.TryGetValue(lang, out var xml) ? xml : null;
		}

		[Test]
		public void LocalizationTable_ParsesNestedXmlBothParentAndChildTextAndDraftAttribute() {
			const string xml = @"<root draft=""TRUE"">
  <!-- comment ignored -->
  <CommonUI>
    <Loading text=""Loading..."" />
    <MultiLine text=""Line1&#xA;Line2"" />
    <Empty text="""" />
    <Parent text=""ParentVal"">
      <Child text=""ChildVal"" />
    </Parent>
  </CommonUI>
</root>";

			var table = new LocalizationTable();
			var ok = table.LoadFromXml(xml, out var duplicates);

			Assert.IsTrue(ok);
			Assert.AreEqual(0, duplicates);
			Assert.IsTrue(table.IsDraft);
			Assert.AreEqual(4, table.Count);
			Assert.IsTrue(table.TryGet("CommonUI.Loading", out var loading));
			Assert.AreEqual("Loading...", loading);
			Assert.IsTrue(table.TryGet("CommonUI.MultiLine", out var multiLine));
			Assert.AreEqual("Line1\nLine2", multiLine);
			Assert.AreEqual("ParentVal", table.GetOrMissing("CommonUI.Parent"));
			Assert.AreEqual("ChildVal", table.GetOrMissing("CommonUI.Parent.Child"));
			Assert.IsFalse(table.ContainsKey("CommonUI.Empty"));
			Assert.AreEqual("!Empty!", table.GetOrMissing("CommonUI.Empty"));
		}

		[Test]
		public void LocalizationTable_EmptySelfClosingRootIsValid() {
			var table = new LocalizationTable();
			Assert.IsTrue(table.LoadFromXml("<root draft=\"true\" />", out var duplicates));
			Assert.AreEqual(0, duplicates);
			Assert.AreEqual(0, table.Count);
			Assert.IsTrue(table.IsDraft);
		}

		[Test]
		public void LocalizationTable_DuplicateKeyLogsErrorAndOverwrites() {
			const string xml = @"<root>
  <Sec>
    <Item text=""First"" />
    <Item text=""Second"" />
  </Sec>
</root>";

			LogAssert.Expect(LogType.Error, new Regex("Duplicate entries in loc file for 'Sec\\.Item'"));

			var table = new LocalizationTable();
			var ok = table.LoadFromXml(xml, out var duplicates);

			Assert.IsTrue(ok);
			Assert.AreEqual(1, duplicates);
			Assert.AreEqual("Second", table.GetOrMissing("Sec.Item"));
		}

		[Test]
		public void LocalizationTable_MissingRootOrMalformedXmlReturnsFalse() {
			var table = new LocalizationTable();
			Assert.IsFalse(table.LoadFromXml("<wrong><A text=\"1\" /></wrong>", out _));
			Assert.AreEqual(0, table.Count);

			LogAssert.Expect(LogType.Warning, new Regex("Failed to parse locale XML"));
			Assert.IsFalse(table.LoadFromXml("<root><Broken", out _));
			Assert.AreEqual(0, table.Count);
		}

		[Test]
		public void LocalizationTable_GetChildIdsClearsOutputAndCollectSymbols() {
			const string xml = @"<root>
  <Hints>
    <One text=""AB"" />
    <Two text=""BC"" />
  </Hints>
  <Other>
    <Item text=""Z"" />
  </Other>
</root>";

			var table = new LocalizationTable();
			Assert.IsTrue(table.LoadFromXml(xml, out _));

			var ids = new List<string> { "Stale.Entry" };
			table.GetChildIds("Hints.", ids);
			CollectionAssert.AreEquivalent(new[] { "Hints.One", "Hints.Two" }, ids);

			var symbols = new HashSet<char>();
			table.CollectSymbols(symbols);
			CollectionAssert.AreEquivalent(new[] { 'A', 'B', 'C', 'Z' }, symbols);
		}

		[Test]
		public void LocalizationTable_FormatMissingKeyHandlesNullEmptyAndDotted() {
			Assert.AreEqual(string.Empty, LocalizationTable.FormatMissingKey(null));
			Assert.AreEqual(string.Empty, LocalizationTable.FormatMissingKey(string.Empty));
			Assert.AreEqual("!Leaf!", LocalizationTable.FormatMissingKey("Section.Sub.Leaf"));
			Assert.AreEqual("!Single!", LocalizationTable.FormatMissingKey("Single"));
		}

		[Test]
		public void LocalizationConfig_ParsesOrderDefaultAndAliases() {
			const string xml = @"<root default=""Russian"">
  <language name=""English"" />
  <language name=""Russian"" />
  <language name=""Chinese"" aliases=""ChineseSimplified, ChineseTraditional"" />
</root>";

			var config = LocalizationConfig.ParseXml(xml, out var ok);

			Assert.IsTrue(ok);
			Assert.AreEqual(SystemLanguage.Russian, config.DefaultLanguage);
			CollectionAssert.AreEqual(
				new[] { SystemLanguage.English, SystemLanguage.Russian, SystemLanguage.Chinese },
				config.SupportedLanguages);
			Assert.AreEqual(SystemLanguage.Chinese, config.NormalizeAlias(SystemLanguage.ChineseSimplified));
			Assert.AreEqual(SystemLanguage.Chinese, config.NormalizeAlias(SystemLanguage.ChineseTraditional));
			Assert.AreEqual(SystemLanguage.Japanese, config.NormalizeAlias(SystemLanguage.Japanese));
		}

		[Test]
		public void LocalizationConfig_RejectsNumericInvalidDuplicatesAndFixesMissingDefault() {
			const string xml = @"<root default=""Japanese"">
  <language name=""999"" />
  <language name=""NotALanguage"" />
  <language name=""Russian"" />
  <language name=""Russian"" />
</root>";

			LogAssert.Expect(LogType.Warning, new Regex("Unsupported language name '999'"));
			LogAssert.Expect(LogType.Warning, new Regex("Unsupported language name 'NotALanguage'"));
			LogAssert.Expect(LogType.Warning, new Regex("Duplicate language 'Russian'"));
			LogAssert.Expect(LogType.Warning, new Regex("Default language 'Japanese' is not in SupportedLanguages"));

			var config = LocalizationConfig.ParseXml(xml, out var ok);

			Assert.IsTrue(ok);
			Assert.AreEqual(SystemLanguage.Russian, config.DefaultLanguage);
			CollectionAssert.AreEqual(new[] { SystemLanguage.Russian }, config.SupportedLanguages);
		}

		[Test]
		public void LocalizationConfig_MalformedXmlFallsBackToEnglish() {
			LogAssert.Expect(LogType.Warning, new Regex("Failed to parse XML config"));
			var config = LocalizationConfig.ParseXml("<root><Broken", out var ok);

			Assert.IsFalse(ok);
			Assert.AreEqual(SystemLanguage.English, config.DefaultLanguage);
			CollectionAssert.AreEqual(new[] { SystemLanguage.English }, config.SupportedLanguages);
		}

		[Test]
		public void Controller_CurrentLanguageDoesNotTriggerLocaleLoadWhileStatusPropertiesDo() {
			var loadCalls = 0;
			LocalizationController.Configure(new LocalizationConfig(
				SystemLanguage.English,
				new[] { SystemLanguage.English, SystemLanguage.Chinese }));
			LocalizationController.ResourceXmlLoaderOverride = lang => {
				loadCalls++;
				return lang == SystemLanguage.Chinese ? SampleChineseDraftXml : SampleEnglishXml;
			};
			LocalizationController.StreamingScannerOverride = _ => { };
			PlayerPrefs.SetString(LocalePrefsKey, SystemLanguage.Chinese.ToString());

			Assert.AreEqual(SystemLanguage.Chinese, LocalizationController.CurrentLanguage);
			Assert.AreEqual(0, loadCalls);

			// Accessing CurrentLocaleIsDraft must lazily load the locale instead of returning stale false.
			LogAssert.Expect(LogType.Warning, new Regex("Locale Chinese is marked as a draft"));
			Assert.IsTrue(LocalizationController.CurrentLocaleIsDraft);
			Assert.AreEqual(1, loadCalls);
			Assert.IsFalse(LocalizationController.LocaleLoadFailed);
			Assert.AreEqual(SystemLanguage.Chinese, LocalizationController.LoadedLanguage);
		}

		[Test]
		public void Controller_NullAndEmptyKeysAreSafe() {
			LocalizationController.Configure(new LocalizationConfig(
				SystemLanguage.English,
				new[] { SystemLanguage.English }));
			InstallInMemoryResources(new Dictionary<SystemLanguage, string> {
				{ SystemLanguage.English, SampleEnglishXml }
			});
			LocalizationController.ChangeLanguage(SystemLanguage.English);

			Assert.AreEqual(string.Empty, LocalizationController.Trans(null));
			Assert.AreEqual(string.Empty, LocalizationController.Trans(string.Empty));
			Assert.IsFalse(LocalizationController.CanTrans(null));
			Assert.IsFalse(LocalizationController.CanTrans(string.Empty));
			Assert.IsFalse(LocalizationController.TryTrans(null, out var t1));
			Assert.IsNull(t1);
			Assert.IsFalse(LocalizationController.TryTrans(string.Empty, out var t2));
			Assert.IsNull(t2);
			Assert.AreEqual("!MissingLeaf!", LocalizationController.Trans("NoSuchSection.MissingLeaf"));
		}

		[Test]
		public void Controller_TransFormatOverloadsAndInvalidFormatGuard() {
			LocalizationController.Configure(new LocalizationConfig(
				SystemLanguage.English,
				new[] { SystemLanguage.English }));
			InstallInMemoryResources(new Dictionary<SystemLanguage, string> {
				{ SystemLanguage.English, SampleEnglishXml }
			});
			LocalizationController.ChangeLanguage(SystemLanguage.English);

			LocalizationController.AddDynamicTranslation("Test.Fmt1", "A={0}", overwrite: true);
			LocalizationController.AddDynamicTranslation("Test.Fmt2", "A={0}, B={1}", overwrite: true);
			LocalizationController.AddDynamicTranslation("Test.Fmt3", "{0}-{1}-{2}", overwrite: true);
			LocalizationController.AddDynamicTranslation("Test.Fmt4", "{0}.{1}.{2}.{3}", overwrite: true);
			LocalizationController.AddDynamicTranslation("Test.BadFmt", "Broken {0} {3}", overwrite: true);

			Assert.AreEqual("A=1", LocalizationController.TransFormat("Test.Fmt1", "1"));
			Assert.AreEqual("A=1, B=2", LocalizationController.TransFormat("Test.Fmt2", "1", "2"));
			Assert.AreEqual("1-2-3", LocalizationController.TransFormat("Test.Fmt3", "1", "2", "3"));
			Assert.AreEqual("1.2.3.4", LocalizationController.TransFormat("Test.Fmt4", "1", "2", "3", "4"));

			LogAssert.Expect(LogType.Error, new Regex("Invalid format for key 'Test\\.BadFmt'"));
			Assert.AreEqual("Broken {0} {3}", LocalizationController.TransFormat("Test.BadFmt", "onlyOne"));
		}

		[Test]
		public void Controller_DynamicEntriesSurviveLanguageChangeAndEmptyRemovesOverlay() {
			LocalizationController.Configure(new LocalizationConfig(
				SystemLanguage.English,
				new[] { SystemLanguage.English, SystemLanguage.Russian }));
			InstallInMemoryResources(new Dictionary<SystemLanguage, string> {
				{ SystemLanguage.English, SampleEnglishXml },
				{ SystemLanguage.Russian, SampleRussianXml }
			});
			LocalizationController.ChangeLanguage(SystemLanguage.English);

			LocalizationController.AddDynamicTranslation("CommonUI.Title", "OverrideTitle", overwrite: true);
			Assert.IsTrue(LocalizationController.CanTrans("CommonUI.Title"));
			Assert.IsTrue(LocalizationController.TryTrans("CommonUI.Title", out var overridden));
			Assert.AreEqual("OverrideTitle", overridden);
			Assert.AreEqual("OverrideTitle", LocalizationController.Trans("CommonUI.Title"));

			// Dynamic overlay survives language switch.
			LocalizationController.ChangeLanguage(SystemLanguage.Russian);
			Assert.AreEqual("OverrideTitle", LocalizationController.Trans("CommonUI.Title"));

			// Empty dynamic text with overwrite:true removes the dynamic entry and falls back consistently to base table.
			LocalizationController.AddDynamicTranslation("CommonUI.Title", string.Empty, overwrite: true);
			Assert.IsTrue(LocalizationController.CanTrans("CommonUI.Title"));
			Assert.IsTrue(LocalizationController.TryTrans("CommonUI.Title", out var baseVal));
			Assert.AreEqual("Подлодка", baseVal);
			Assert.AreEqual("Подлодка", LocalizationController.Trans("CommonUI.Title"));

			// Empty dynamic text for a key absent from base table leaves key consistently missing.
			LocalizationController.AddDynamicTranslation("Temp.Missing", "Val", overwrite: true);
			LocalizationController.AddDynamicTranslation("Temp.Missing", string.Empty, overwrite: true);
			Assert.IsFalse(LocalizationController.CanTrans("Temp.Missing"));
			Assert.IsFalse(LocalizationController.TryTrans("Temp.Missing", out _));
			Assert.AreEqual("!Missing!", LocalizationController.Trans("Temp.Missing"));
		}

		[Test]
		public void Controller_FallbackMarksLocaleLoadFailedAndFiresLoadedLanguage() {
			LocalizationController.Configure(new LocalizationConfig(
				SystemLanguage.English,
				new[] { SystemLanguage.English, SystemLanguage.Icelandic }));
			InstallInMemoryResources(new Dictionary<SystemLanguage, string> {
				{ SystemLanguage.English, SampleEnglishXml }
			});

			var firedCount = 0;
			SystemLanguage firedLanguage = default;
			var watcher = new object();
			EventManager.Subscribe<Event_LanguageChanged>(watcher, e => {
				firedCount++;
				firedLanguage = e.Language;
			});

			LogAssert.Expect(LogType.Warning, new Regex("Cannot load locale Icelandic\\. Falling back to English"));

			LocalizationController.ChangeLanguage(SystemLanguage.Icelandic);

			Assert.IsTrue(LocalizationController.LocaleLoadFailed);
			Assert.AreEqual(SystemLanguage.Icelandic, LocalizationController.CurrentLanguage);
			Assert.AreEqual(SystemLanguage.English, LocalizationController.LoadedLanguage);
			Assert.AreEqual("Loading...", LocalizationController.Trans("CommonUI.Loading"));
			Assert.AreEqual(1, firedCount);
			Assert.AreEqual(SystemLanguage.English, firedLanguage);
		}

		[Test]
		public void Controller_StreamingAssetsPriorityFallbackAndReloadRescan() {
			var aliases = new Dictionary<SystemLanguage, SystemLanguage> {
				{ SystemLanguage.ChineseTraditional, SystemLanguage.Chinese }
			};
			LocalizationController.Configure(new LocalizationConfig(
				SystemLanguage.English,
				new[] { SystemLanguage.English, SystemLanguage.Chinese },
				aliases));

			var hasExternalTraditional = true;
			var streamingXml = new Dictionary<SystemLanguage, string> {
				{ SystemLanguage.ChineseTraditional, "<root><CommonUI><Loading text=\"繁體載入中\" /></CommonUI></root>" },
				{ SystemLanguage.Chinese, "<root><Broken" }
			};
			var resourcesXml = new Dictionary<SystemLanguage, string> {
				{ SystemLanguage.English, SampleEnglishXml },
				{ SystemLanguage.Chinese, SampleChineseDraftXml }
			};

			InstallInMemoryResources(
				resourcesXml,
				list => {
					list.Add(SystemLanguage.Chinese);
					if ( hasExternalTraditional ) {
						list.Add(SystemLanguage.ChineseTraditional);
					}
				},
				streamingXml);

			// Exact StreamingAssets match takes priority over alias.
			LocalizationController.ChangeLanguage(SystemLanguage.ChineseTraditional);
			Assert.AreEqual(SystemLanguage.ChineseTraditional, LocalizationController.CurrentLanguage);
			Assert.AreEqual(SystemLanguage.ChineseTraditional, LocalizationController.LoadedLanguage);
			Assert.AreEqual("繁體載入中", LocalizationController.Trans("CommonUI.Loading"));

			// Broken StreamingAssets file falls back cleanly to built-in Resources locale.
			LogAssert.Expect(LogType.Warning, new Regex("Failed to parse locale XML"));
			LogAssert.Expect(LogType.Error, new Regex("Cannot load locale Chinese from streaming assets"));
			LogAssert.Expect(LogType.Warning, new Regex("Locale Chinese is marked as a draft"));
			LocalizationController.ChangeLanguage(SystemLanguage.Chinese);
			Assert.IsFalse(LocalizationController.LocaleLoadFailed);
			Assert.IsTrue(LocalizationController.CurrentLocaleIsDraft);
			Assert.AreEqual("加载中...", LocalizationController.Trans("CommonUI.Loading"));

			// Removing external file + Reload() rescans StreamingAssets and falls back via alias.
			LocalizationController.ChangeLanguage(SystemLanguage.ChineseTraditional);
			hasExternalTraditional = false;
			LogAssert.Expect(LogType.Warning, new Regex("Failed to parse locale XML"));
			LogAssert.Expect(LogType.Error, new Regex("Cannot load locale Chinese from streaming assets"));
			LogAssert.Expect(LogType.Warning, new Regex("Locale Chinese is marked as a draft"));
			LocalizationController.Reload();
			Assert.AreEqual(SystemLanguage.Chinese, LocalizationController.CurrentLanguage);
			Assert.AreEqual(SystemLanguage.Chinese, LocalizationController.LoadedLanguage);
		}

		[Test]
		public void Controller_AliasNormalizationAndResetWithoutDomainReload() {
			var aliases = new Dictionary<SystemLanguage, SystemLanguage> {
				{ SystemLanguage.ChineseSimplified, SystemLanguage.Chinese }
			};
			var config = new LocalizationConfig(
				SystemLanguage.English,
				new[] { SystemLanguage.English, SystemLanguage.Chinese },
				aliases);
			var resources = new Dictionary<SystemLanguage, string> {
				{ SystemLanguage.English, SampleEnglishXml },
				{ SystemLanguage.Chinese, SampleChineseDraftXml }
			};

			LocalizationController.Configure(config);
			InstallInMemoryResources(resources);

			LogAssert.Expect(LogType.Warning, new Regex("Locale Chinese is marked as a draft"));
			LocalizationController.ChangeLanguage(SystemLanguage.ChineseSimplified);
			Assert.AreEqual(SystemLanguage.Chinese, LocalizationController.CurrentLanguage);
			Assert.AreEqual(SystemLanguage.Chinese, LocalizationController.LoadedLanguage);
			Assert.IsTrue(LocalizationController.CurrentLocaleIsDraft);

			LocalizationController.AddDynamicTranslation("Temp.Key", "TempVal", overwrite: true);
			Assert.IsTrue(LocalizationController.CanTrans("Temp.Key"));

			// Simulate Play Mode restart with Disable Domain Reload enabled.
			LocalizationController.Reset();
			PlayerPrefs.SetString(LocalePrefsKey, SystemLanguage.English.ToString());
			LocalizationController.Configure(config);
			InstallInMemoryResources(resources);

			Assert.IsFalse(LocalizationController.LocaleLoadFailed);
			Assert.IsFalse(LocalizationController.CurrentLocaleIsDraft);
			Assert.IsFalse(LocalizationController.CanTrans("Temp.Key"));
			Assert.AreEqual(SystemLanguage.English, LocalizationController.LoadedLanguage);
		}

		[Test]
		public void XmlUtility_CreateAndSavePreservesIndentationEncodedNewlinesAndNormalizesCrLf() {
			const string original = "<root>\r\n  <Sec>\r\n    <A text=\"Line1&#xA;Line2\" />\r\n  </Sec>\r\n</root>\r\n";
			var tempPath = Path.Combine(Path.GetTempPath(), $"smgcore_loc_test_{Guid.NewGuid():N}.xml");
			try {
				var doc = new XmlDocument { PreserveWhitespace = true };
				doc.LoadXml(original);
				LocalizationXmlUtility.SavePreservingFormat(doc, tempPath, useCrLf: true);
				Assert.AreEqual(original, File.ReadAllText(tempPath));

				// Add leaf with Windows CRLF inside existing section, plus a brand-new section via "root." prefix.
				LocalizationXmlUtility.CreateNewLocaleEntry("Sec.B", "Hello\r\nWorld", doc);
				LocalizationXmlUtility.CreateNewLocaleEntry("root.NewSec.Item", "Val", doc);
				LocalizationXmlUtility.SavePreservingFormat(doc, tempPath, useCrLf: true);

				const string expected =
					"<root>\r\n" +
					"  <Sec>\r\n" +
					"    <A text=\"Line1&#xA;Line2\" />\r\n" +
					"    <B text=\"Hello&#xA;World\" />\r\n" +
					"  </Sec>\r\n" +
					"  <NewSec>\r\n" +
					"    <Item text=\"Val\" />\r\n" +
					"  </NewSec>\r\n" +
					"</root>\r\n";

				Assert.AreEqual(expected, File.ReadAllText(tempPath));

				// Invalid paths must log error without throwing.
				LogAssert.Expect(LogType.Error, new Regex("invalid path"));
				LocalizationXmlUtility.CreateNewLocaleEntry("Bad..Path", "X", doc);
			} finally {
				if ( File.Exists(tempPath) ) {
					File.Delete(tempPath);
				}
			}
		}
	}
}
