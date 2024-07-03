using HarmonyLib;
using Melbot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using static ElementsOfHarmony.Dance;

namespace ElementsOfHarmony.AMBA
{
	public static class Dance
	{
		public static Dictionary<Song, SortedDictionary<float, float>> SongLargeComboMap = new Dictionary<Song, SortedDictionary<float, float>>();

		public static void Init()
		{
			foreach (var song in Songs)
			{
				SongLargeComboMap[song.Value] = new SortedDictionary<float, float>();
				foreach (var punch in song.Value.Punches)
				{
					if (punch.Value is decimal punchEndTime)
					{
						SongLargeComboMap[song.Value][(float)(punch.Key + (song.Value.PunchOffset ?? decimal.Zero))] =
							(float)(punchEndTime + (song.Value.PunchOffset ?? decimal.Zero));
					}
				}
			}
		}

		public static string GetDescription()
		{
			if (SelectedSong is Song Selected)
			{
				return Selected.Description;
			}
			return "";
		}
		public static float? GetBeginAt()
		{
			if (SelectedSong is Song Selected && Selected.BeginAt is decimal BeginAt)
			{
				return (float)BeginAt;
			}
			return null;
		}
		public static float? GetEndAt()
		{
			if (SelectedSong is Song Selected && Selected.EndAt is decimal EndAt)
			{
				return (float)EndAt;
			}
			return null;
		}
		public static string? GetLyrics(float time)
		{
			if (SelectedSong is Song Selected)
			{
				Selected.Lyrics.UpdateFrame((decimal)time, null, out object? A, out _, out _);
				return A as string;
			}
			return null;
		}
		public static float? GetPunchEndTime(float PunchStartTime)
		{
			if (SelectedSong is Song Selected && SongLargeComboMap[Selected].TryGetValue(PunchStartTime, out float result))
			{
				return result;
			}
			return null;
		}
		public static int? GetLowScore()
		{
			if (SelectedSong is Song Selected)
			{
				return (int)Selected.LowScore;
			}
			return null;
		}
		public static int? GetMediumScore()
		{
			if (SelectedSong is Song Selected)
			{
				return (int)Selected.MediumScore;
			}
			return null;
		}
		public static int? GetHighScore()
		{
			if (SelectedSong is Song Selected)
			{
				return (int)Selected.HighScore;
			}
			return null;
		}

		public static FashionShowMinigame? PreviousInstance = null;
		public static TextMeshProUGUI? DescriptionDisplay;
		public static TextMeshProUGUI? LyricsDisplay;

		public static bool Started = false;
		public static bool Paused = false;
		public static float StartTime = 0f;
		public static float PauseTime = 0f;

		public static float PreviousTimestamp = 0f;
		public static float CurrentTimestamp => Started ?
			PreviousTimestamp = Paused ? PreviousTimestamp : (Time.time - StartTime) :
			float.NegativeInfinity;

		public static string? PreviousLyrics = null;

