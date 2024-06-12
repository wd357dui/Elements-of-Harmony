using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using System.Threading;
using System.IO;
using System;
using System.Linq;
using HarmonyLib;
using System.Reflection;

namespace ElementsOfHarmony
{
	public static class Dance
	{
		public static readonly object SongsMutex = new object();

		public static readonly SortedDictionary<string, Song> Songs = new SortedDictionary<string, Song>();
		public static readonly HashSet<UnityWebRequestAsyncOperation> AudioClipLoadingOperations = new HashSet<UnityWebRequestAsyncOperation>();
		public static readonly ManualResetEventSlim SongsLoadedEvent = new ManualResetEventSlim(false);
		public static volatile bool SongsLoaded = false;

		public static readonly Tuple<string, AudioType>[] SupportedAudioFormats = new Tuple<string, AudioType>[]{
			new Tuple<string, AudioType>( ".aiff", AudioType.AIFF ),
			new Tuple<string, AudioType>( ".ogg", AudioType.OGGVORBIS ),
			new Tuple<string, AudioType>( ".wav", AudioType.WAV )
		};

		public class Song
		{
			public string Description = "";
			public AudioClip Clip = null!;
			public SortedSet<decimal> Beats = new SortedSet<decimal>();
			public SortedDictionary<decimal, decimal?> Punches = new SortedDictionary<decimal, decimal?>();
			public DreamProject.Animator<string> Lyrics = new DreamProject.Animator<string>();
			public decimal? BeatOffset = null;
			public decimal? PunchOffset = null;
			public decimal? BeginAt = null, EndAt = null;
			public decimal Speed = 4, SpeedMultiplayerSplitScreen = 6;
			public decimal LowScore = 0, MediumScore = 0, HighScore = 0;
		}

