using HarmonyLib;
using I2.Loc;
using Melbot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ElementsOfHarmony
{
	public class Localization
	{
		private static readonly object AudioClipMutex = new object();

		private static readonly SortedDictionary<string, AudioClip> OurAudioClips = new SortedDictionary<string, AudioClip>();
		private static readonly HashSet<UnityWebRequestAsyncOperation> AudioClipLoadingOperations = new HashSet<UnityWebRequestAsyncOperation>();
		private static readonly ManualResetEvent AudioClipsLoadedEvent = new ManualResetEvent(false);
		private static volatile bool AudioClipsLoaded = false;

		private static readonly SortedDictionary<string, SortedDictionary<string, string>> OurTranslations = new SortedDictionary<string, SortedDictionary<string, string>>();
		private static string[] OriginalSupportedLanguageList = null;
		private static string[] OurSupportedLanguageList = null;

		private static readonly Tuple<string, AudioType>[] OurSupportedAudioFormats = new Tuple<string, AudioType>[]{
			new Tuple<string, AudioType>( ".aiff", AudioType.AIFF ),
			new Tuple<string, AudioType>( ".ogg", AudioType.OGGVORBIS ),
			new Tuple<string, AudioType>( ".wav", AudioType.WAV )
		};

		public static void Init()
		{
			try
			{
				// empty the field that is partially responsible for removing the Russian language
				string[] excludedLanguages = Traverse.Create<I2LManager>().Field("excludedLanguages").GetValue<string[]>();
				excludedLanguages[excludedLanguages.IndexOf("ru")] = "";
			}
			catch { }

			try
			{
				// load our audio clip translations
				int numAudioClips = 0;
				void LoadAudioClips(string directory)
				{
					IEnumerable<string> files = Directory.EnumerateFiles(directory);
					foreach (string f in files)
					{
						foreach (Tuple<string, AudioType> format in OurSupportedAudioFormats)
						{
							if (f.EndsWith(format.Item1, StringComparison.InvariantCultureIgnoreCase))
							{
								UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip("file:///" + f, format.Item2);
								req.SendWebRequest().completed += AudioClipLoadComplete;
								numAudioClips++;
								break;
							}
						}
					}
				}
				void LoadAudioClipsRecursive(string directory)
				{
					IEnumerable<string> directories = Directory.EnumerateDirectories(directory);
					foreach (string currentDirectory in directories)
					{
						LoadAudioClips(currentDirectory);
					}
				}
				void AudioClipLoadComplete(AsyncOperation op)
				{
					UnityWebRequestAsyncOperation req = op as UnityWebRequestAsyncOperation;
					AudioClip clip = DownloadHandlerAudioClip.GetContent(req.webRequest);
					string term = req.webRequest.url;
					term = term.Replace('\\', '/');
					if (term.Contains("/"))
					{
						term = term.Substring(term.LastIndexOf("/") + 1);
					}
					if (term.Contains("."))
					{
						term = term.Remove(term.LastIndexOf("."));
					}
					lock (AudioClipMutex)
					{
						OurAudioClips[term] = clip;
						Log.Message("Audio clip loaded: " + term);
						AudioClipLoadingOperations.Remove(req);
						if (AudioClipLoadingOperations.Count == 0)
						{
							AudioClipsLoaded = true;
							AudioClipsLoadedEvent.Set();
						}
					}
				}
				// look for all audio files recursively in the "Elements of Harmony/AudioClip" folder and all its sub folders
				if (Directory.Exists("Elements of Harmony/AudioClip"))
				{
					LoadAudioClipsRecursive("Elements of Harmony/AudioClip");
					Log.Message($"{numAudioClips} audio clips queued for loading");
				}
				else
				{
					AudioClipsLoaded = true;
					AudioClipsLoadedEvent.Set();
				}
			}
			catch (Exception e)
			{
				Log.Message(e.StackTrace + "\n" + e.Message);
			}

			try
			{
				// load our text translations from the "Elements of Harmony/Translations" folder
				if (Directory.Exists("Elements of Harmony/Translations"))
				{
					IEnumerable<string> files = Directory.EnumerateFiles("Elements of Harmony/Translations");
					foreach (string f in files)
					{
						if (f.ToLower().EndsWith(".txt"))
						{
							// file name of the txt should be the ISO language code
							// the content should be tab-separated values (TSV)
							// where first column is the term (case sensitive)
							// and second column is the translated text (other columns will be ignored)
							// please also add your language code (as term) and your language name (as translated text)
							// so that your language display name can show up in the game menu
							string langCode = f.Remove(f.LastIndexOf("."));
							langCode = langCode.Replace("\\", "/");
							langCode = langCode.Substring(langCode.LastIndexOf("/") + 1);
							SortedDictionary<string, string> Translations = new SortedDictionary<string, string>();
							OurTranslations.Add(langCode, Translations);
							using (StreamReader reader = new StreamReader(f))
							{
								char[] tab = new char[] { '\t' };
								while (!reader.EndOfStream)
								{
									string line = reader.ReadLine();
									if (string.IsNullOrEmpty(line)) continue;
									if (line.Contains("\t"))
									{
										string[] pair = line.Split(tab, StringSplitOptions.RemoveEmptyEntries);
										if (pair.Length >= 2)
										{
											string term = pair[0];
											string text = pair[1].Replace("\\n", "\n");
											Translations.Add(term, text);
											Log.Message($"Translation added: term={term} value={text}");
										}
									}
								}
							}
							Log.Message($"language {langCode} loaded, {Translations.Count} translations");
						}
					}
					List<string> OurLanguageList = new List<string>();
					OurLanguageList.AddRange(OurTranslations.Keys);
					OurSupportedLanguageList = OurLanguageList.ToArray();
				}
				else
				{
					OurSupportedLanguageList = new string[0];
				}
			}
			catch (Exception e)
			{
				Log.Message(e.StackTrace + "\n" + e.Message);
			}

			try
			{
				// apply all of our patch procedures using Harmony API
				Harmony element = new Harmony($"{typeof(Localization).FullName}");
				if (ElementsOfHarmony.IsAMBA)
				{
					int Num = 0;
					foreach (var Patch in typeof(AMBA).GetNestedTypes())
					{
						element.CreateClassProcessor(Patch).Patch();
						Num++;
					}
					if (Num > 0)
					{
						Log.Message($"Harmony patch for {typeof(AMBA).FullName} successful - {Num} Patches");
					}
				}
				if (ElementsOfHarmony.IsAZHM)
				{
					int Num = 0;
					foreach (var Patch in typeof(AZHM).GetNestedTypes())
					{
						element.CreateClassProcessor(Patch).Patch();
						Num++;
					}
					if (Num > 0)
					{
						Log.Message($"Harmony patch for {typeof(AZHM).FullName} successful - {Num} Patches");
					}
				}
			}
			catch (Exception e)
			{
				Log.Message(e.StackTrace + "\n" + e.Message);
			}
		}

		public static class AMBA
		{
			// if we have a language override, use our override settings, otherwise we follow the game settings
			public static string TargetLanguage => string.IsNullOrEmpty(Settings.OurSelectedLanguageOverride) ? LocalizationManager.CurrentLanguageCode : Settings.OurSelectedLanguageOverride;

			[HarmonyPatch(typeof(SupportedLanguages))]
			[HarmonyPatch("GetSupportedLanguages")] // the game use this method to fetch a list of all supported languages
			public static class GetSupportedLanguagesPatch
			{
				public static bool Prefix(SupportedLanguages __instance, ref List<SupportedLanguageParams> __result)
				{
					var supportedLanguagesField = Traverse.Create(__instance).Field("supportedLanguages");
					SupportedLanguageParams[] supportedLanguages = supportedLanguagesField.GetValue<SupportedLanguageParams[]>();
					SupportedLanguageParams en_US = null;
					foreach (string langCode in OurSupportedLanguageList)
					{
						if (!supportedLanguages.Any(L => L.code == langCode))
						{
							if (en_US == null) en_US = supportedLanguages.First(L => L.code == "en-US");
							// our language was not present in the original supported languages list
							// so we clone a SupportedLanguageParams from en-US and change the language code to our's
							OurSupportedLanguageParams ourCurrentLang = new OurSupportedLanguageParams(en_US)
							{
								code = langCode, // this field alone does not reflect how the game determines the language code
								localizedAudio = true // this field does not reflect audio localization status, the game handles audio localization differently
							};
							if (ourCurrentLang.assetReference != null)
							{
								LanguageSourceAsset sourceAsset = ourCurrentLang.assetReference.Asset as LanguageSourceAsset;
								if (sourceAsset != null)
								{
									sourceAsset.name = "I2Languages_" + langCode; // this is where the game actually determines language code
									supportedLanguagesField.SetValue(supportedLanguages.Append(ourCurrentLang).ToArray());
									Log.Message("Language added " + langCode);
								}
							}
						}
					}
					__result = new List<SupportedLanguageParams>(supportedLanguages);
					return false;
				}

				public class OurSupportedLanguageParams :
					SupportedLanguageParams // I just want a copy constructor
				{
					public OurSupportedLanguageParams(SupportedLanguageParams source)
					{
						code = source.code;
						localizedAudio = source.localizedAudio;
						assetReference = source.assetReference;
					}
				}
			}

			[HarmonyPatch(typeof(LanguageSelector))]
			[HarmonyPatch("OnEnable")] // called when the language menu settings shows up
			public static class OnEnablePatch
			{
				public static bool Prefix(LanguageSelector __instance)
				{
					List<SupportedLanguageParams> supportedLanguageParams = NonPersistentSingleton<BaseSystem>.Get().i2LManager.supportedLanguages.GetSupportedLanguages();
					SupportedLanguageParams supportedLanguageParams2 = supportedLanguageParams.Find((SupportedLanguageParams x) => x.code == LocalizationManager.CurrentLanguageCode);
					string TargetLanguage = string.IsNullOrEmpty(Settings.OurSelectedLanguageOverride) ? LocalizationManager.CurrentLanguageCode : Settings.OurSelectedLanguageOverride;
					int index = supportedLanguageParams.FindIndex((SupportedLanguageParams x) => x.code == TargetLanguage);
					Traverse.Create(__instance).Field("index").SetValue(index);
					Traverse.Create(__instance).Method("ShowLangname", TargetLanguage).GetValue();
					// now the menu will show our override language instead of previous working language when player opens up language settings
					return false;
				}
			}

			[HarmonyPatch(typeof(LanguageSelector))]
			[HarmonyPatch("GetSupportedLanguageParams")]
			public static class GetSupportedLanguageParamsPatch
			{
				public static bool Prefix(ref List<SupportedLanguageParams> __result)
				{
					__result = NonPersistentSingleton<BaseSystem>.Get().i2LManager.supportedLanguages.GetSupportedLanguages();
					return false; // bypass the original method which was partially responsible for removing the Russian language
				}
			}

			[HarmonyPatch(typeof(LanguageSelector))]
			[HarmonyPatch("ShowLangname")] // the game calls this method to make language names appear/disapper in the menu when player moves the selection
			public static class ShowLangnamePatch
			{
				public static void Prefix(LanguageSelector __instance)
				{
					Transform rootTransform = __instance.langNames;
					Transform firstChild = null;
					List<string> Existed_Languages = new List<string>();
					foreach (Transform child in rootTransform)
					{
						// enumerate current languages
						if (firstChild == null) firstChild = child;
						Text text = child.gameObject.GetComponentInChildren<Text>(true);
						if (text != null)
						{
							Existed_Languages.Add(child.gameObject.name);
						}
					}
					foreach (string ourLang in OurSupportedLanguageList)
					{
						if (!Existed_Languages.Contains(ourLang))
						{
							// add our languge to the game objects
							// so that the name of our language can show up in the menu
							// otherwise our language name will just show as blank
							SortedDictionary<string, string> Translations = OurTranslations[ourLang];
							if (!Translations.TryGetValue(ourLang, out string ourLanguageName))
							{
								ourLanguageName = ourLang;
							}

							if (firstChild != null)
							{
								// duplicate a game object from an existing one and add it to the hierachy
								GameObject GameObjectForOurLang = UnityEngine.Object.Instantiate(firstChild.gameObject, rootTransform, false);
								if (GameObjectForOurLang != null)
								{
									// set our language code
									GameObjectForOurLang.name = ourLang;
									Text text = GameObjectForOurLang.GetComponentInChildren<Text>(true);
									if (text != null)
									{
										// set our language name
										text.text = ourLanguageName;
										Log.Message($"added our language {ourLang}'s display name: {ourLanguageName}");
									}
								}
							}
						}
					}
				}
			}

			[HarmonyPatch(typeof(LanguageSelector))]
			[HarmonyPatch("OnSelected")] // the game calls this method when player selects a language
			public static class OnSelectedPatch
			{
				public static void Prefix(LanguageSelector __instance)
				{
					int index = Traverse.Create(__instance).Field("index").GetValue<int>();
					List<SupportedLanguageParams> OurSupportedLanguages = NonPersistentSingleton<BaseSystem>.Get().i2LManager.supportedLanguages.GetSupportedLanguages();
					string SelectedLangCode = OurSupportedLanguages[index].code;
					bool wasOverride = !string.IsNullOrEmpty(Settings.OurSelectedLanguageOverride);
					if (!OriginalSupportedLanguageList.Contains(SelectedLangCode))
					{
						// if the language is not originally supported we will follow our override settings
						Settings.OurSelectedLanguageOverride = SelectedLangCode;
						Log.Message("language override: " + SelectedLangCode);
						LocalizationManager.CurrentLanguageCode = Settings.OurFallbackLanguage;
					}
					else
					{
						// but if the language is already originally supported then we will just follow the game settings
						Settings.OurSelectedLanguageOverride = "";
						Log.Message("language selected: " + SelectedLangCode);
					}
					if (wasOverride &&
						SelectedLangCode == LocalizationManager.CurrentLanguageCode)
					{
						// when player is switching back from our "override" language to a fallback language
						// the game won't start changing because the game thinks it's already in that language
						// so in order to fix this, we switch to a different language before the game checks the language settings
						foreach (string langCode in OriginalSupportedLanguageList)
						{
							if (langCode != LocalizationManager.CurrentLanguageCode)
							{
								LocalizationManager.CurrentLanguageCode = langCode;
								break;
							}
						}
					}
				}
			}

			[HarmonyPatch(typeof(LocalizationManager))]
			[HarmonyPatch("GetTranslation")] // the game use this method to fetch text translations and to map a "term" to a translated audio clip
			public static class GetTranslationPatch
			{
				public static bool Prefix(string Term, ref string __result, out string __state)
				{
					__state = LocalizationManager.CurrentLanguageCode; // save the current game language setting for Postfix function to recover to
					if (OriginalSupportedLanguageList == null)
					{
						// save a list of the game's original supported langauges if we haven't already
						SupportedLanguageParams[] OriginalSupportedLanguageParams = NonPersistentSingleton<BaseSystem>.Get().i2LManager.supportedLanguages.supportedLanguages;
						OriginalSupportedLanguageList = new string[OriginalSupportedLanguageParams.Length];
						bool isValid = false;
						for (int i = 0; i < OriginalSupportedLanguageParams.Length; i++)
						{
							OriginalSupportedLanguageList[i] = OriginalSupportedLanguageParams[i].code;
							if (Settings.OurFallbackLanguage == OriginalSupportedLanguageParams[i].code)
							{
								isValid = true;
							}
							Log.Message($"Originally supported language: {OriginalSupportedLanguageParams[i].code}");
						}
						if (!isValid)
						{
							// looks like our previous fallback language isn't valid, choose the first selected language instead
							Settings.OurFallbackLanguage = OriginalSupportedLanguageParams.FirstOrDefault()?.code ?? "en-US";
						}
					}
					if (!string.IsNullOrEmpty(Term))
					{
						string TargetLanguage = AMBA.TargetLanguage;
						if (OurTranslations.ContainsKey(TargetLanguage))
						{
							// search for the term in our translation list
							if (OurTranslations[TargetLanguage].TryGetValue(Term, out __result))
							{
								Log.Message($"Translation Matched! lang={TargetLanguage} term={Term} result={__result}");
								return false;
							}
						}
					}

					if (!string.IsNullOrEmpty(Settings.OurSelectedLanguageOverride))
					{
						// if we are overriding the language but failed to match the specified term in our translation list,
						// we will let the game fetch translation in fallback language
						LocalizationManager.CurrentLanguageCode = Settings.OurFallbackLanguage;
						Log.Message($"Override translation not matched, fallback to {Settings.OurFallbackLanguage}, term={Term}");
					}
					return true;
				}
				public static void Postfix(string Term, ref string __result, ref string __state)
				{
					LocalizationManager.CurrentLanguageCode = __state; // set the game language setting to what it was before
					Log.Message($"Translation result: lang={LocalizationManager.CurrentLanguageCode} term={Term} result={__result}");
				}
			}

			[HarmonyPatch(typeof(LocalizeTarget_UnityStandard_AudioSource))]
			[HarmonyPatch("DoLocalize")] // the game use this method to fetch translated audio clips
			public static class UnityStandardAudioSourcePatch
			{
				// "mainTranslation" is the term for the specified audio clip
				public static bool Prefix(LocalizeTarget_UnityStandard_AudioSource __instance, Localize cmp, string mainTranslation)
				{
					// code copied from the original method
					bool num = (__instance.mTarget.isPlaying || __instance.mTarget.loop) && Application.isPlaying;
					AudioClip clip = __instance.mTarget.clip;

					// original code:
					// AudioClip audioClip = cmp.FindTranslatedObject<AudioClip>(mainTranslation);

					#region my replacement code

					if (string.IsNullOrEmpty(mainTranslation))
					{
						return true;
					}
					if (!AudioClipsLoaded)
					{
						// if we haven't finished loading our audio clips, we wait until it does
						AudioClipsLoadedEvent.WaitOne();
					}
					bool AudioMatched;
					if (AudioMatched = OurAudioClips.TryGetValue(mainTranslation, out AudioClip audioClip))
					{
						Log.Message($"Audio: term matched - {mainTranslation}");
					}
					if (!AudioMatched)
					{
						// if we failed to match the specified audio clip, we look for it in the original game assets
						audioClip = cmp.FindTranslatedObject<AudioClip>(mainTranslation);
						AudioMatched = audioClip != null;
					}
					if (!AudioMatched)
					{
						// if we still didn't match an audio clip,
						// we change the language code in the "term" to English and try again (aka. language fallback)
						string fallback = mainTranslation.Replace(LocalizationManager.CurrentLanguageCode, "en-US");
						if (!string.IsNullOrEmpty(Settings.OurSelectedLanguageOverride))
						{
							fallback = fallback.Replace(Settings.OurSelectedLanguageOverride, "en-US");
						}
						audioClip = cmp.FindTranslatedObject<AudioClip>(fallback);
						AudioMatched = audioClip != null;
					}
					if (!AudioMatched)
					{
						// and if all of the above didn't work, we'll let the game handle it
						return true;
					}

					#endregion

					// code copied from the original method
					if (clip != audioClip)
					{
						__instance.mTarget.clip = audioClip;
					}
					if (num && (bool)__instance.mTarget.clip)
					{
						__instance.mTarget.Play();
					}
					return false;
				}
			}

			[HarmonyPatch(typeof(ControlsMenu))]
			[HarmonyPatch("SetAsCurrent")] // called when the controls menu settings shows up
			public static class ControlsMenuTextFix
			{
				// aquí me estoy arreglando bugs para Melbot lol
				public static void Postfix(ControlsMenu __instance)
				{
					Text[] ActiveTexts = __instance.GetComponentsInChildren<Text>(false);

					if (ActiveTexts.Find(T => T.name == "Text (TMP)") is Text ShownMovementText &&
						ActiveTexts.Find(T => T.name == "movement") is Text LocalizedMovementText)
					{
						// the text "Movement" on the upper left was not localized, this will fix it
						ShownMovementText.text = LocalizedMovementText.text;
					}
					else
					{
						Log.Message($"{typeof(ControlsMenuTextFix).FullName} - \"Text(TMP)\" or \"movement\" not found, this fix may be out of date");
					}

					if (ActiveTexts.Find(T => T.name == "title2players") is Text Shown2PlayersText)
					{
						// the text "2 Players" on the bottom left was not localized, this will fix it
						Shown2PlayersText.text = LocalizationManager.GetTranslation("Menu/P2");
					}
					else
					{
						Log.Message($"{typeof(ControlsMenuTextFix).FullName} - \"title2players\" not found, this fix may be out of date");
					}

					if (ActiveTexts.Find(T => T.name == "Text" && T.text == "Shift") is Text ShiftKeyText)
					{
						// the text "Shift" was not entirely shown (the last letter was missing), this will fix it
						ShiftKeyText.horizontalOverflow = HorizontalWrapMode.Overflow;
					}
					else
					{
						Log.Message($"{typeof(ControlsMenuTextFix).FullName} - \"Shift\" not found, this fix may be out of date");
					}
				}
			}

			[HarmonyPatch(typeof(ImageLanguageHandler))]
			[HarmonyPatch("SetImage")]
			public static class ImageLanguageHandlerPatch
			{
				public static bool Prefix(ImageLanguageHandler __instance)
				{
					List<LanguageSprite> sprites = Traverse.Create(__instance).Field("sprites").GetValue<List<LanguageSprite>>();
					Image image = Traverse.Create(__instance).Field("image").GetValue<Image>();

					void TryLoadLogoFile(LanguageSprite language)
					{
						if (LocalizationManager.GetTranslation("Menu/MLP_logo_file") is string LogoFileName)
						{
							LogoFileName = Path.Combine("Elements of Harmony/Translations", LogoFileName);
							Log.Message($"Trying to load localized logo file at {LogoFileName}");
							try
							{
								var Logo_Texture = new Texture2D(2, 2);
								if (Logo_Texture.LoadImage(File.ReadAllBytes(LogoFileName)))
								{
									language.sprite = Sprite.Create(Logo_Texture, language.sprite.rect, language.sprite.pivot);
								}
							}
							catch { }
						}
					}

					string TargetLanguage = AMBA.TargetLanguage;
					LanguageSprite languageSprite;
					if ((languageSprite = sprites.Find((LanguageSprite t) => t.languageCode == TargetLanguage)) != null)
					{
						if (languageSprite.languageCode != "en-US" && languageSprite.sprite.name.StartsWith("ENG_MLP_logo"))
						{
							TryLoadLogoFile(languageSprite);
						}

						image.sprite = languageSprite.sprite;
						Debug.Log("Logo Language: " + TargetLanguage);
						return false;
					}
					else
					{
						LanguageSprite en_US = sprites.First(T => T.languageCode == "en-US");
						LanguageSprite OurLanguage = new LanguageSprite()
						{
							languageCode = TargetLanguage,
							sprite = en_US.sprite,
						};
						Log.Message($"Creating new LanguageSprite for {TargetLanguage}");
						TryLoadLogoFile(OurLanguage);
						sprites.Add(OurLanguage);
						return false;
					}
				}
			}

			[HarmonyPatch(typeof(PreMenu))]
			[HarmonyPatch("OnData")] // called before menu is first loaded (I think)
			public static class PreMenuPatch
			{
				public static bool Prefix(PreMenu __instance)
				{
					// original code
					BaseSystem baseSystem = NonPersistentSingleton<BaseSystem>.Get();
					baseSystem.gameSession.previousScene = "MenuScene";
					baseSystem.gameSession.wearRollers = false;
					string text = NonPersistentSingleton<BaseSystem>.Get().i2LManager.Get2LetterISOCodeFromSystemLanguage();
					Debug.Log("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ Lang: " + text);

					/* original code
					if (text == "" || text == "ru") // <- saw this while looking for ways to fix text blurriness in menu so I'm replacing it
					{
						text = "en-US";
					}
					*/
					// replacement code
					if (string.IsNullOrEmpty(text))
					{
						text = "en-US";
					}

					// original code
					DataObject dataObject = Traverse.Create(__instance).Field("dataObject").GetValue<DataObject>();
					Dictionary<string, object> @object = dataObject.GetObject();
					if (@object.ContainsKey("lang") && !string.IsNullOrEmpty(@object["lang"].ToString()))
					{
						text = @object["lang"].ToString();
					}
					NonPersistentSingleton<BaseSystem>.Get().i2LManager.LoadI2L(text, () => Traverse.Create(__instance).Method("OnLoaded").GetValue());
					LocalizationManager.CurrentLanguage = NonPersistentSingleton<BaseSystem>.Get().i2LManager.GetSystemLanguage(text);
					return false;
				}
			}

			[HarmonyPatch(typeof(GlobalReferences))]
			[HarmonyPatch("SetCamera")]
			public static class GlobalReferencesCameraPatch
			{
				public static void Postfix(GlobalReferences __instance)
				{
					UniversalAdditionalCameraData CameraData = __instance.MainCamera.GetComponent<UniversalAdditionalCameraData>();
					if (CameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing) // this is causing the main menu's text blurry
					{
						CameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; // so we change it
					}
				}
			}
		}

		public static class AZHM
		{

		}
	}
}