		[HarmonyPatch(typeof(FashionShowMinigame), "OnEnable")]
		public static class FashionShowMinigameOnEnableHook
		{
			public static void Prefix(FashionShowMinigame __instance)
			{
				PreviousInstance = __instance;
			}
			public static void Postfix(FashionShowMinigame __instance)
			{
				SelectSong();

				if (SelectedSong is Song Selected)
				{
					// modify the "punches"
					void ModifyPlayerCombo(PlayerCombo playerCombo)
					{
						Log.Message($"modifying \"punch\" list for {playerCombo}");
						playerCombo.fashionShowCombo.signalCombo.puncher.punches = Selected.Punches.Select(Pair => new BeatType()
						{
							time = (float)(Pair.Key + (Selected.PunchOffset ?? decimal.Zero)),
							large = Pair.Value != null,
						}).ToList();
					}
					ModifyPlayerCombo(__instance.playerCombo_s);
					//ModifyPlayerCombo(__instance.playerCombo_0); // these three use the same `puncher` instance
					//ModifyPlayerCombo(__instance.playerCombo_1);

					// modify the "beats"
					Log.Message("modifying \"beat\" list");
					__instance.beater.beats = Selected.Beats.Select(B => (float)(B + (Selected.BeatOffset ?? decimal.Zero))).ToList();

					// replace the audio track
					__instance.audioSource.clip = Selected.Clip;
					// since I adjusted the "Music" mixer group volume beyond 100%
					// to fix the in-game-BGMs-are-too-quiet problem,
					// it can't be used for third-party audios without losing quality,
					// so I choose to use speech volume instead
					__instance.audioSource.outputAudioMixerGroup = __instance.audioSource.outputAudioMixerGroup
						.audioMixer.FindMatchingGroups("Voice").First();
				}

				TextMeshProUGUI text = Traverse.Create(__instance.gui.points).Field<TextMeshProUGUI>("text").Value;
				// Initialize description and lyrics display
				if (DescriptionDisplay == null)
				{
					GameObject DescriptionDisplayObj = new GameObject(nameof(DescriptionDisplay), typeof(RectTransform), typeof(TextMeshProUGUI));
					DescriptionDisplayObj.transform.parent = __instance.gui.transform;

					DescriptionDisplay = DescriptionDisplayObj.GetComponent<TextMeshProUGUI>();
					DescriptionDisplay.name = nameof(DescriptionDisplay);

					DescriptionDisplay.font = text.font;
					DescriptionDisplay.fontSize = text.fontSize;
					DescriptionDisplay.fontStyle = text.fontStyle;
					DescriptionDisplay.fontWeight = text.fontWeight;
					DescriptionDisplay.outlineWidth = text.outlineWidth;
					DescriptionDisplay.color = text.color;
					DescriptionDisplay.faceColor = text.faceColor;
					DescriptionDisplay.outlineColor = text.outlineColor;
					DescriptionDisplay.characterSpacing = text.characterSpacing;
					DescriptionDisplay.fontMaterial = text.fontMaterial;

					DescriptionDisplay.text = GetDescription();
					DescriptionDisplay.alignment = TextAlignmentOptions.Left;
					DescriptionDisplay.overflowMode = TextOverflowModes.Overflow;
					DescriptionDisplay.margin = new Vector4(80f, 0f, 0f, 0f);

					DescriptionDisplay.rectTransform.localPosition = Vector3.zero;
					DescriptionDisplay.rectTransform.localScale = Vector3.one;
					DescriptionDisplay.rectTransform.pivot = new Vector2(0.5f, 0.5f); // regard `position` as the center point of this rect
					DescriptionDisplay.rectTransform.anchorMin = new Vector2(0f, 0f); // set bottom left corner of this rect as bottom left corner of parent
					DescriptionDisplay.rectTransform.anchorMax = new Vector2(1f, 1f); // set top right corner of this rect as top right corner of parent
					DescriptionDisplay.rectTransform.offsetMin = Vector2.zero; // no position deviation for the bottom left corner
					DescriptionDisplay.rectTransform.offsetMax = Vector2.zero; // no position deviation for the top right corner

					DescriptionDisplay.enabled = true;
					DescriptionDisplay.gameObject.SetActive(true);
				}
				if (LyricsDisplay == null)
				{
					GameObject LyricsDisplayObj = new GameObject(nameof(DescriptionDisplay), typeof(RectTransform), typeof(TextMeshProUGUI));
					LyricsDisplayObj.transform.parent = __instance.gui.transform;

					LyricsDisplay = LyricsDisplayObj.GetComponent<TextMeshProUGUI>();
					LyricsDisplay.name = nameof(LyricsDisplay);

					LyricsDisplay.font = text.font;
					LyricsDisplay.fontSize = text.fontSize;
					LyricsDisplay.fontStyle = text.fontStyle;
					LyricsDisplay.fontWeight = text.fontWeight;
					LyricsDisplay.outlineWidth = text.outlineWidth;
					LyricsDisplay.color = text.color;
					LyricsDisplay.faceColor = text.faceColor;
					LyricsDisplay.outlineColor = text.outlineColor;
					LyricsDisplay.characterSpacing = text.characterSpacing;
					LyricsDisplay.fontMaterial = text.fontMaterial;

					LyricsDisplay.text = "";
					LyricsDisplay.alignment = TextAlignmentOptions.Top;
					LyricsDisplay.overflowMode = TextOverflowModes.Overflow;
					LyricsDisplay.margin = new Vector4(0f, 80f, 0f, 0f);

					LyricsDisplay.rectTransform.localPosition = Vector3.zero;
					LyricsDisplay.rectTransform.localScale = Vector3.one;
					LyricsDisplay.rectTransform.pivot = new Vector2(0.5f, 0.5f); // regard position as center point of this rect
					LyricsDisplay.rectTransform.anchorMin = new Vector2(0.25f, 0f); // set 25% x as left border and 75% x as right border
					LyricsDisplay.rectTransform.anchorMax = new Vector2(0.75f, 1f); // so that lyrics won't block the score number display
					LyricsDisplay.rectTransform.offsetMin = Vector2.zero; // no position deviation for the bottom left corner
					LyricsDisplay.rectTransform.offsetMax = Vector2.zero; // no position deviation for the top right corner

					LyricsDisplay.enabled = true;
					LyricsDisplay.gameObject.SetActive(true);
				}
			}
		}

