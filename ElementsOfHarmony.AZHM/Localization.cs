using DrakharStudio.Engine;
using DrakharStudio.Engine.DataTypes;
using HarmonyLib;
using I2.Loc;

namespace ElementsOfHarmony.AZHM
{
	public static class Localization
	{
		public static void Exist()
		{
			var langs = LocalizationManager.GetAllLanguages(false);
			var langCodes = LocalizationManager.GetAllLanguagesCode(true, false);
			Log.Message($"langs count = {langs.Count}");
			foreach (var language in langs)
			{
				Log.Message(language);
			}
			Log.Message($"langs codes count = {langCodes.Count}");
			foreach (var language in langCodes)
			{
				Log.Message(language);
			}
		}

		[HarmonyPatch(typeof(LocalizationManager), "SetLanguageAndCode")]
		public static class DisplayLanguageAndCodePatch
		{
			public static void Postfix(string LanguageName, string LanguageCode, bool RememberLanguage, bool Force)
			{
				Log.Message($"LanguageName={LanguageName}");
				Log.Message($"LanguageCode={LanguageCode}");
				Log.Message($"RememberLanguage={RememberLanguage}");
				Log.Message($"Force={Force}");
				Log.Message(UnityEngine.StackTraceUtility.ExtractStackTrace());
			}
		}

		/*
		[HarmonyPatch(typeof(I2LanguageLoader), methodName: "LoadSystemLanguage")]
		public static class LoadSystemLanguagePatch
		{
			public static void Postfix(I2LanguageLoader __instance)
			{
			}
		}
		*/

		[HarmonyPatch(typeof(SystemLanguageAsset), methodName: "GetLanguage")]
		public static class LanguageMappingCorrection
		{
			public static void Prefix(I2LanguageLoader __instance)
			{
				var _languagePerSystemCode = Traverse.Create(__instance).Field<StringStringDictionary>("_languagePerSystemCode").Value;
				foreach (var language in _languagePerSystemCode)
				{
					Log.Message($"language, key={language.Key} value={language.Value}");
				}
				_languagePerSystemCode["zh-cn"] = "Chinese (Simplified)";
				_languagePerSystemCode["zh-sg"] = "Chinese (Simplified)";
				_languagePerSystemCode["zh-hans"] = "Chinese (Simplified)";
				_languagePerSystemCode["zh-hans-cn"] = "Chinese (Simplified)";
				_languagePerSystemCode["zh-hans-sg"] = "Chinese (Simplified)";

				_languagePerSystemCode["zh-tw"] = "Chinese (Traditional)";
				_languagePerSystemCode["zh-hk"] = "Chinese (Traditional)";
				_languagePerSystemCode["zh-hant"] = "Chinese (Traditional)";
				_languagePerSystemCode["zh-hant-tw"] = "Chinese (Traditional)";
				_languagePerSystemCode["zh-hant-hk"] = "Chinese (Traditional)";

				_languagePerSystemCode["zh"] = "Chinese (Simplified)";

				_languagePerSystemCode["ru"] = "Russian";
			}
		}
	}
}
