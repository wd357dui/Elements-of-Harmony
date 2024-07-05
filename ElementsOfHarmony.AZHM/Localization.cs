using DrakharStudio.Engine;
using DrakharStudio.Engine.Audio;
using DrakharStudio.Engine.DataTypes;
using HarmonyLib;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using static ElementsOfHarmony.Localization;

namespace ElementsOfHarmony.AZHM
{
	public static class Localization
	{
		// if we have a language override, use our override settings, otherwise we follow the game settings
		public static string TargetLanguage => !string.IsNullOrEmpty(Settings.OurSelectedLanguageOverride) ?
			Settings.OurSelectedLanguageOverride : LocalizationManager.CurrentLanguageCode;

		public static string TargetAudioLanguage => !string.IsNullOrEmpty(Settings.OurSelectedAudioLanguageOverride) ?
			Settings.OurSelectedAudioLanguageOverride : TargetLanguage;

		public static SortedDictionary<string, string> OriginalSupportedAudioLanguageCodeMapping = new SortedDictionary<string, string>()
		{	
			// these are case sensitive

			{ "en", "EN" }, // English
			{ "en-US", "EN" }, // English
			{ "en-GB", "EN" }, // English

			{ "es", "es" }, // Spanish (Spain)
			{ "es-ES", "es" }, // Spanish (Spain)

			{ "es-419", "las" }, // Spanish (Latin America)
			{ "es-US", // yeah but "US"? what are the developers thinking?
				"las" }, // Spanish (Latin America)

			{ "de", "de" }, // German

			{ "fr", "fr" }, // French

			{ "it", "it" }, // Italian
		};

		public static void Exist()
		{
			var langs = LocalizationManager.GetAllLanguages(false);
			var langCodes = LocalizationManager.GetAllLanguagesCode(true, false);
			Log.Message($"languages count = {langs.Count}");
			foreach (var language in langs)
			{
				Log.Message(language);
			}
			Log.Message($"languages codes count = {langCodes.Count}");
			foreach (var language in langCodes)
			{
				Log.Message(language);
			}
			OriginalSupportedLanguageList = langCodes.ToArray();
			if (!string.IsNullOrEmpty(Settings.OurSelectedAudioLanguageOverride))
			{
				LocalizationManager.CurrentLanguageCode = Settings.OurSelectedAudioLanguageOverride;
			}
		}

		[HarmonyPatch(typeof(SystemLanguageAsset), methodName: "GetLanguage")] // the game use this method to match system language on first start
		public static class LanguageMappingCorrection
		{
			public static void Prefix(I2LanguageLoader __instance)
			{
				var _languagePerSystemCode = Traverse.Create(__instance).Field<StringStringDictionary>("_languagePerSystemCode").Value;

				/* already fixed in base game
				 * https://steamcommunity.com/app/2235440/discussions/0/6761670113856426365/
				 * although they only fixed zh-cn and zh-tw
				
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
				*/

				// additional code
				string SystemLanguageCode = CultureInfo.CurrentCulture.Name;
				if (string.IsNullOrEmpty(Settings.OurSelectedLanguageOverride) &&
					OurSupportedLanguageList?.Any(L => L == SystemLanguageCode) == true)
				{
					if (!OriginalSupportedLanguageList.Contains(SystemLanguageCode))
					{
						Settings.OurSelectedLanguageOverride = SystemLanguageCode;
					}
				}
			}
		}

		[HarmonyPatch(typeof(LocalizationManager), "GetTranslation")] // the game use this method to fetch text translations,
																	  // or to map a "term" to a translated audio clip, just like the previous game
		public static class GetTranslationPatch
		{
			public static bool Prefix(string Term, ref string? __result)
			{
				if (!string.IsNullOrEmpty(Term))
				{
					bool Search(string TargetTerm, out string? __result)
					{
						__result = null;
						// search for the term in our translation list
						if (OurTranslations.TryGetValue(TargetLanguage, out SortedDictionary<string, string> Translations) &&
							Translations.TryGetValue(TargetTerm, out __result))
						{
							Log.Message($"Translation Matched! lang={TargetLanguage} term={Term} result={__result}");
							return true;
						}
						return false;
					}
					if (Search(Term, out __result))
					{
						return false;
					}
					if (TermFallback.TryGetValue(Term, out string FallbackTerm) && Search(Term, out __result))
					{
						Log.Message($"fallback term translation matched! lang={TargetLanguage} term={Term} FallbackTerm={FallbackTerm} result={__result}");
						return false;
					}
					// let the game search for the translation
					if (!Search("LanguageEnglishDisplayName", out string? LanguageEnglishDisplayName)) {
						LanguageEnglishDisplayName = LocalizationManager.GetLanguageFromCode(TargetLanguage);
					}
					if (LocalizationManager.TryGetTranslation(Term, out __result, overrideLanguage: LanguageEnglishDisplayName))
					{
						Log.Message($"game original translation matched, lang={TargetLanguage} term={Term} result={__result}");
						return false;
					}
					else
					{
						Log.Message($"text translation not matched, lang={TargetLanguage} term={Term} result={__result}");
					}
				}
				return true;
			}
			public static void Postfix(string Term, ref string __result)
			{
				Log.Message($"Translation result: lang={LocalizationManager.CurrentLanguageCode} term={Term} result={__result}");
			}
		}