		[HarmonyPatch(typeof(FashionShowMinigame), "OnDisable")]
		public static class FashionShowMinigameOnDisableHook
		{
			public static void Postfix()
			{
				PreviousInstance = null;
				Started = false;
			}
		}

		[HarmonyPatch(typeof(FashionShowMinigame), "StartGame")]
		public static class FashionShowMinigameStartGamePatch
		{
			public static bool Prefix(FashionShowMinigame __instance)
			{
				// original code
				__instance.active = true;
				__instance.gui.gameObject.SetActive(value: true);
				List<MinigamePJ> list = new List<MinigamePJ>();
				if (__instance.Mode == MinigamePlayersType.multiplayer)
				{
					GameSession gameSession = NonPersistentSingleton<BaseSystem>.Get().gameSession;
					list.Add(gameSession.GetPjFromPlayer(1));
					list.Add(gameSession.GetPjFromPlayer(2));
				}
				__instance.gui.SetGameMode(__instance.Mode, list);
				__instance.fashionShowRewarding.ResetRewards();

				// turn off unused pseudo timer display
				__instance.gui.timer.gameObject.SetActive(false);
				__instance.gui.timer_2.gameObject.SetActive(false);

				// original code
				for (int i = 0; i < __instance.playerCombos.Count; i++)
				{
					PlayerCombo playerCombo = __instance.playerCombos[i];
					FashionShowScore obj = __instance.Scores[i];
					obj.CorrectActions = 0;
					obj.TotalActions = 0;
					playerCombo.StartGame();
				}

				// original code
				__instance.gui.points.SetPoints(0);
				__instance.onGameStarted();

				return false;
			}
		}

		[HarmonyPatch(typeof(GoCountDown), "BeginCountdown")]
		public static class GoCountDownHook
		{
			public static void Prefix()
			{
				if (PreviousInstance != null)
				{
					StartTime = Time.time + 3f - (GetBeginAt() ?? 0f);
					Paused = false;
					Started = true;
				}
			}
		}

		[HarmonyPatch(typeof(FashionShowMinigame), "UpdateGame")]
		public static class FashionShowMinigameUpdateGamePatch
		{
			public static readonly MethodInfo OnGameFinished = AccessTools.Method(typeof(FashionShowMinigame), "OnGameFinished");
			public static bool Prefix(FashionShowMinigame __instance)
			{
				var playing = Traverse.Create(__instance).Field<bool>("playing");

				float time = CurrentTimestamp;
				if (!float.IsFinite(time)) return false;

				bool TimesUp = time > (GetEndAt() ?? __instance.audioSource.clip.length);
				if (!TimesUp)
				{
					for (int i = 0; i < __instance.playerCombos.Count; i++)
					{
						__instance.playerCombos[i].UpdateGame();
					}
				}
				else
				{
					playing.Value = false;
					OnGameFinished.Invoke(__instance, Array.Empty<object>());
				}

				return false;
			}
		}