		public static void Init()
		{
			try
			{
				static void LoadSong(string SongFile, AudioType Format, string DescFile, string LyricsFile)
				{
					Log.Message($"Loading file: {DescFile}");
					using StreamReader desc = new StreamReader(DescFile);
					int numLine = 0;
					string Line, State = "";
					decimal? BPM = null;
					decimal? LowScore = null, MediumScore = null, HighScore = null;
					bool? LowScoreIsPercent = null, MediumScoreIsPercent = null, HighScoreIsPercent = null;
					Song CurrentSong = new Song();
					while ((Line = desc.ReadLine()) != null)
					{
						numLine++;
						if (Line.StartsWith("[") && Line.EndsWith("]"))
						{
							State = Line.Trim('[', ']');
						}
						else if (string.IsNullOrWhiteSpace(Line.Trim())) continue;
						else
						{
							decimal? ParseTimestamp(string Timestamp)
							{
								Timestamp = Timestamp.Trim();
								bool Negative = false;
								if (Timestamp.StartsWith("-"))
								{
									Timestamp = Timestamp.Substring(1);
									Negative = true;
								}
								if (string.IsNullOrEmpty(Timestamp)) return null;
								if (decimal.TryParse(Timestamp, out decimal num))
								{
									return Negative ? -num : num;
								}
								else if (TimeSpan.TryParseExact(Timestamp, @"mm\:ss\.fff", null, out TimeSpan result))
								{
									num = Math.Floor((decimal)result.TotalSeconds) + (result.Milliseconds * 0.001m);
									return Negative ? -num : num;
								}
								else
								{
									Log.Message($"{typeof(Dance).FullName} - {DescFile} ({numLine}): warning, failed to parse timestamp: {Timestamp}");
								}
								return null;
							}
							switch (State)
							{
								case "Description":
									CurrentSong.Description += Line + "\r\n";
									break;
								case "BPM":
									if (BPM != null)
									{
										Log.Message($"{typeof(Dance).FullName} - {DescFile} ({numLine}): warning, duplicate value for BPM: {Line}");
									}
									if (decimal.TryParse(Line, out decimal num))
									{
										BPM = num;
									}
									else
									{
										Log.Message($"{typeof(Dance).FullName} - {DescFile} ({numLine}): warning, failed to parse BPM value: {Line}");
									}
									break;
								case "Beats":
									if (ParseTimestamp(Line) is decimal beat)
									{
										CurrentSong.Beats.Add(beat);
									}
									break;
								case "Punches":
									if (Line.Split(',') is string[] BeginEnd && BeginEnd.Length > 0 &&
										ParseTimestamp(BeginEnd[0].Trim()) is decimal punch)
									{
										decimal? punchEndTime = null;
										if (BeginEnd.Length > 1)
										{
											punchEndTime = ParseTimestamp(BeginEnd[1].Trim());
										}
										CurrentSong.Punches[punch] = punchEndTime;
									}
									break;
								case "BeatOffset":
									if (CurrentSong.BeatOffset != null)
									{
										Log.Message($"{typeof(Dance).FullName} - {DescFile} ({numLine}): warning, duplicate value for BeatOffset: {Line}");
									}
									if (ParseTimestamp(Line) is decimal BeatDeviation)
									{
										CurrentSong.BeatOffset = BeatDeviation;
									}
									break;
								case "PunchOffset":
									if (CurrentSong.PunchOffset != null)
									{
										Log.Message($"{typeof(Dance).FullName} - {DescFile} ({numLine}): warning, duplicate value for PunchOffset: {Line}");
									}
									if (ParseTimestamp(Line) is decimal PunchDeviation)
									{
										CurrentSong.PunchOffset = PunchDeviation;
									}
									break;
								case "BeginAt":
									if (CurrentSong.BeginAt != null)
									{
										Log.Message($"{typeof(Dance).FullName} - {DescFile} ({numLine}): warning, duplicate value for BeginAt: {Line}");
									}
									if (ParseTimestamp(Line) is decimal begin)
									{
										CurrentSong.BeginAt = begin;
									}
									break;
								case "EndAt":
									if (CurrentSong.EndAt != null)
									{
										Log.Message($"{typeof(Dance).FullName} - {DescFile} ({numLine}): warning, duplicate value for EndAt: {Line}");
									}
									if (ParseTimestamp(Line) is decimal end)
									{
										CurrentSong.EndAt = end;
									}
									break;
								case "Speed":
									if (Line.Split(',') is string[] Parts && Parts.Length > 0 &&
										decimal.TryParse(Parts[0].Trim(), out decimal num1))
									{
										CurrentSong.Speed = num1;
										if (Parts.Length > 1 && decimal.TryParse(Parts[1].Trim(), out decimal num2))
										{
											CurrentSong.SpeedMultiplayerSplitScreen = num2;
										}
									}
									else
									{
										Log.Message($"{typeof(Dance).FullName} - {DescFile} ({numLine}): warning, failed to parse Speed value: {Line}");
									}
									break;
								case "Score":
									if (Line.Split(',') is string[] Scores && Scores.Length == 3)
									{
										static void DetermineScore(string Block, ref decimal? Score, ref bool? IsPercent)
										{
											Block = Block.Trim();
											if (string.IsNullOrEmpty(Block)) return;
											if (true == (IsPercent = Block.EndsWith("%")))
											{
												Block = Block.TrimEnd('%');
											}
											if (decimal.TryParse(Block, out decimal score))
											{
												Score = score;
											}
										}
										DetermineScore(Scores[0], ref LowScore, ref LowScoreIsPercent);
										DetermineScore(Scores[1], ref MediumScore, ref MediumScoreIsPercent);
										DetermineScore(Scores[2], ref HighScore, ref HighScoreIsPercent);
									}
									else
									{
										Log.Message($"{typeof(Dance).FullName} - {DescFile} ({numLine}): scores must have 3 values for low, medium, high, seperated by comma");
									}
									break;
							}
						}
					}

					if (File.Exists(LyricsFile))
					{
						Log.Message($"loading lyrics file: {LyricsFile}");
						using StreamReader LRC = new StreamReader(LyricsFile);
						while ((Line = LRC.ReadLine()) != null)
						{
							int right;
							if (Line.StartsWith("[") && (right = Line.IndexOf("]")) >= 0)
							{
								string Timestamp = Line.Remove(right).Substring(1);
								if (TimeSpan.TryParseExact(Timestamp, @"mm\:ss\.fff", null, out TimeSpan result))
								{
									string Lyric = Line.Substring(right + 1);
									decimal Seconds = Math.Floor((decimal)result.TotalSeconds) + (result.Milliseconds * 0.001m);
									CurrentSong.Lyrics.Frames[Seconds] = Lyric;
								}
							}
						}
					}

					/*
					var last = CurrentSong.Lyrics.Frames.Last();
					Log.Message($"wtf is last {last.Key} {last.Value}");
					CurrentSong.Lyrics.UpdateFrame(600m, DreamProject.Animator<string>.Seek.Backward, out object? A, out object? B, out _);
					Log.Message($"600m A={A} B={B} A is string={A is string} B is string={B is string}");
					*/

					CurrentSong.Description = CurrentSong.Description.TrimEnd('\r', '\n', ' ');

					/* score per good hit is 5, promotes to better hit after 2 hits
					 * score per better hit is 7, promotes to perfect hit after 4 hits
					 * score per perfect hit is 9
					 * score per beat in large combo is 1, the end hit of large combo goes to a normal hit
					*/
					int numPunches = CurrentSong.Punches.Count;
					int maxScore = CurrentSong.Punches.Count * 9;
					maxScore -= 2 * Math.Min(6, numPunches);
					maxScore -= 2 * Math.Min(2, numPunches);
					decimal CalculateScore(decimal? Score, bool? ScoreIsPercent, decimal DefaultPercent)
					{
						if (Score is decimal score && ScoreIsPercent is bool percent)
						{
							if (percent)
							{
								return maxScore * 0.01m * score;
							}
							else
							{
								return score; // absolute score
							}
						}
						else
						{
							return maxScore * 0.01m * DefaultPercent;
						}
					}
					CurrentSong.LowScore = CalculateScore(LowScore, LowScoreIsPercent, 30); // default is 30%
					CurrentSong.MediumScore = CalculateScore(MediumScore, MediumScoreIsPercent, 50); // default is 50%
					CurrentSong.HighScore = CalculateScore(HighScore, HighScoreIsPercent, 70); // default is 70%

					UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip("file:///" + SongFile, Format);
					req.SendWebRequest().completed += LoadComplete;

					Songs.Add(Path.GetFileNameWithoutExtension(SongFile), CurrentSong);

					void LoadComplete(AsyncOperation op)
					{
						UnityWebRequestAsyncOperation req = (UnityWebRequestAsyncOperation)op;
						AudioClip clip = DownloadHandlerAudioClip.GetContent(req.webRequest);
						CurrentSong.Clip = clip;
						if (CurrentSong.Beats.Count == 0)
						{
							if (BPM is decimal bpm)
							{
								decimal step = 60 / bpm;
								decimal beatTimestamp = 0.0m;
								while (beatTimestamp <= (decimal)clip.length)
								{
									CurrentSong.Beats.Add(beatTimestamp);
									beatTimestamp += step;
								}
							}
							else
							{
								Log.Message($"{typeof(Dance).FullName} - {DescFile}: no [Beats] or [BPM] defined, this may cause problems, " +
									$"like large combos (if exists) may not function properly");
							}
						}
						lock (SongsMutex)
						{
							Log.Message($"Song loaded: {SongFile}");
							Log.Message($"Description: {CurrentSong.Description}");
							Log.Message($"Punches: {CurrentSong.Punches.Count}");
							AudioClipLoadingOperations.Remove(req);
							if (AudioClipLoadingOperations.Count == 0)
							{
								SongsLoaded = true;
								SongsLoadedEvent.Set();
							}
						}
					}
				}

				int queued = 0;
				if (Directory.Exists("Elements of Harmony/Assets/Minigame/Dance"))
				{
					foreach (var FilePath in Directory.EnumerateFiles("Elements of Harmony/Assets/Minigame/Dance"))
					{
						foreach (var AudioFormat in SupportedAudioFormats)
						{
							if (FilePath.EndsWith(AudioFormat.Item1, StringComparison.InvariantCultureIgnoreCase))
							{
								string DescFile = Path.ChangeExtension(FilePath, "txt");
								string LyricsFile = Path.ChangeExtension(FilePath, "lrc");
								if (File.Exists(DescFile))
								{
									try
									{
										LoadSong(FilePath, AudioFormat.Item2, DescFile, LyricsFile);
										queued++;
									}
									catch (Exception e)
									{
									repeat:
										Log.Message($"{typeof(Dance).FullName} - {e.GetType()}\n{e.StackTrace}\n{e.Message}");
										if (e.InnerException != null)
										{
											e = e.InnerException;
											goto repeat;
										}
									}
								}
							}
						}
					}
				}
				if (queued == 0)
				{
					SongsLoaded = true;
					SongsLoadedEvent.Set();
				}

				// apply all of our patch procedures using Harmony API
				Harmony element = new Harmony($"{typeof(Dance).FullName}");
				int Num = 0;
				if (ElementsOfHarmony.IsAMBA)
				{
					Assembly ElementsOfHarmony_AMBA =
						AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(A => A.GetName().Name == "ElementsOfHarmony.AMBA") ??
						Assembly.LoadFile(Path.Combine(ElementsOfHarmony.AssemblyDirectory, "ElementsOfHarmony.AMBA.dll"));
					if (ElementsOfHarmony_AMBA.GetType("ElementsOfHarmony.AMBA.Dance") is Type Dance_AMBA)
					{
						if (Dance_AMBA.GetMethod("Init") is MethodInfo InitMethod)
						{
							InitMethod.Invoke(null, Array.Empty<object>());
						}
						Num = 0;
						foreach (var Patch in Dance_AMBA.GetNestedTypes())
						{
							new PatchClassProcessor(element, Patch).Patch();
							Num++;
						}
						if (Num > 0)
						{
							Log.Message($"Harmony patch for {Dance_AMBA.FullName} successful - {Num} Patches");
						}
					}
				}
				if (ElementsOfHarmony.IsAZHM)
				{
					Assembly ElementsOfHarmony_AZHM =
						AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(A => A.GetName().Name == "ElementsOfHarmony.AZHM") ??
						Assembly.LoadFile(Path.Combine(ElementsOfHarmony.AssemblyDirectory, "ElementsOfHarmony.AZHM.dll"));
					if (ElementsOfHarmony_AZHM.GetType("ElementsOfHarmony.AZHM.Dance") is Type Dance_AZHM)
					{
						if (Dance_AZHM.GetMethod("Init") is MethodInfo InitMethod)
						{
							InitMethod.Invoke(null, Array.Empty<object>());
						}
						Num = 0;
						foreach (var Patch in Dance_AZHM.GetNestedTypes())
						{
							new PatchClassProcessor(element, Patch).Patch();
							Num++;
						}
						if (Num > 0)
						{
							Log.Message($"Harmony patch for {Dance_AZHM.FullName} successful - {Num} Patches");
						}
					}
				}
			}
			catch (Exception e)
			{
			repeat:
				Log.Message($"{typeof(Dance).FullName} - {e.GetType()}\n{e.StackTrace}\n{e.Message}");
				if (e.InnerException != null)
				{
					e = e.InnerException;
					goto repeat;
				}
			}
		}

		public static Song? SelectedSong
		{
			get
			{
				if (Playlist != null)
				{
					if (CurrentSongIndex >= Playlist.Count)
					{
						CurrentSongIndex = 0;
					}
					return Playlist[CurrentSongIndex];
				}
				return null;
			}
		}

		public static int CurrentSongIndex = 0;
		public static List<Song>? Playlist;

		public static void SelectSong()
		{
			lock (SongsMutex)
			{
				if (!SongsLoaded)
				{
					SongsLoadedEvent.Wait();
				}
				if (Settings.Dance.Customization.Enabled)
				{
					Playlist ??= Songs.Values.ToList();
					CurrentSongIndex++;
				}
			}
		}
	}
}