		[HarmonyPatch(typeof(AudioManager), methodName: "get_Item")] // the game use this method to fetch audio translations
		public static class AudioManagerPatch
		{
			public static void Postfix(string i_id, ref AudioManagerElement __result)
			{
				if (OurAudioClips.TryGetValue(TargetAudioLanguage, out SortedDictionary<string, AudioClip> clips) &&
					clips.TryGetValue(i_id, out AudioClip localized))
				{
					Log.Message($"Audio translation matched, lang={TargetAudioLanguage} i_id={i_id}");
					__result._clip = localized;
					return;
				}
				
				if (!string.IsNullOrEmpty(Settings.OurSelectedAudioLanguageOverride))
				{
					lock (OriginalAudioClips)
					{
						if (OriginalAudioClips.TryGetValue(i_id, out AudioManagerItem item))
						{
							Log.Message($"Audio translation override matched, lang={TargetAudioLanguage} i_id={i_id}");
							__result = item.Select();
							return;
						}
					}
				}

				Log.Message($"audio translation not matched, lang={TargetAudioLanguage} i_id={i_id}");
			}

			internal static void ListAdd(AudioManagerItem[] items)
			{
				lock (OriginalAudioClips)
				{
					foreach (AudioManagerItem item in items)
					{
						OriginalAudioClips[item._id] = item;
						Log.Message($"audio translation added from original game assets, i_id={item._id}");
					}
				}
			}
			public static SortedDictionary<string, AudioManagerItem> OriginalAudioClips = new SortedDictionary<string, AudioManagerItem>();
		}

		[HarmonyPatch(typeof(AudioManager), methodName: "ListAdd", // the game use this method to load audio translations
			argumentTypes: new Type[] { typeof(string), typeof(bool) })]
		public static class ListAddPatch
		{
			public static void Prefix(string i_resourcePath, bool usingAddressables)
			{
				if (!string.IsNullOrEmpty(Settings.OurSelectedAudioLanguageOverride))
				{
					if (!OriginalSupportedAudioLanguageCodeMapping.TryGetValue(LocalizationManager.CurrentLanguageCode, out string CurrentCode))
					{
						CurrentCode = "EN";
					}
					OriginalSupportedAudioLanguageCodeMapping.TryGetValue(TargetAudioLanguage, out string TargetCode);
					if (i_resourcePath.Contains($"/{CurrentCode}/") &&
						i_resourcePath.Contains($"_{CurrentCode}."))
					{
						// get audio language assets from game
						string ResourcePathResult = i_resourcePath
							.Replace($"/{CurrentCode}/", $"/{TargetCode}/")
							.Replace($"_{CurrentCode}.", $"_{TargetCode}.");

						Log.Message($"Loading audios from original game assets, " +
							$"TargetAudioLanguage={TargetAudioLanguage} " +
							$"CurrentCode={CurrentCode} TargetCode={TargetCode} " +
							$"i_resourcePath={i_resourcePath} usingAddressables={usingAddressables} " +
							$"ResourcePathResult={ResourcePathResult} ");

						if (!usingAddressables)
						{
							// original code
							int num = ResourcePathResult.IndexOf("/Resources/");
							if (num > 0)
							{
								int length = ResourcePathResult.Length - 6 - 11 - num;
								string text = ResourcePathResult.Substring(num + 11, length);
								AudioManagerItemListDefinition audioManagerItemListDefinition = Resources.Load<AudioManagerItemListDefinition>(text);
								if (audioManagerItemListDefinition != null)
								{
									//ListAdd(audioManagerItemListDefinition._items); // original code
									AudioManagerPatch.ListAdd(audioManagerItemListDefinition._items);
									Resources.UnloadAsset(audioManagerItemListDefinition);
								}
							}
						}
						else
						{
							// modified from original code
							Singletons.Instance.resourcesMgr.AddAssetRequest("VOLATILE", ResourcePathResult,
								(AsyncOperationHandle operationHandle, ResourcesMgr mgr, AudioManagerItemListDefinition item, object userData) =>
								{
									AudioManagerPatch.ListAdd(item._items);
								});
							Singletons.Instance.resourcesMgr.StartToProcess();
						}
					}
				}
			}
		}
	}
}