		[HarmonyPatch(typeof(MinigameBase), "OnPauseToggled")]
		public static class MinigameBaseOnPauseToggledHook
		{
			public static void Postfix(MinigameBase __instance, bool paused)
			{
				if (__instance is FashionShowMinigame)
				{
					if (paused)
					{
						PauseTime = Time.time;
						Paused = true;
					}
					else
					{
						StartTime += Time.time - PauseTime;
						Paused = false;
						__instance.audioSource.time = CurrentTimestamp;
					}
				}
			}
		}

		[HarmonyPatch(typeof(SignalCombo), "Update")] // generate combos
		public static class SignalComboUpdatePatch
		{
			public static bool Prefix(SignalCombo __instance)
			{
				var nextPunch = Traverse.Create(__instance).Field<int>("nextPunch");
				var total = Traverse.Create(__instance).Field<int>("total");

				// original code
				if (!Traverse.Create(__instance.fashionShowCombo.playerCombo.fashionShowMinigame).Field<bool>("playing").Value ||
					0 >= __instance.puncher.punches.Count)
				{
					return false;
				}

				// my code
				float time = CurrentTimestamp;
				if (!float.IsFinite(time)) return false;

				// additional code
				if (time >= 0f && Started && !Paused &&
					PreviousInstance?.audioSource.isPlaying == false)
				{
					PreviousInstance.audioSource.Play();
					PreviousInstance.audioSource.time = time;
				}
				if (PreviousInstance?.introSource.isPlaying == true)
				{
					float GradualDecrease = Mathf.Clamp01(Mathf.Pow(-time / 3f, 2f)) * 0.5f;
					PreviousInstance.introSource.volume = GradualDecrease;
					if (time >= 0f)
					{
						PreviousInstance.introSource.Stop();
					}
				}
				if (LyricsDisplay is TextMeshProUGUI text &&
					GetLyrics(time) is string Lyrics &&
					Lyrics != PreviousLyrics)
				{
					// dislay the lyrics
					text.text = PreviousLyrics = Lyrics;
				}

				// original code
				if (nextPunch.Value >= __instance.puncher.punches.Count)
				{
					return false;
				}
				BeatType beatType = __instance.puncher.punches[nextPunch.Value];

				// original code
				//float num = Vector3.Distance(__instance.signalLauncher.hit.position, __instance.signalLauncher.start.position) /
				//	__instance.configuration.speeds[__instance.signalLauncher.lifetimeIndex];
				// my code
				float speed = (__instance.signalLauncher.lifetimeIndex == 0 ?
					(float?)(SelectedSong?.Speed) : (float?)(SelectedSong?.SpeedMultiplayerSplitScreen)
					) ?? __instance.configuration.speeds[__instance.signalLauncher.lifetimeIndex];
				float num = Vector3.Distance(__instance.signalLauncher.hit.position, __instance.signalLauncher.start.position) / speed;

				if (beatType.time <= time + num)
				{
					total.Value++;
					PackData data = __instance.comboPack.GetData(total.Value);
					if (beatType.large)
					{
						data.action = MLPAction.ANY;
					}
					nextPunch.Value++;
					Signal signal = __instance.signalLauncher.Launch(data);
					if ((bool)signal)
					{
						// original code
						//signal.traveling.startTime = time;
						//signal.traveling.hitTime = beatType.time - 0.16f;

						// my code
						signal.traveling.startTime = beatType.time - num;
						signal.traveling.hitTime = beatType.time;

						if (beatType.large)
						{
							// code modifed from Signal.Initialize
							// to set the length of the large combo bar
							RectTransform[] backgroundImages = signal.buttonFashion.backgroundImages;
							foreach (RectTransform rectTransform in backgroundImages)
							{
								float punchMaxSize = (0 < __instance.signalLauncher.lifetimeIndex) ? 850f : 1000f;
								if (GetPunchEndTime(beatType.time) is float punchEndTime)
								{
									float punchDuration = punchEndTime - beatType.time;
									float shinkFactor = (0 < __instance.signalLauncher.lifetimeIndex) ? 60f : 120f;
									punchMaxSize = shinkFactor * speed * punchDuration + 236f;
								}
								rectTransform.sizeDelta = new Vector2(punchMaxSize, rectTransform.sizeDelta.y);
							}
						}
					}
				}

				return false;
			}
		}

