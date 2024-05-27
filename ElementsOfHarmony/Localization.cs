using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace ElementsOfHarmony
{
	public class Localization
	{
		public static readonly object AudioClipMutex = new object();

		public static readonly SortedDictionary<string, SortedDictionary<string, AudioClip>> OurAudioClips =
			new SortedDictionary<string, SortedDictionary<string, AudioClip>>();
		public static readonly HashSet<UnityWebRequestAsyncOperation> AudioClipLoadingOperations = new HashSet<UnityWebRequestAsyncOperation>();
		public static readonly ManualResetEventSlim AudioClipsLoadedEvent = new ManualResetEventSlim(false);
		public static volatile bool AudioClipsLoaded = false;

		public static readonly SortedDictionary<string, SortedDictionary<string, string>> OurTranslations =
			new SortedDictionary<string, SortedDictionary<string, string>>();
		public static readonly SortedDictionary<string, string> TermFallback =
			new SortedDictionary<string, string>()
			{
				{ "MLP_Font", "FONTS/NOTO_SANS" }
			};

		public static string[]? OriginalSupportedLanguageList = null;
		public static string[]? OurSupportedLanguageList = null;

		public static readonly Tuple<string, AudioType>[] OurSupportedAudioFormats = new Tuple<string, AudioType>[]{
			new Tuple<string, AudioType>( ".aiff", AudioType.AIFF ),
			new Tuple<string, AudioType>( ".ogg", AudioType.OGGVORBIS ),
			new Tuple<string, AudioType>( ".wav", AudioType.WAV )
		};

		public static void Init()
		{
			try
			{
				// load our audio clip translations
				int numAudioClips = 0;
				void LoadAudioClips(string directory)
				{
					string lang = Path.GetFileName(directory.TrimEnd('/', '\\'));
					OurAudioClips[lang] = new SortedDictionary<string, AudioClip>();
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
					void AudioClipLoadComplete(AsyncOperation op)
					{
						UnityWebRequestAsyncOperation req = (UnityWebRequestAsyncOperation)op;
						AudioClip clip = DownloadHandlerAudioClip.GetContent(req.webRequest);
						string term = Path.GetFileNameWithoutExtension(new Uri(req.webRequest.url).LocalPath);
						lock (AudioClipMutex)
						{
							OurAudioClips[lang][term] = clip;
							Log.Message($"Audio clip loaded: lang={lang}, term={term}");
							AudioClipLoadingOperations.Remove(req);
							if (AudioClipLoadingOperations.Count == 0)
							{
								AudioClipsLoaded = true;
								AudioClipsLoadedEvent.Set();
							}
						}
					}
				}
				// look for all audio files in all sub folders
				if (ElementsOfHarmony.IsAMBA && Directory.Exists("Elements of Harmony/Assets/Localization/AMBA"))
				{
					var langs = Directory.EnumerateDirectories("Elements of Harmony/Assets/Localization/AMBA")
						.Select(D => Path.Combine(D, "AudioClip"))
						.Where(D => Directory.Exists(D));
					foreach (var lang in langs)
					{
						LoadAudioClips(lang);
					}
					Log.Message($"{numAudioClips} audio clips queued for loading");
				}
				else if (ElementsOfHarmony.IsAZHM && Directory.Exists("Elements of Harmony/Assets/Localization/AZHM"))
				{
					var langs = Directory.EnumerateDirectories("Elements of Harmony/Assets/Localization/AZHM")
						.Select(D => Path.Combine(D, "AudioClip"))
						.Where(D => Directory.Exists(D));
					foreach (var lang in langs)
					{
						LoadAudioClips(lang);
					}
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
			repeat:
				Log.Message($"{e.GetType()}\n{e.StackTrace}\n{e.Message}");
				if (e.InnerException != null)
				{
					e = e.InnerException;
					goto repeat;
				}
			}

			try
			{
				// load all text translations
				void LoadTranslation(string f)
				{
					// file name of the txt should be the ISO language code
					// the content should be tab-separated values (TSV)
					// see readme file for content specifications
					string langCode = Path.GetFileNameWithoutExtension(f);
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
									string term = pair[0].Trim(new char[] { '\"' });
									string text = pair[1].Replace(@"\n", "\n").Trim(new char[] { '\"' }).Replace("\"\"", "\"");
									Translations.Add(term, text);
									Log.Message($"Translation added: term={term} value={text}");
								}
							}
						}
					}
					Log.Message($"language {langCode} loaded, {Translations.Count} translations");
				}
				if (ElementsOfHarmony.IsAMBA && Directory.Exists("Elements of Harmony/Assets/Localization/AMBA"))
				{
					var files = Directory.EnumerateDirectories("Elements of Harmony/Assets/Localization/AMBA")
						.Select(D => Path.Combine(D, $"{Path.GetFileName(D)}.txt"))
						.Where(File.Exists);
					foreach (string f in files)
					{
						LoadTranslation(f);
					}
					List<string> OurLanguageList = new List<string>();
					OurLanguageList.AddRange(OurTranslations.Keys);
					OurSupportedLanguageList = OurLanguageList.ToArray();
				}
				else if (ElementsOfHarmony.IsAZHM && Directory.Exists("Elements of Harmony/Assets/Localization/AZHM"))
				{
					var files = Directory.EnumerateDirectories("Elements of Harmony/Assets/Localization/AZHM")
						.Select(D => Path.Combine(D, $"{Path.GetFileName(D)}.txt"))
						.Where(File.Exists);
					foreach (string f in files)
					{
						LoadTranslation(f);
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
			repeat:
				Log.Message($"{e.GetType()}\n{e.StackTrace}\n{e.Message}");
				if (e.InnerException != null)
				{
					e = e.InnerException;
					goto repeat;
				}
			}

			try
			{
				// load fallback fonts into TMP's fallback fonts
				foreach (string fallback in Font.GetPathsToOSFonts())
				{
					try
					{
						TMP_Settings.fallbackFontAssets.Add(TMP_FontAsset.CreateFontAsset(
							new Font(fallback)));
					}
					catch { }
				}
			}
			catch (Exception e)
			{
			repeat:
				Log.Message($"{e.GetType()}\n{e.StackTrace}\n{e.Message}");
				if (e.InnerException != null)
				{
					e = e.InnerException;
					goto repeat;
				}
			}

			try
			{
				// apply all of our patch procedures using Harmony API
				Harmony element = new Harmony($"{typeof(Localization).FullName}");
				int Num = 0;
				if (ElementsOfHarmony.IsAMBA)
				{
					Assembly ElementsOfHarmony_AMBA =
						AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(A => A.GetName().Name == "ElementsOfHarmony.AMBA") ??
						Assembly.LoadFile(Path.Combine(ElementsOfHarmony.AssemblyDirectory, "ElementsOfHarmony.AMBA.dll"));
					if (ElementsOfHarmony_AMBA.GetType("ElementsOfHarmony.AMBA.Localization") is Type Localization_AMBA)
					{
						if (Localization_AMBA.GetMethod("Exist") is MethodInfo ExistMethod)
						{
							ExistMethod.Invoke(null, Array.Empty<object>());
						}
						Num = 0;
						foreach (var Patch in Localization_AMBA.GetNestedTypes())
						{
							new PatchClassProcessor(element, Patch).Patch();
							Num++;
						}
						if (Num > 0)
						{
							Log.Message($"Harmony patch for {Localization_AMBA.FullName} successful - {Num} Patches");
						}
					}
				}
				if (ElementsOfHarmony.IsAZHM)
				{
					Assembly ElementsOfHarmony_AZHM =
						AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(A => A.GetName().Name == "ElementsOfHarmony.AZHM") ??
						Assembly.LoadFile(Path.Combine(ElementsOfHarmony.AssemblyDirectory, "ElementsOfHarmony.AZHM.dll"));
					if (ElementsOfHarmony_AZHM.GetType("ElementsOfHarmony.AZHM.Localization") is Type Localization_AZHM)
					{
						if (Localization_AZHM.GetMethod("Exist") is MethodInfo ExistMethod)
						{
							ExistMethod.Invoke(null, Array.Empty<object>());
						}
						Num = 0;
						foreach (var Patch in Localization_AZHM.GetNestedTypes())
						{
							new PatchClassProcessor(element, Patch).Patch();
							Num++;
						}
						if (Num > 0)
						{
							Log.Message($"Harmony patch for {Localization_AZHM.FullName} successful - {Num} Patches");
						}
					}
				}
			}
			catch (Exception e)
			{
			repeat:
				Log.Message($"{e.GetType()}\n{e.StackTrace}\n{e.Message}");
				if (e.InnerException != null)
				{
					e = e.InnerException;
					goto repeat;
				}
			}
		}
	}
}
