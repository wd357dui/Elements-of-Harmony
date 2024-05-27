using Character;
using HarmonyLib;
using I2.Loc;
using Melbot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static ElementsOfHarmony.Localization;

namespace ElementsOfHarmony.AMBA
{
	public static class Localization
	{
		// if we have a language override, use our override settings, otherwise we follow the game settings
		public static string TargetLanguage => !string.IsNullOrEmpty(Settings.OurSelectedLanguageOverride) ?
			Settings.OurSelectedLanguageOverride : LocalizationManager.CurrentLanguageCode;

		public static string TargetAudioLanguage => !string.IsNullOrEmpty(Settings.OurSelectedAudioLanguageOverride) ?
			Settings.OurSelectedAudioLanguageOverride : TargetLanguage;

		public static void Exist()
		{
			// empty the field that is partially responsible for removing the Russian language
			string[] excludedLanguages = Traverse.Create<I2LManager>().Field<string[]>("excludedLanguages").Value;
			excludedLanguages[Array.IndexOf(excludedLanguages, "ru")] = "";
		}

		[HarmonyPatch(typeof(SupportedLanguages), methodName: "GetSupportedLanguages")] // the game use this method to fetch a list of all supported languages
		public static class GetSupportedLanguagesPatch
		{
			public static bool Prefix(SupportedLanguages __instance, ref List<SupportedLanguageParams> __result)
			{
				var supportedLanguages = Traverse.Create(__instance).Field<SupportedLanguageParams[]>("supportedLanguages");
				foreach (string langCode in OurSupportedLanguageList!)
				{
					if (!supportedLanguages.Value.Any(L => L.code == langCode))
					{
						SupportedLanguageParams en_US = supportedLanguages.Value.First(L => L.code == "en-US");
						// our language was not present in the original supported languages list
						// so we clone a SupportedLanguageParams from en-US and change the language code to our's
						OurSupportedLanguageParams ourCurrentLang = new OurSupportedLanguageParams(en_US)
						{
							code = langCode, // this field alone does not reflect how the game determines the language code
							localizedAudio = true // this field does not reflect audio localization status, the game handles audio localization differently
						};
						if (ourCurrentLang.assetReference != null)
						{
							LanguageSourceAsset sourceAsset = (LanguageSourceAsset)ourCurrentLang.assetReference.Asset;
							if (sourceAsset != null)
							{
								sourceAsset.name = "I2Languages_" + langCode; // this is where the game actually determines language code
								supportedLanguages.Value = supportedLanguages.Value.Append(ourCurrentLang).ToArray();
								Log.Message("Language added " + langCode);
							}
						}
					}
				}
				__result = new List<SupportedLanguageParams>(supportedLanguages.Value);
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

		[HarmonyPatch(typeof(LanguageSelector), "OnEnable")] // called when the language menu settings shows up
		public static class OnEnablePatch
		{
			public static bool Prefix(LanguageSelector __instance)
			{
				List<SupportedLanguageParams> supportedLanguageParams = NonPersistentSingleton<BaseSystem>.Get().i2LManager.supportedLanguages.GetSupportedLanguages();
				int index = supportedLanguageParams.FindIndex((SupportedLanguageParams x) => x.code == TargetLanguage);
				Traverse.Create(__instance).Field<int>("index").Value = index;
				AccessTools.Method(typeof(LanguageSelector), "ShowLangname").Invoke(__instance, new object[] { TargetLanguage });
				// now the menu will show our override language instead of previous working language when player opens up language settings
				return false;
			}
		}

		[HarmonyPatch(typeof(LanguageSelector), "GetSupportedLanguageParams")]
		public static class GetSupportedLanguageParamsPatch
		{
			public static bool Prefix(ref List<SupportedLanguageParams> __result)
			{
				__result = NonPersistentSingleton<BaseSystem>.Get().i2LManager.supportedLanguages.GetSupportedLanguages();
				return false; // bypass the original method which was partially responsible for removing the Russian language
			}
		}

		[HarmonyPatch(typeof(LanguageSelector), "ShowLangname")] // the game calls this method to make language names appear/disapper
																 // in the menuwhen player moves the language selection
		public static class ShowLangnamePatch
		{
			public static void Prefix(LanguageSelector __instance)
			{
				Transform rootTransform = __instance.langNames;
				Transform? firstChild = null;
				List<string> Existed_Languages = new List<string>();
				foreach (Transform child in rootTransform)
				{
					// enumerate current languages
					firstChild ??= child;
					Text text = child.gameObject.GetComponentInChildren<Text>(true);
					if (text != null)
					{
						Existed_Languages.Add(child.gameObject.name);
					}
				}
				foreach (string ourLang in OurSupportedLanguageList!)
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

		[HarmonyPatch(typeof(LanguageSelector), "OnSelected")] // the game calls this method when player selects a language
		public static class OnSelectedPatch
		{
			public static void Prefix(LanguageSelector __instance, out int __state)
			{
				var index = Traverse.Create(__instance).Field<int>("index");
				List<SupportedLanguageParams> OurSupportedLanguages = NonPersistentSingleton<BaseSystem>.Get().i2LManager.supportedLanguages.GetSupportedLanguages();
				string SelectedLangCode = OurSupportedLanguages[index.Value].code;
				if (!OriginalSupportedLanguageList.Contains(SelectedLangCode))
				{
					// if the language is not originally supported we will follow our override settings
					Settings.OurSelectedLanguageOverride = SelectedLangCode;
					__state = index.Value;
					index.Value = 0;
					Log.Message("language override: " + SelectedLangCode);
				}
				else
				{
					// but if the language is already originally supported then we will just follow the game settings
					Settings.OurSelectedLanguageOverride = "";
					__state = index.Value;
					Log.Message("language selected: " + SelectedLangCode);
				}
				LocalizationManager.CurrentLanguageCode = OriginalSupportedLanguageList
						.First(L => L != OurSupportedLanguages[index.Value].code);
			}
			public static void Postfix(LanguageSelector __instance, int __state)
			{
				Traverse.Create(__instance).Field<int>("index").Value = __state;
			}
		}

		[HarmonyPatch(typeof(LocalizationManager), "GetTranslation")] // the game use this method to fetch text translations, or to map a "term" to a translated audio clip
		public static class GetTranslationPatch
		{
			public static bool Prefix(string Term, ref string? __result)
			{
				if (OriginalSupportedLanguageList == null)
				{
					// save a list of the game's original supported langauges if we haven't already
					SupportedLanguageParams[] OriginalSupportedLanguageParams = NonPersistentSingleton<BaseSystem>.Get().i2LManager.supportedLanguages.supportedLanguages;
					OriginalSupportedLanguageList = new string[OriginalSupportedLanguageParams.Length];
					for (int i = 0; i < OriginalSupportedLanguageParams.Length; i++)
					{
						OriginalSupportedLanguageList[i] = OriginalSupportedLanguageParams[i].code;
						Log.Message($"Originally supported language: {OriginalSupportedLanguageParams[i].code}");
					}
				}

				if (!string.IsNullOrEmpty(Term))
				{
					static bool Search(string TargetTerm, out string? __result)
					{
						__result = null;
						// search for the term in our translation list
						if (OurTranslations.TryGetValue(TargetLanguage, out SortedDictionary<string, string> Translations) &&
							Translations.TryGetValue(TargetTerm, out __result))
						{
							Log.Message($"Translation Matched! lang={TargetLanguage} term={TargetTerm} result={__result}");
							return true;
						}
						return false;
					}

					if (Search(Term, out __result))
					{
						return false;
					}
					if (TermFallback.TryGetValue(Term, out string FallbackTerm) && Search(FallbackTerm, out __result))
					{
						Log.Message($"fallback term translation matched! lang={TargetLanguage} term={Term} FallbackTerm={FallbackTerm} result={__result}");
						return false;
					}
					// let the game search for the translation
					if (LocalizationManager.TryGetTranslation(Term, out __result, overrideLanguage: TargetLanguage))
					{
						Log.Message($"game original translation matched, lang={TargetLanguage} term={Term} result={__result}");
						return false;
					}
				}
				return true;
			}
			public static void Postfix(string Term, ref string __result)
			{
				Log.Message($"Translation result: lang={LocalizationManager.CurrentLanguageCode} term={Term} result={__result}");
				if (Term.StartsWith("Audio") && !string.IsNullOrEmpty(__result) &&
					!string.IsNullOrEmpty(Settings.OurSelectedAudioLanguageOverride))
				{
					__result = __result
						.Replace($"_{TargetLanguage}", $"_{TargetAudioLanguage}")
						.Replace("_en-US", $"_{TargetAudioLanguage}");
					Log.Message($"Applying audio language override, result={__result}");
				}
			}
		}

		[HarmonyPatch(typeof(LocalizeTarget_UnityStandard_AudioSource), "DoLocalize")] // the game use this method to fetch translated audio clips
		public static class UnityStandardAudioSourcePatch
		{
			// `mainTranslation` is the term string for the requested audio clip
			public static bool Prefix(LocalizeTarget_UnityStandard_AudioSource __instance, Localize cmp, string mainTranslation)
			{
				if (string.IsNullOrEmpty(mainTranslation))
				{
					return true;
				}

				// code copied from the original method
				bool num = (__instance.mTarget.isPlaying || __instance.mTarget.loop) && Application.isPlaying;
				AudioClip clip = __instance.mTarget.clip;

				// original code:
				// AudioClip audioClip = cmp.FindTranslatedObject<AudioClip>(mainTranslation);

				#region my replacement code

				if (!AudioClipsLoaded)
				{
					// if our audio clips haven't finished loading, we wait until they do
					AudioClipsLoadedEvent.Wait();
				}

				AudioClip? audioClip = null;
				if (OurAudioClips.TryGetValue(TargetAudioLanguage, out SortedDictionary<string, AudioClip> clips) &&
					clips.TryGetValue(mainTranslation, out audioClip))
				{
					Log.Message($"Audio: term matched - {mainTranslation}");
				}

				if (audioClip == null)
				{
					// if we failed to match the specified audio clip, we look for it in the original game assets
					audioClip = cmp.FindTranslatedObject<AudioClip>(mainTranslation);
					if (audioClip != null)
					{
						Log.Message($"Audio: term matched in game's original assets - {mainTranslation}");
					}
				}
				if (audioClip == null)
				{
					// if we still didn't match an audio clip,
					// we change the language code in the "term" to English and try again (aka. language fallback)
					string fallback = mainTranslation.Replace(LocalizationManager.CurrentLanguageCode, "en-US");
					if (!string.IsNullOrEmpty(Settings.OurSelectedLanguageOverride))
					{
						fallback = fallback.Replace($"_{TargetAudioLanguage}", "_en-US");
					}
					audioClip = cmp.FindTranslatedObject<AudioClip>(fallback);
					if (audioClip != null)
					{
						Log.Message($"Audio: term matched for English fallback - {fallback}");
					}
				}
				if (audioClip == null)
				{
					Log.Message($"Audio: term not matched!! - {mainTranslation}");
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

		[HarmonyPatch(typeof(ControlsMenu), "SetAsCurrent")] // called when the controls menu settings shows up
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

		[HarmonyPatch(typeof(ImageLanguageHandler), "SetImage")] // called when the MLP logo is begin loaded in the main menu (on the top left)
		public static class ImageLanguageHandlerPatch
		{
			public static bool Prefix(ImageLanguageHandler __instance)
			{
				List<LanguageSprite> sprites = Traverse.Create(__instance).Field<List<LanguageSprite>>("sprites").Value;
				Image image = Traverse.Create(__instance).Field<Image>("image").Value;

				static void TryLoadLogoFile(LanguageSprite language)
				{
					if (LocalizationManager.GetTranslation("Menu/MLP_logo_file") is string LogoFileName)
					{
						LogoFileName = Path.Combine("Elements of Harmony/Assets/Localization/AMBA/", LogoFileName);
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

		[HarmonyPatch(typeof(PreMenu), "OnData")] // called before menu is first loaded (I think)
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
				if (text == "" || text == "ru") // <- saw this when I was looking for ways to fix text blurriness in menu so I'm replacing it
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
				DataObject dataObject = Traverse.Create(__instance).Field<DataObject>("dataObject").Value;
				Dictionary<string, object> @object = dataObject.GetObject();
				if (@object.ContainsKey("lang") && !string.IsNullOrEmpty(@object["lang"].ToString()))
				{
					text = @object["lang"].ToString();
				}
				NonPersistentSingleton<BaseSystem>.Get().i2LManager.LoadI2L(text,
					() => AccessTools.Method(typeof(PreMenu), "OnLoaded").Invoke(__instance, Array.Empty<object>()));
				LocalizationManager.CurrentLanguage = NonPersistentSingleton<BaseSystem>.Get().i2LManager.GetSystemLanguage(text);

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

				return false;
			}
		}

		[HarmonyPatch(typeof(GlobalReferences), "SetCamera")]
		public static class GlobalReferencesCameraPatch
		{
			public static void Postfix(GlobalReferences __instance)
			{
				UniversalAdditionalCameraData CameraData = __instance.MainCamera.GetComponent<UniversalAdditionalCameraData>();
				if (CameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing) // this is causing the main menu's text blurry
				{
					Log.Message("FXAA settings detected on main camera, changing to SMAA");
					CameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; // so we change it
				}
			}
		}

		[HarmonyPatch(typeof(Minigame), "MoveToNextPhaseIE")] // minigame has following phases: initialization, countdown, tutorial, gameplay, score
		public static class MinigameMoveToNextPhaseIEPatch
		{
			public static bool Prefix(Minigame __instance, ref IEnumerator __result)
			{
				var currentPhase = Traverse.Create(__instance).Field<MinigamePhase>("currentPhase");
				var nextPhase = currentPhase.Value?.Next;
				var nextNextPhase = currentPhase.Value?.Next?.Next;
				var nextNextNextPhase = currentPhase.Value?.Next?.Next?.Next;

				Action? AdditionalActionPre = null;
				Action? AdditionalActionPost = null;
				Func<bool>? WaitUntilCondition = null;

				if (nextPhase is CountDownPhase countDownPhase &&
					nextNextPhase is TutorialPhase tutorialPhase &&
					nextNextNextPhase is GameplayPhase gameplayPhase)
				{
					Log.Message("minigame starting, relocating the tutorial phase to before the countdown phase");
					MinigamePhase AfterTutorialAndCountdown = tutorialPhase.Next;
					Traverse.Create(currentPhase.Value!).Field<MinigamePhase>("nextState").Value = tutorialPhase;
					Traverse.Create(tutorialPhase).Field<MinigamePhase>("nextState").Value = countDownPhase;
					Traverse.Create(countDownPhase).Field<MinigamePhase>("nextState").Value = AfterTutorialAndCountdown;

					MinigameBase ActualGame = Traverse.Create(gameplayPhase).Field<MinigameBase>("minigameBase").Value;
					if (ActualGame is DashThroughTheSkyMiniGame ||
						ActualGame is Runner1MiniGame)
					{
						AdditionalActionPre = () =>
						{
							__instance.StartCoroutine((IEnumerator)
								AccessTools.Method(typeof(MinigameBase), "InitializeTutorialAndGame")
								.Invoke(ActualGame, Array.Empty<object>()));
						};
						if (ActualGame is DashThroughTheSkyMiniGame &&
							Traverse.Create(ActualGame).Field<bool>("flyingStarted") is Traverse<bool> flyingStarted)
						{
							WaitUntilCondition = () => flyingStarted.Value;
						}
						else if (ActualGame is Runner1MiniGame &&
							Traverse.Create(ActualGame).Field<bool>("runnerStarted") is Traverse<bool> runnerStarted)
						{
							WaitUntilCondition = () => runnerStarted.Value;

							/* debug: enable this code to start runner game in cinematic (story) mode
							AdditionalActionPost = () =>
							{
								var generator = (ActualGame as Runner1MiniGame).GetRunnerLevelGenerator();
								if (generator is RunnerLevelPredefinedGenerator predef)
								{
									Traverse.Create(predef).Field<PredefinedRoadData>("levelDefinedRoads").Value = predef.levelDefinedRoadsCinematic;
								}
							};
							*/
						}
					}
				}
				else if (currentPhase.Value is TutorialPhase &&
					nextPhase is CountDownPhase &&
					nextNextPhase is GameplayPhase gameplay)
				{
					MinigameBase ActualGame = Traverse.Create(gameplay).Field<MinigameBase>("minigameBase").Value;
					if (ActualGame is HerdingBunniesMinigame bunnies)
					{
						Log.Message("countdown is about to begin, load level for HerdingBunniesMinigame in advance");
						// so that it would not change in a sudden after "3, 2, 1, go"
						HerdingBunniesMinigamePatch.StartGameFirstHalf(bunnies);
					}
					else if (ActualGame is HerdingCrabsMinigame crabs)
					{
						Log.Message("countdown is about to begin, load level for HerdingCrabsMinigame in advance");
						// so that it would not change in a sudden after "3, 2, 1, go"
						HerdingCrabsMinigamePatch.StartGameFirstHalf(crabs);
					}
					else if (ActualGame is Runner1MiniGame runner)
					{
						runner.GetRunnerLevelGenerator().StartMovement();
					}
					MLPCharacterOnAxisEventPatch.Enabled = false;
					Log.Message($"disable character movement during countdown for fairness");
				}
				else if (currentPhase.Value is CountDownPhase &&
					nextPhase is GameplayPhase)
				{
					MLPCharacterOnAxisEventPatch.Enabled = true;
					Log.Message($"recover character movement after countdown");
				}
				__result = MoveToNext(__instance, currentPhase, AdditionalActionPre, WaitUntilCondition, AdditionalActionPost);
				return false;
			}
			public static IEnumerator MoveToNext(Minigame __instance, Traverse<MinigamePhase> currentPhase,
				Action? AdditionalActionPre, Func<bool>? WaitUntilCondition, Action? AdditionalActionPost)
			{
				yield return new WaitUntil(() => __instance.AdressablesLoaded());
				AdditionalActionPre?.Invoke();
				if (WaitUntilCondition != null)
				{
					yield return new WaitUntil(WaitUntilCondition);
				}
				AdditionalActionPost?.Invoke();
				currentPhase.Value.ExitPhase();
				currentPhase.Value = currentPhase.Value.Next;
				currentPhase.Value.EnterPhase();
			}
		}

		[HarmonyPatch(typeof(SequenceNode), "OnEnable")]
		public static class SequenceNodeRemoveRedundantTimers
		{
			public static void Postfix(SequenceNode __instance)
			{
				int DisableCameraNodeIndex = __instance.nodes.FindIndex(N => N is EnableGameObjectNode);
				int GoCountDownIndex = __instance.nodes.FindIndex(N => N is GoCountDownNode);
				if (GoCountDownIndex >= 0 && DisableCameraNodeIndex >= 0 &&
					GoCountDownIndex > DisableCameraNodeIndex)
				{
					int RedundantTimerIndex;
					do
					{
						TimerNode? RedundantTimer = null;
						RedundantTimerIndex = __instance.nodes.FindIndex(DisableCameraNodeIndex,
							N => (RedundantTimer = (TimerNode)N) != null &&
							RedundantTimer.total > 0.0f && RedundantTimer.total < 3.0f);
						if (RedundantTimerIndex >= 0 && RedundantTimer != null)
						{
							Log.Message($"Removing redundant timer for countdown phase, " +
								$"name={RedundantTimer.name} time={RedundantTimer.total}");
							__instance.nodes.RemoveAt(RedundantTimerIndex);
						}
					}
					while (RedundantTimerIndex >= 0);
				}
			}
		}

		[HarmonyPatch(typeof(MLPCharacter), "OnAxisEvent")]
		public static class MLPCharacterOnAxisEventPatch
		{
			public static bool Enabled = true;
			public static bool Prefix()
			{
				return Enabled;
			}
		}

		#region Hitch's Bunny Herding Training

		[HarmonyPatch(typeof(HerdingBunniesMinigame), "StartGameIE")]
		public static class HerdingBunniesMinigamePatch
		{
			public static void StartGameFirstHalf(HerdingBunniesMinigame __instance)
			{
				// first half of StartGameIE

				HerdingMinigames_GUI gui = Traverse.Create(__instance).Field<HerdingMinigames_GUI>("gui").Value;
				List<HerdingBunniesMinigame.BonusLimit> bonusLimits = Traverse.Create(__instance).Field<List<HerdingBunniesMinigame.BonusLimit>>("bonusLimits").Value;

				__instance.active = true;
				List<MinigamePJ> list = new List<MinigamePJ>();
				if (__instance.Mode == MinigamePlayersType.multiplayer)
				{
					GameSession gameSession = NonPersistentSingleton<BaseSystem>.Get().gameSession;
					list.Add(gameSession.GetPjFromPlayer(1));
					list.Add(gameSession.GetPjFromPlayer(2));
				}
				gui.SetGameMode(__instance.Mode, copp: true, list);
				gui.PauseTimer();
				Traverse.Create(__instance).Field<List<HerdingBunniesMinigame.BonusLimit>>("bonusLimits").Value =
					bonusLimits.OrderByDescending((HerdingBunniesMinigame.BonusLimit t) => t.numberOfBunnys).ToList();

				AccessTools.Method(typeof(HerdingBunniesMinigame), "RestartGame").Invoke(__instance, Array.Empty<object>());

				object Value = Traverse.Create(__instance).Field<Array>("roundsPrefabs").Value.GetValue(0);
				SetUpRoundFirstHalf(__instance, ref Value, 0);
				Traverse.Create(__instance).Field<Array>("roundsPrefabs").Value.SetValue(Value, 0);

				if (__instance.Mode == MinigamePlayersType.multiplayer)
				{
					Value = Traverse.Create(__instance).Field<Array>("roundsPrefabs").Value.GetValue(0);
					SetUpRoundFirstHalf(__instance, ref Value, 1);
					Traverse.Create(__instance).Field<Array>("roundsPrefabs").Value.SetValue(Value, 0);
				}
			}

			public static bool Prefix(HerdingBunniesMinigame __instance, ref IEnumerator __result)
			{
				HerdingMinigames_GUI gui = Traverse.Create(__instance).Field<HerdingMinigames_GUI>("gui").Value;
				MinigamePhase minigamePhase = Traverse.Create(__instance).Field<MinigamePhase>("minigamePhase").Value;

				// finish up previous calls
				SetUpRoundSecondHalf(__instance, true);

				// second half of StartGameIE

				__result = EnumerateCoroutine(__instance);

				gui.ShowGUI();
				minigamePhase.minigame.SetAddressablesLoading(bLoading: false);
				if (!__instance.audioSource.isPlaying)
				{
					__instance.audioSource.loop = true;
					__instance.audioSource.Play();
				}
				Traverse.Create(__instance).Field<List<HerdingBunniesCharacter>>("herdingCharacters").Value =
					(List<HerdingBunniesCharacter>)AccessTools.Method(typeof(HerdingBunniesMinigame), "GetHerdingCharacters")
					.Invoke(__instance, Array.Empty<object>());
				return false;
			}

			public static IEnumerator EnumerateCoroutine(HerdingBunniesMinigame __instance)
			{
				yield return new WaitUntil(() => Traverse.Create(__instance).Field<bool>("bunniesInitialized").Value);
			}

			public static void SetUpRoundFirstHalf(HerdingBunniesMinigame __instance, ref object info, int nPlayer)
			{
				MinigamePhase minigamePhase = Traverse.Create(__instance).Field<MinigamePhase>("minigamePhase").Value;
				YardSet yards = Traverse.Create(__instance).Field<YardSet>("yards").Value;

				((GameplayPhase)minigamePhase).setShotScreen.GoOut();
				Traverse.Create(__instance).Field<HerdingMinigames_Positions>("occupiedPositions").Value = 0;

				AccessTools.Method(typeof(HerdingBunniesMinigame), "InstantiateYard").Invoke(__instance, new object[] { nPlayer });

				object[] Args = new object[]
				{
						info, nPlayer
				};

				if (nPlayer == 0)
				{
					AccessTools.Method(typeof(HerdingBunniesMinigame), "InstantiateBunnies").Invoke(__instance, Args);
				}
				AccessTools.Method(typeof(HerdingBunniesMinigame), "InstantiateBunnyHoles").Invoke(__instance, Args);
				AccessTools.Method(typeof(HerdingBunniesMinigame), "InstantiateMud").Invoke(__instance, Args);
				AccessTools.Method(typeof(HerdingBunniesMinigame), "InstantiatePumpkins").Invoke(__instance, Args);
				info = Args[0];

				MLPCharacter mLPCharacter = minigamePhase.minigame.Characters[0];
				((HerdingBunniesStates)mLPCharacter.bodyController.states).Walk();
				mLPCharacter.transform.rotation = yards.yard.transform.rotation;
				mLPCharacter.transform.position = yards.yard.transform.position + yards.yard.transform.forward * 1f - yards.yard.transform.right * 1f;
				if (__instance.Mode == MinigamePlayersType.multiplayer)
				{
					MLPCharacter mLPCharacter2 = minigamePhase.minigame.Characters[1];
					((HerdingBunniesStates)mLPCharacter2.bodyController.states).Walk();
					mLPCharacter2.transform.rotation = yards.yard.transform.rotation;
					mLPCharacter2.transform.position = yards.yard.transform.position + yards.yard.transform.forward * 1f + yards.yard.transform.right * 1f;
				}
			}

			public static void SetUpRoundSecondHalf(HerdingBunniesMinigame __instance, bool firstRound = false)
			{
				HerdingMinigames_GUI gui = Traverse.Create(__instance).Field<HerdingMinigames_GUI>("gui").Value;
				SequenceNode initPhaseSequenceNode = __instance.initPhaseSequenceNode;

				gui.ShowGUI();
				gui.SetScore(0, __instance.Scores[0].Value);
				gui.InitializeTimerAnimation((int)Traverse.Create(__instance).Field<float>("roundsDuration").Value);

				AccessTools.Method(typeof(HerdingBunniesMinigame), "StartTime").Invoke(__instance, Array.Empty<object>());

				if (!firstRound)
				{
					gui.ShowGoSign(1.5f);
					initPhaseSequenceNode.ResetNode();
				}
			}
		}

		#endregion

		#region Bunny and Crab Herding Classic

		[HarmonyPatch(typeof(HerdingCrabsMinigame), "StartGameIE")]
		public static class HerdingCrabsMinigamePatch
		{
			public static void StartGameFirstHalf(HerdingCrabsMinigame __instance)
			{
				// first half of StartGameIE

				HerdingMinigames_GUI gui = Traverse.Create(__instance).Field<HerdingMinigames_GUI>("gui").Value;
				List<HerdingCrabsMinigame.BonusLimit> crabsBonusLimits = Traverse.Create(__instance).Field<List<HerdingCrabsMinigame.BonusLimit>>("crabsBonusLimits").Value;
				List<HerdingCrabsMinigame.BonusLimit> bunniesBonusLimits = Traverse.Create(__instance).Field<List<HerdingCrabsMinigame.BonusLimit>>("bunniesBonusLimits").Value;
				MinigamePhase minigamePhase = Traverse.Create(__instance).Field<MinigamePhase>("minigamePhase").Value;

				Traverse.Create(__instance).Field<bool>("active").Value = true;
				List<MinigamePJ> list = new List<MinigamePJ>();
				if (__instance.Mode == MinigamePlayersType.multiplayer)
				{
					GameSession gameSession = NonPersistentSingleton<BaseSystem>.Get().gameSession;
					list.Add(gameSession.GetPjFromPlayer(1));
					list.Add(gameSession.GetPjFromPlayer(2));
				}
				gui.SetGameMode(__instance.Mode, copp: true, list);
				gui.PauseTimer();
				Traverse.Create(__instance).Field<List<HerdingCrabsMinigame.BonusLimit>>("crabsBonusLimits").Value = 
					crabsBonusLimits.OrderByDescending((HerdingCrabsMinigame.BonusLimit t) => t.numberOfAnimals).ToList();
				Traverse.Create(__instance).Field<List<HerdingCrabsMinigame.BonusLimit>>("bunniesBonusLimits").Value =
					bunniesBonusLimits.OrderByDescending((HerdingCrabsMinigame.BonusLimit t) => t.numberOfAnimals).ToList();

				AccessTools.Method(typeof(HerdingCrabsMinigame), "RestartGame").Invoke(__instance, Array.Empty<object>());

				object Value = Traverse.Create(__instance).Field<Array>("roundsPrefabs").Value.GetValue(0);
				SetUpRoundFirstHalf(__instance, ref Value);
				Traverse.Create(__instance).Field<Array>("roundsPrefabs").Value.SetValue(Value, 0);
			}

			public static bool Prefix(HerdingCrabsMinigame __instance, ref IEnumerator __result)
			{
				HerdingMinigames_GUI gui = Traverse.Create(__instance).Field<HerdingMinigames_GUI>("gui").Value;
				MinigamePhase minigamePhase = Traverse.Create(__instance).Field<MinigamePhase>("minigamePhase").Value;

				// finish up previous calls
				SetUpRoundSecondHalf(__instance, true);

				// second half of StartGameIE

				__result = EnumerateCoroutine(__instance);

				gui.ShowGUI();
				minigamePhase.minigame.SetAddressablesLoading(bLoading: false);
				if (!__instance.audioSource.isPlaying)
				{
					__instance.audioSource.loop = true;
					__instance.audioSource.Play();
				}
				Traverse.Create(__instance).Field<List<HerdingCrabsCharacter>>("herdingCharacters").Value =
					(List<HerdingCrabsCharacter>)AccessTools.Method(typeof(HerdingCrabsMinigame), "GetHerdingCharacters")
					.Invoke(__instance, Array.Empty<object>());
				return false;
			}

			public static IEnumerator EnumerateCoroutine(HerdingCrabsMinigame __instance)
			{
				yield return new WaitUntil(() => Traverse.Create(__instance).Field<bool>("bunniesCrabsInitialized").Value);
			}

			public static void SetUpRoundFirstHalf(HerdingCrabsMinigame __instance, ref object info)
			{
				MinigamePhase minigamePhase = Traverse.Create(__instance).Field<MinigamePhase>("minigamePhase").Value;
				HerdingCrabs_Exits herdingCrabsExits = Traverse.Create(__instance).Field<HerdingCrabs_Exits>("herdingCrabsExits").Value;

				((GameplayPhase)minigamePhase).setShotScreen.GoOut();
				Traverse.Create(__instance).Field<HerdingMinigames_Positions>("occupiedPositions").Value = 0;

				AccessTools.Method(typeof(HerdingCrabsMinigame), "InstantiateExits").Invoke(__instance, Array.Empty<object>());

				object[] Args = new object[]
				{
						info,
				};

				AccessTools.Method(typeof(HerdingCrabsMinigame), "InstantiateBunnyHoles").Invoke(__instance, Args);
				AccessTools.Method(typeof(HerdingCrabsMinigame), "InstantiateMud").Invoke(__instance, Args);
				AccessTools.Method(typeof(HerdingCrabsMinigame), "InstantiatePumpkins").Invoke(__instance, Args);
				AccessTools.Method(typeof(HerdingCrabsMinigame), "InstantiateCrabs").Invoke(__instance, Args);
				AccessTools.Method(typeof(HerdingCrabsMinigame), "InstantiateBunnies").Invoke(__instance, Args);
				info = Args[0];

				int num = 0;
				foreach (MLPCharacter character in minigamePhase.minigame.Characters)
				{
					((HerdingCrabsStates)character.bodyController.states).Walk();
					character.GetComponent<HerdingCrabsCharacter>().CleanCrab();
					if (num == 0)
					{
						character.transform.rotation = herdingCrabsExits.BunnyYard.Entrance.transform.rotation;
						character.transform.position = herdingCrabsExits.BunnyYard.Entrance.transform.position + herdingCrabsExits.BunnyYard.Entrance.transform.forward * 1f - herdingCrabsExits.BunnyYard.Entrance.transform.right * 1f;
					}
					else
					{
						character.transform.rotation = herdingCrabsExits.BunnyYard.Entrance.transform.rotation;
						character.transform.position = herdingCrabsExits.BunnyYard.Entrance.transform.position + herdingCrabsExits.BunnyYard.Entrance.transform.forward * 1f + herdingCrabsExits.BunnyYard.Entrance.transform.right * 1f;
					}
					num++;
				}
			}

			public static void SetUpRoundSecondHalf(HerdingCrabsMinigame __instance, bool firstRound = false)
			{
				HerdingMinigames_GUI gui = Traverse.Create(__instance).Field<HerdingMinigames_GUI>("gui").Value;
				SequenceNode initPhaseSequenceNode = __instance.initPhaseSequenceNode;

				gui.ShowGUI();
				gui.InitializeTimerAnimation((int)Traverse.Create(__instance).Field<float>("roundsDuration").Value);
				AccessTools.Method(typeof(HerdingCrabsMinigame), "StartTime").Invoke(__instance, Array.Empty<object>());
				if (!firstRound)
				{
					gui.ShowGoSign(1.5f);
					initPhaseSequenceNode.ResetNode();
				}
			}
		}

		#endregion

		#region Pipp Pipp Dance Parade

		[HarmonyPatch(typeof(PlayerCombo), "StartGame")]
		public static class PlayerComboStartGamePatch
		{
			public static void Postfix(PlayerCombo __instance)
			{
				static GameObject? Find(GameObject obj, string name)
				{
					if (obj.name == name) return obj;
					foreach (Transform child in obj.transform)
					{
						if (Find(child.gameObject, name) is GameObject result)
						{
							return result;
						}
					}
					return null;
				}

				Log.Message("applying fix for dancing game");

				// set evaluation text active
				__instance.fashionShowCombo.smsPivote.gameObject.SetActive(true);

				// adjust emoji position
				GameObject emojis = Find(__instance.gameObject, "emojis")!;
				emojis.GetComponent<RectTransform>().position += new Vector3(0, 1.0f, 0);
			}
		}

		[HarmonyPatch(typeof(PlayerCombo), "OnSignalHit")]
		public static class OnSignalHitPatch
		{
			public static void Prefix(FashionShowCombo fashionShowCombo)
			{
				fashionShowCombo.sms.Rebind();
			}
		}

		#endregion

		#region Zipp's Flight Academy

		[HarmonyPatch(typeof(DashThroughTheSkyMiniGame), "StartGame")]
		public static class DashThroughTheSkyMiniGameStartPatch
		{
			public static void Prefix(DashThroughTheSkyMiniGame __instance)
			{
				Log.Message("fixing background music volume for DashThroughTheSkyMiniGame");
				__instance.audioSource.volume = 1.0f;
				__instance.audioSource.loop = true;
				__instance.audioSource.outputAudioMixerGroup = __instance.audioSource.outputAudioMixerGroup
					.audioMixer.FindMatchingGroups("Music").First();
			}
		}

		#endregion

		#region Sprout's Roller Blading Chase

		[HarmonyPatch(typeof(Runner1MiniGame), "StartGame")]
		public static class Runner1MiniGameStartGamePatch
		{
			public static Runner1MiniGame? previousInstance = null;
			public static bool Prefix(Runner1MiniGame __instance)
			{
				if (previousInstance == null)
				{
					previousInstance = __instance;
					if (RunnerCinematic.CheckCinematic(__instance))
					{
						__instance.StartCoroutine(RunnerCinematic.StartFirstDialog(__instance));
					}
					/* I've had enough of this fakeTimer, it's inconsistent every time no matter what I try
					 * 
					var fakeTimer = Traverse.Create(__instance).Field<float>("fakeTimer");
					fakeTimer.Value = float.PositiveInfinity; // cannot set value here directly
					__instance.StartCoroutine(FixFakeTimer(fakeTimer));
					*/
					return true;
				}
				else return false;
			}
			/*
			public static IEnumerator FixFakeTimer(Traverse<float> __instance)
			{
				// must wait for it to get assigned by some code first
				yield return new WaitUntil(() => !float.IsPositiveInfinity(__instance.Value));
				if (RunnerCinematic.IsCinematic)
				{
					// approximate duration is about 140 seconds or so
					__instance.Value += 40f;
				}
				else
				{
					// approximate duration is about 130 seconds or so
					__instance.Value += 30f;
				}
			}
			*/
		}

		[HarmonyPatch(typeof(Runner1MiniGame), "UpdateGame")]
		public static class Runner1MiniGameUpdatePatch
		{
			public const float SrcZ = 25.0f;
			public const float DstZ = 7.0f;
			public static void Postfix(Runner1MiniGame __instance)
			{
				if (__instance.Mode == MinigamePlayersType.adventure &&
					!Traverse.Create(__instance.GetRunnerLevelGenerator()).Field<bool>("bEnd").Value)
				{
					/* I've had enough of this fakeTimer, it's inconsistent every time no matter what I try
					 * 
					float fakeTimer = Traverse.Create(__instance).Field<float>("fakeTimer").Value;
					float Progress = fakeTimer / 100f;
					*/
					float Progress = 1.0f - (PredefinedGeneratorPatch.num / 120f);
					Progress = 1.0f - Mathf.Clamp(Progress, 0.0f, 1.0f);
					RunnerStates states = (RunnerStates)__instance.GetEnemy().GetComponent<BodyController>().states;
					states.racing.GoTo(new Vector3(0.0f, 0.0f, Mathf.Lerp(SrcZ, DstZ, Progress)));
				}
			}
		}

		[HarmonyPatch(typeof(Runner1MiniGame), "FinishGame")]
		public static class Runner1MiniGameFinishGamePatch
		{
			public static void Postfix()
			{
				Runner1MiniGameStartGamePatch.previousInstance = null;
				PredefinedGeneratorPatch.num = 0;
			}
		}

		[HarmonyPatch(typeof(RunnerLevelGenerator), "MoveEnemyFromTrigger")]
		public static class RunnerLevelGeneratorMoveEnemyFromTriggerPatch
		{
			public static bool Prefix(RunnerLevelGenerator __instance, int triggerAdd, bool finish)
			{
				bool bEnd = Traverse.Create(__instance).Field<bool>("bEnd").Value;
				if (!bEnd)
				{
					if (!finish)
					{
						var enemyMovePointPos = Traverse.Create(__instance).Field<int>("enemyMovePointPos");
						List<Transform> enemyMovePoints = Traverse.Create(__instance).Field<List<Transform>>("enemyMovePoints").Value;
						enemyMovePointPos.Value += triggerAdd;
						enemyMovePointPos.Value = Mathf.Clamp(enemyMovePointPos.Value, 0, enemyMovePoints.Count - 1);
						//racing.GoTo(enemyMovePoints[enemyMovePointPos].position); // original code
					}
					else
					{
						/* original code
						Transform enemyFinalPoint = Traverse.Create(__instance).Field("enemyFinalPoint").GetValue<Transform>();
						racing.GoTo(enemyFinalPoint.position);
						*/
					}
				}
				return false;
			}
		}

		[HarmonyPatch(typeof(RunnerLevelGenerator), "MoveEnemyToTrigger")]
		public static class RunnerLevelGeneratorMoveEnemyToTriggerPatch
		{
			public static bool Prefix(RunnerLevelGenerator __instance, int triggerAdd)
			{
				List<Transform> enemyMovePoints = Traverse.Create(__instance).Field<List<Transform>>("enemyMovePoints").Value;
				var enemyMovePointPos = Traverse.Create(__instance).Field<int>("enemyMovePointPos");
				enemyMovePointPos.Value = triggerAdd;
				enemyMovePointPos.Value = Mathf.Clamp(enemyMovePointPos.Value, 0, enemyMovePoints.Count - 1);
				/* original code
				racing.GoTo(enemyMovePoints[enemyMovePointPos].position);
				racing.StartTo(enemyMovePoints[enemyMovePointPos].position);
				*/
				return false;
			}
		}

		[HarmonyPatch(typeof(RunnerLevelGenerator), "ChangeVcamIE")]
		public static class RunnerLevelGeneratorChangeVcamIEPatch
		{
			public static bool Prefix(bool continueGame, bool returnToMainCam, ref Action action)
			{
				if (!RunnerCinematic.IsCinematic) return true;
				if (continueGame && returnToMainCam)
				{
					// trigger the corresponding dialog after this cutscene
					action += () => RunnerCinematic.PostCutscene(Runner1MiniGameStartGamePatch.previousInstance!);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(Runner1MiniGame), "TriggerObstacle")]
		public static class TriggerObstaclePatch
		{
			public static void Postfix(GameObject triggerObject, GameObject character)
			{
				if (!RunnerCinematic.IsCinematic) return;
				if (triggerObject.name.Contains("Bunny"))
				{
					if (character.name == "Enemy")
					{
						Runner1MiniGameStartGamePatch.previousInstance!.StartCoroutine(
							RunnerCinematic.StartSproutHitDialog(Runner1MiniGameStartGamePatch.previousInstance));
					}
					else
					{
						Runner1MiniGameStartGamePatch.previousInstance!.StartCoroutine(
							RunnerCinematic.StartSunnyHitDialog(Runner1MiniGameStartGamePatch.previousInstance));
					}
				}
			}
		}

		[HarmonyPatch(typeof(Runner1MiniGame), "TriggerRoadCinematic")]
		public static class TriggerRoadCinematicPatch
		{
			public static bool ShouldContinue = false;
			public static bool Prefix(Runner1MiniGame __instance, GameObject triggerObject, GameObject character, int triggerAdd)
			{
				if (!RunnerCinematic.IsCinematic) return true;
				if (!ShouldContinue)
				{
					// any running dialog should stop immediately so that this cutscene can start
					RunnerCinematic.NeedToStop = true;
					__instance.StartCoroutine(Trigger(__instance, triggerObject, character, triggerAdd));
					return false;
				}
				else
				{
					return ShouldContinue;
				}
			}
			public static IEnumerator Trigger(Runner1MiniGame __instance, GameObject triggerObject, GameObject character, int triggerAdd)
			{
				yield return new WaitUntil(() => !RunnerCinematic.IsDialogPlaying);
				ShouldContinue = true;
				AccessTools.Method(typeof(Runner1MiniGame), "TriggerRoadCinematic").Invoke(__instance, new object[] { triggerObject, character, triggerAdd });
				ShouldContinue = false;
				RunnerCinematic.NeedToStop = false;
			}
		}

		[HarmonyPatch(typeof(RunnerLevelPredefinedGenerator), "SpawnRoad")]
		public static class PredefinedGeneratorPatch
		{
			public static int num = 0;
			public static void Postfix(LevelRoad road)
			{
				Log.Message($"SpawnRoad, num={++num}, road.roadType={road.roadType}");
				if (num == 75 && road.roadType == LevelRoadType.cinematic)
				{
					Runner1MiniGameStartGamePatch.previousInstance!.StartCoroutine(
						RunnerCinematic.StartPreRoyalSistersDialog(Runner1MiniGameStartGamePatch.previousInstance));
				}
				else if (num == 117 && road.roadType == LevelRoadType.cinematic)
				{
					RunnerCinematic.StartNearFinishDialog(Runner1MiniGameStartGamePatch.previousInstance!);
				}
			}
		}

		public static class RunnerCinematic
		{
			public static bool IsCinematic = false;
			/// <summary>
			/// chech if current game mode is "Cinematic"
			/// </summary>
			public static bool CheckCinematic(Runner1MiniGame __instance)
			{
				RunnerLevelPredefinedGenerator predefined = (RunnerLevelPredefinedGenerator)__instance.GetRunnerLevelGenerator();
				return IsCinematic = ReferenceEquals(
					Traverse.Create(predefined).Field<PredefinedRoadData>("levelDefinedRoads").Value,
					predefined.levelDefinedRoadsCinematic);
			}

			public static bool HasPlayedSunnyHit = false;
			public static bool HasPlayedSproutHit = false;
			public static bool HasPlayedPostHitch = false;

			public static bool IsDialogPlaying = false;
			public static bool NeedToStop = false;
			public static IEnumerator StartDialog(Runner1MiniGame __instance, params MessageObject[] messages)
			{
				IsDialogPlaying = true;

				// I'm just gonna borrow a ShowMessageSequence
				ShowMessageSequence ShowMessage = __instance.gameObject.GetComponentInChildren<ShowMessageSequence>(true);
				ShowMessage.enabled = true;
				ShowMessage.gameObject.SetActive(true);

				// will give back soon
				MessageObject[] messagesBackup = ShowMessage.messageSequence.messages;

				var sequenceIndex = Traverse.Create(ShowMessage).Field<int>("sequenceIndex");
				var soundDuration = Traverse.Create(ShowMessage).Field<float>("soundDuration");

				Log.Message("dialog starting");

				sequenceIndex.Value = -1;
				ShowMessage.messageSequence.messages = messages;

				float delay = 0.0f, startTime = 0.0f;
				for (int i = 0; i < messages.Length; i++)
				{
					if (NeedToStop) break;
					Log.Message($"dialog={messages[i].textTerm}");
					ShowMessage.ShowNextMessage();
					startTime = Time.time;
					delay = soundDuration.Value;
					yield return new WaitWhile(() => (Time.time <= (startTime + delay)) && !NeedToStop);
				}

				Log.Message("dialog ending");

				// restore initial status
				PopupsUI popupsUI = ShowMessage.GetResource<PopupsUI>();
				popupsUI.popupAudio.audioSource?.Stop();
				popupsUI.popupHud.normalConversation.gameObject.SetActive(false);
				ShowMessage.messageSequence.messages = messagesBackup;
				sequenceIndex.Value = -1;

				IsDialogPlaying = false;
			}

			public static MessageObject AssembleDialogMessage(string term)
			{
				MessageObject message = ScriptableObject.CreateInstance<MessageObject>();
				message.textTerm = term;
				message.options = Array.Empty<LocalizedTerm>();
				message.AutoComplete();
				message.autoComplete = true;
				if (term.Contains("_Sprout_"))
				{
					message.title = SpeakerTitle.NONE; // using SpeakerTitle.SPROUT doesn't work (Sprout's name doesn't show up in subtitles)
					message.npcName = "Characters/Sprout_Name"; // so we use npcName instead
				}
				return message;
			}

			public static IEnumerator StartFirstDialog(Runner1MiniGame __instance)
			{
				// Sunny: "Sprout! Don't run, we can still fix all this."
				MessageObject MS_MR_EV_01_01_GP_SN_01 = AssembleDialogMessage("Mainstreet/MR/EV_01/MS_MR_EV_01_01_GP_SN_01");

				// Sprout: "Fix this!"
				MessageObject MS_MR_EV_01_02_GP_Sprout_01 = AssembleDialogMessage("Mainstreet/MR/EV_01/MS_MR_EV_01_02_GP_Sprout_01");

				yield return new WaitForSeconds(1f);
				__instance.StartCoroutine(StartDialog(__instance, MS_MR_EV_01_01_GP_SN_01, MS_MR_EV_01_02_GP_Sprout_01));
			}

			public static void StartPostHitchDialog(Runner1MiniGame __instance)
			{
				// Sunny: "I'll be careful!"
				MessageObject MS_MR_EV_02_02_GP_SN_01 = AssembleDialogMessage("Mainstreet/MR/EV_02/MS_MR_EV_02_02_GP_SN_01");

				__instance.StartCoroutine(StartDialog(__instance, MS_MR_EV_02_02_GP_SN_01));
				HasPlayedPostHitch = true;
			}

			public static IEnumerator StartSproutHitDialog(Runner1MiniGame __instance)
			{
				yield return new WaitForSeconds(0.6f);
				if (!IsDialogPlaying && !HasPlayedSproutHit && !NeedToStop && HasPlayedPostHitch)
				{
					// Sprout: "Where are they coming from!"
					MessageObject MS_MR_EV_02_05_GP_Sprout_02 = AssembleDialogMessage("Mainstreet/MR/EV_02/MS_MR_EV_02_05_GP_Sprout_02");

					__instance.StartCoroutine(StartDialog(__instance, MS_MR_EV_02_05_GP_Sprout_02));
					HasPlayedSproutHit = true;
				}
			}

			public static IEnumerator StartSunnyHitDialog(Runner1MiniGame __instance)
			{
				yield return new WaitForSeconds(0.6f);
				if (!IsDialogPlaying && !HasPlayedSunnyHit && !NeedToStop && HasPlayedPostHitch)
				{
					// Sunny: "Oh come on!"
					MessageObject MS_MR_EV_02_03_GP_SN_02 = AssembleDialogMessage("Mainstreet/MR/EV_02/MS_MR_EV_02_03_GP_SN_02");

					__instance.StartCoroutine(StartDialog(__instance, MS_MR_EV_02_03_GP_SN_02));
					HasPlayedSunnyHit = true;
				}
			}

			public static IEnumerator StartPreRoyalSistersDialog(Runner1MiniGame __instance)
			{
				NeedToStop = true;
				yield return new WaitUntil(() => !IsDialogPlaying);
				NeedToStop = false;

				// Sprout: "You'll never catch me!"
				MessageObject MS_MR_EV_03_01_GP_Sprout_01 = AssembleDialogMessage("Mainstreet/MR/EV_03/MS_MR_EV_03_01_GP_Sprout_01");

				__instance.StartCoroutine(StartDialog(__instance, MS_MR_EV_03_01_GP_Sprout_01));
			}

			public static void StartPostRoyalSistersDialog(Runner1MiniGame __instance)
			{
				// Sunny: "I won't let you down!"
				MessageObject MS_MR_EV_03_05_CS_SN_02 = AssembleDialogMessage("Mainstreet/MR/EV_03/MS_MR_EV_03_05_CS_SN_02");

				__instance.StartCoroutine(StartDialog(__instance, MS_MR_EV_03_05_CS_SN_02));
			}

			public static void StartNearFinishDialog(Runner1MiniGame __instance)
			{
				// Sunny: "I'm getting closer!"
				MessageObject MS_MR_EV_07_01_GP_SN_01 = AssembleDialogMessage("Mainstreet/MR/EV_07/MS_MR_EV_07_01_GP_SN_01");

				__instance.StartCoroutine(StartDialog(__instance, MS_MR_EV_07_01_GP_SN_01));
			}

			public static void PostCutscene(Runner1MiniGame __instance)
			{
				if (!HasPlayedPostHitch)
				{
					StartPostHitchDialog(__instance);
				}
				else
				{
					StartPostRoyalSistersDialog(__instance);
				}
			}
		}
		#endregion

		[HarmonyPatch(typeof(VolumeSlider), "SetValue")] // audio volume slider in the audio settings menu
		public static class VolumeSliderPatch
		{
			public static bool Prefix(VolumeSlider __instance, float sliderValue)
			{
				__instance.slider.value = Mathf.Clamp01(sliderValue);
				float num = Mathf.Log(1f + 9f * __instance.slider.value, 10f);
				__instance.percentage.text = (100f * __instance.slider.value).ToString("F0") + " %";
				if (__instance.name == "Music")
				{
					// allow music to have max volume of +20 db instead of -0 db
					num *= 1.2f;
				}
				__instance.audioMmixer.SetFloat(__instance.volumeParameter, (num - 1f) * 80f);
				__instance.audioMmixer.GetFloat(__instance.volumeParameter, out var _);
				return false;
			}
		}

		/*
		[HarmonyPatch(typeof(LocalizeTarget_UnityUI_Text), "DoLocalize")] // the game use this function to set font and text for UnityEngine.UI.Text
		public static class LocalizeTarget_UnityUI_Text_DoLocalizePatch
		{
			public static bool Prefix(LocalizeTarget_UnityUI_Text __instance, Localize cmp, string mainTranslation, string secondaryTranslation)
			{
				object[] Args = new object[]
				{
					mainTranslation, secondaryTranslation
				};
				Font secondaryTranslatedObj = TryGetFont(mainTranslation, secondaryTranslation, __instance.mTarget.font) ??
					(Font)AccessTools.Method(typeof(Localize), "GetSecondaryTranslatedObj")
					.MakeGenericMethod(typeof(Font))
					.Invoke(cmp, Args);
				mainTranslation = (string)Args[0];
				if (secondaryTranslatedObj != null && secondaryTranslatedObj != __instance.mTarget.font)
				{
					__instance.mTarget.font = secondaryTranslatedObj;
				}
				if (Traverse.Create(__instance).Field<bool>("mInitializeAlignment").Value)
				{
					Traverse.Create(__instance).Field<bool>("mInitializeAlignment").Value = false;
					Traverse.Create(__instance).Field<bool>("mAlignmentWasRTL").Value = LocalizationManager.IsRight2Left;
					Args = new object[]
					{
							Traverse.Create(__instance).Field<bool>("mAlignmentWasRTL").Value,
							__instance.mTarget.alignment,
							Traverse.Create(__instance).Field<TextAnchor>("mAlignment_LTR").Value,
							Traverse.Create(__instance).Field<TextAnchor>("mAlignment_RTL").Value,
					};
					AccessTools.Method(typeof(LocalizeTarget_UnityUI_Text), "InitAlignment").Invoke(__instance, Args);
					Traverse.Create(__instance).Field<TextAnchor>("mAlignment_LTR").Value = (TextAnchor)Args[2];
					Traverse.Create(__instance).Field<TextAnchor>("mAlignment_RTL").Value = (TextAnchor)Args[3];
				}
				else
				{
					Args = new object[]
					{
							Traverse.Create(__instance).Field<bool>("mAlignmentWasRTL").Value,
							__instance.mTarget.alignment,
							Traverse.Create(__instance).Field<TextAnchor>("mAlignment_LTR").Value,
							Traverse.Create(__instance).Field<TextAnchor>("mAlignment_RTL").Value,
					};
					AccessTools.Method(typeof(LocalizeTarget_UnityUI_Text), "InitAlignment").Invoke(__instance, Args);
					TextAnchor alignRTL = (TextAnchor)Args[2];
					TextAnchor alignLTR = (TextAnchor)Args[3];

					if ((Traverse.Create(__instance).Field<bool>("mAlignmentWasRTL").Value && Traverse.Create(__instance).Field<TextAnchor>("mAlignment_RTL").Value != alignRTL) ||
						(!Traverse.Create(__instance).Field<bool>("mAlignmentWasRTL").Value && Traverse.Create(__instance).Field<TextAnchor>("mAlignment_LTR").Value != alignLTR))
					{
						Traverse.Create(__instance).Field<TextAnchor>("mAlignment_LTR").Value = alignLTR;
						Traverse.Create(__instance).Field<TextAnchor>("mAlignment_RTL").Value = alignRTL;
					}
					Traverse.Create(__instance).Field<bool>("mAlignmentWasRTL").Value = LocalizationManager.IsRight2Left;
				}
				if (mainTranslation != null && __instance.mTarget.text != mainTranslation)
				{
					if (cmp.CorrectAlignmentForRTL)
					{
						__instance.mTarget.alignment = LocalizationManager.IsRight2Left ?
							Traverse.Create(__instance).Field<TextAnchor>("mAlignment_RTL").Value :
							Traverse.Create(__instance).Field<TextAnchor>("mAlignment_LTR").Value;
					}
					__instance.mTarget.text = mainTranslation;
					__instance.mTarget.SetVerticesDirty();
				}
				return false;
			}
			public static Font? TryGetFont(string translation, string font, Font existing)
			{
				try
				{
					if (string.IsNullOrEmpty(font)) return null;

					string fontFamilyName = new Regex(@"(\w+)").Matches(font)[0].Value;

					if (existing.fontNames.Exists(N => N.StartsWith(fontFamilyName, StringComparison.InvariantCultureIgnoreCase)))
					{
						return null;
					}

					TMP_FontAsset tmp = TMP_Settings.fallbackFontAssets.FirstOrDefault(F =>
					{
						return F.faceInfo.familyName.StartsWith(fontFamilyName, StringComparison.InvariantCultureIgnoreCase);
					});

					if (tmp != null)
					{
						tmp.TryAddCharacters(translation);

						if (tmp.HasCharacters(translation, out List<char> missing) || // all characters exists
							missing.All(C => !new Regex(@"\w").IsMatch($"{C}"))) // character not present, but is not a word anyways
						{
							Log.Message($"font matched, fontFamilyName={fontFamilyName}");
							return Font.CreateDynamicFontFromOSFont(fontFamilyName, 36);
						}
						else
						{
							// this extra check is here because
							// if we go ahead and set this font regardless of character graph availability
							// unity would use the same fallbacks from when no font is assigned (which is good)
							// but the font size would be changed (which is ugly)
							// 
							// so even though set a faulty font would not cause error
							// we still want to avoid doing it
							Log.Message($"font doesn't have some characters, fontFamilyName={fontFamilyName}, missing.Count={missing.Count} missing={new string(missing.ToArray())}");
						}
					}
					else
					{
						Log.Message($"font not found, fontFamilyName={fontFamilyName}");
					}
				}
				catch
				{
					Log.Message($"exception happened during TryGetFont");
				}
				return null;
			}
		}
		*/

		[HarmonyPatch(typeof(Volume), methodName: "profileRef", methodType: MethodType.Getter)]
		public static class VolumnProfile_profileRef_getter
		{
			public static void Postfix(Volume __instance, VolumeProfile __result)
			{
				if (__result.name.Contains("Pantano")) // "Swamp"
				{
					// if we just change the property right now, somewhere will trigger
					// "collection modified during iteration" exception,
					// so we wait for an appropriate time to do it
					__instance.StartCoroutine(DelaySetDisable(__instance));
				}
			}
			public static IEnumerator DelaySetDisable(Volume __instance)
			{
				yield return null; // wait for a right moment
				__instance.enabled = false; // now disable the green filter for swamp areas
			}
		}

		[HarmonyPatch(typeof(TutorialScreen), "Show")] // called when tutorial is showing
		public static class TutorialScreenShowPatch
		{
			public static bool Prefix()
			{
				TutorialScreenPatch.PlayAudio.Clear();
				return true;
			}
		}

		[HarmonyPatch(typeof(TutorialScreen), "IEShowTutorialStep")] // called when tutorial is showing
		public static class TutorialScreenPatch
		{
			public static Dictionary<string, bool> PlayAudio = new Dictionary<string, bool>();
			public static bool Prefix(TutorialScreen __instance)
			{
				var tutorialStep = Traverse.Create(__instance).Field<TutorialStep>("tutorialStep").Value;

				if (tutorialStep.audioTerm == "-")
				{
					tutorialStep.audioTerm = $"Audio_{tutorialStep.descriptionTerm}";
					Log.Message($"fixing tutorial audio, tutorialStep.audioTerm={tutorialStep.audioTerm}");
				}
				if (!PlayAudio.TryGetValue(tutorialStep.audioTerm, out bool ShouldPlay))
				{
					PlayAudio.Add(tutorialStep.audioTerm, ShouldPlay = true);
				}
				if (ShouldPlay)
				{
					// hearing the instructions audio repeatedly is annoying,
					// so we add this check to halve the frequency of repeats
					__instance.audioSource.mute = false;
				}
				else
				{
					__instance.audioSource.mute = true;
				}
				PlayAudio[tutorialStep.audioTerm] = !ShouldPlay;
				return true;
			}
		}
	}
}