		[HarmonyPatch(typeof(Traveling), "Update")]
		public static class ComboTravelingPatch
		{
			public static bool Prefix(Traveling __instance)
			{
				var signal = __instance.signal;
				var signalLauncher = Traverse.Create(signal).Field<SignalLauncher>("signalLauncher").Value;

				// the original code had lots of Debug.Log calls,
				// which may hinder performance, especially when any mod is hooking debug logs
				// 
				// furthermore, the original code uses AudioSource.time as timestamp which,
				// although straight forward, is inaccurate and causes stutter

				float time = CurrentTimestamp;
				if (!float.IsFinite(time)) return false;

				//Debug.Log("------------------------------START TRAVELING UPDATE");
				//Vector3.Distance(signal.finalPosition, signal.transform.position); // this seems unused
				//Debug.Log("------------------------------DISTANCE CALCULATED");
				//Vector3.Distance(signal.finalPosition, signalLauncher.hitAreaStart.position); // this seems unused
				//Debug.Log("------------------------------MAX DISTANCE CALCULATED");
				//Debug.Log("------------------------------BEFORE FIRST IF");
				if (!signal.buttonFashion.model.animator.GetBool("in"))
				{
					//Debug.Log("------------------------------FIRST IF");
					Vector3 vector = signalLauncher.hitAreaStart.transform.position - signalLauncher.start.transform.position;
					Vector3 vector2 = __instance.transform.position - signalLauncher.start.transform.position;
					bool value = vector.magnitude <= vector2.magnitude;
					signal.buttonFashion.model.animator.SetBool("in", value);
				}

				//Debug.Log("------------------------------BEFORE SECOND IF");
				if (MLPAction.ANY != signal.action)
				{
					//Debug.Log("------------------------------SECOND IF");
					float num = __instance.hitTime - __instance.startTime;
					float num2 = __instance.hitTime - time;
					Vector3 vector3 = signalLauncher.hit.position - signal.initialPosition;
					signal.transform.position = signalLauncher.hit.position - vector3 * (num2 / num);
					Vector3 lhs = signal.finalPosition - signal.initialPosition;
					Vector3 rhs = signal.finalPosition - __instance.transform.position;
					float num3 = Mathf.Sign(Vector3.Dot(lhs, rhs));
					//Debug.Log("------------------------------BEFORE SECOND AND A HALF IF");
					if (0f > num3)
					{
						//Debug.Log("------------------------------SECOND AND A HALF IF");
						__instance.onTimedOut?.Invoke();
					}
				}
				else
				{
					//Debug.Log("------------------------------FIRST ELSE");
					Vector3 lhs2 = signalLauncher.center.position - __instance.transform.position;
					Vector3 rhs2 = signalLauncher.center.position - signal.initialPosition;
					float num4 = Vector3.Dot(lhs2, rhs2);
					//Debug.Log("------------------------------BEFORE FIRST IF INSIDE ELSE");
					if (0f < num4)
					{
						//Debug.Log("------------------------------FIRST IF INSIDE ELSE");
						float num5 = __instance.hitTime - __instance.startTime;
						float num6 = __instance.hitTime - time;
						Vector3 vector4 = signalLauncher.hit.position - signal.initialPosition;
						signal.transform.position = signalLauncher.hit.position - vector4 * (num6 / num5);
					}
					else
					{
						bool flag = false;
						//Debug.Log("------------------------------FIRST ELSE INSIDE ELSE");
						RectTransform[] backgroundImages = signal.buttonFashion.backgroundImages;
						foreach (RectTransform rectTransform in backgroundImages)
						{
							/* although this code is used to shink the large combo bar, it didn't shink it fast enough,
							 * and is causing the large combo bar to gradually "grow" in size compared with other combo bars
							 * and it can block up subsequent combos if any of which is close enough;
							 * 
							 * also deltaTime is not reliable either
							 * 
							float num7 = 236f;
							float num8 = rectTransform.sizeDelta.x - 100f * signal.velocity.magnitude * Time.deltaTime;
							rectTransform.sizeDelta = new Vector2(Mathf.Max(num7, num8), rectTransform.sizeDelta.y);
							flag = num7 >= num8;
							 */

							// so, a better shinking method is needed
							float maxSize = (0 < signalLauncher.lifetimeIndex) ? 850f : 1000f;
							float shinkFactor = (0 < signalLauncher.lifetimeIndex) ? 60f : 120f;
							if (GetPunchEndTime(__instance.hitTime) is float punchEndTime)
							{
								float punchDuration = punchEndTime - __instance.hitTime;
								maxSize = shinkFactor * __instance.signal.maxSpeed * punchDuration + 236f;
							}

							float timeElapsedSinceHit = time - __instance.hitTime;
							float minimunSize = 236f;
							float calculatedSize = maxSize - shinkFactor * __instance.signal.maxSpeed * timeElapsedSinceHit;
							rectTransform.sizeDelta = new Vector2(Mathf.Max(minimunSize, calculatedSize), rectTransform.sizeDelta.y);
							flag = minimunSize >= calculatedSize;
						}

						//Debug.Log("------------------------------BEFORE FIRST IF INSIDE ELSE INSIDE ELSE");
						if (flag)
						{
							//Debug.Log("------------------------------FIRST IF INSIDE ELSE INSIDE ELSE");
							__instance.onLargeDone?.Invoke();
						}
					}
				}

				//Debug.Log("------------------------------END TRAVELING UPDATE");
				return false;
			}
		}

		[HarmonyPatch(typeof(FashionShowCombo), "CheckAction")]
		public static class FashionShowComboCheckActionPatch
		{
			public static bool Prefix(FashionShowCombo __instance, MLPAction action)
			{
				var playerCombo = __instance.playerCombo;
				var signalCombo = __instance.signalCombo;
				var onSignalHit = __instance.onSignalHit;
				var onSignalFailed = __instance.onSignalFailed;

				// original code
				if (!Traverse.Create(playerCombo.fashionShowMinigame).Field<bool>("playing").Value)
				{
					return false;
				}
				Signal current = signalCombo.GetCurrent();
				if (!(null != current) || !current.gameObject.activeSelf)
				{
					return false;
				}
				float num = Vector3.Distance(current.finalPosition, current.transform.position);
				float num2 = Vector3.Distance(current.finalPosition, Traverse.Create(current).Field<SignalLauncher>("signalLauncher").Value.hitAreaStart.position);
				if (!(num <= num2))
				{
					return false;
				}
				if (MLPAction.ANY != current.action)
				{
					if (action == current.action)
					{
						onSignalHit(__instance, signalCombo, current);
						return false;
					}
					__instance.StopEmojis();
					onSignalFailed(__instance, signalCombo, current);
				}
				/* don't want this for the mod
				else
				{
					if (!kids.isPlaying)
					{
						kids.Play();
					}
					_ = 6;
				}
				*/

				return false;
			}
		}

		[HarmonyPatch(typeof(FashionShowMinigame), "FillScores")]
		public static class FashionShowMinigameFillScoresPatch
		{
			public static void Prefix(FashionShowMinigame __instance)
			{
				MinigameStarScoreInfo starScoreInfo = Traverse.Create(__instance).Field<MinigameStarScoreInfo>("starScoreInfo").Value;
				starScoreInfo.bronzeScore.score = GetLowScore() ?? starScoreInfo.bronzeScore.score;
				starScoreInfo.silverScore.score = GetMediumScore() ?? starScoreInfo.silverScore.score;
				starScoreInfo.goldScore.score = GetHighScore() ?? starScoreInfo.goldScore.score;
			}
		}
	}
}
