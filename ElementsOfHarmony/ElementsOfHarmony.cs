using UnityEngine;
using HarmonyLib;
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Collections.Generic;
using I2.Loc;
using System.Reflection;
using UnityEngine.Networking;
using UnityEngine.UI;
using Melbot;

namespace ElementsOfHarmony
{
    // Yes, I named the project "Elements of Harmony",
    // because I'm using Harmony API and I'm modding a MLP game (sorry no sorry lol)
    public static class ElementsOfHarmony
    {
        private static Mutex GlobalMutexA = new Mutex();
        private static Mutex GlobalMutexB = new Mutex();
        private static Mutex GlobalMutexC = new Mutex();
        private static bool Existed = false;
        private static StreamWriter Log;
        private static TcpClient Client;
        private static NetworkStream Stream;
        private static SortedDictionary<string, AudioClip> OurAudioClips = new SortedDictionary<string, AudioClip>();
        private static HashSet<UnityWebRequestAsyncOperation> AudioClipLoadingOperations = new HashSet<UnityWebRequestAsyncOperation>();
        private static ManualResetEvent AudioClipsLoadedEvent = new ManualResetEvent(false);
        private static volatile bool AudioClipsLoaded = false;
        private static SortedDictionary<string, SortedDictionary<string, string>> OurTranslations = new SortedDictionary<string, SortedDictionary<string, string>>();
        private static string[] OriginalSupportedLanguageList = null;
        private static string[] OurSupportedLanguageList = null;
        private static string OurSelectedLanguageOverride_Internal = "";
        private static string OurSelectedLanguageOverride
        {
            get { return OurSelectedLanguageOverride_Internal; }
            set { OurSelectedLanguageOverride_Internal = value; try { WriteOurSettings(); } catch (Exception e) { } }
        }
        private static string[] SupportedAudioFormats = { ".aiff", ".ogg", ".wav" };
        private static AudioType[] SupportedAudioTypes = { AudioType.AIFF, AudioType.OGGVORBIS, AudioType.WAV };

        [UnityEngine.RuntimeInitializeOnLoadMethod(loadType: UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Exist() // this is our "main" function
        {
            GlobalMutexA.WaitOne();
            if (Existed)
            {
                GlobalMutexA.ReleaseMutex();
                return;
            }
            else Existed = true;

            try
            {
                if (!Directory.Exists("Elements of Harmony"))
                {
                    Directory.CreateDirectory("Elements of Harmony");
                }
                Log = new StreamWriter("Elements of Harmony/Elements of Harmony.log");
            }
            catch (Exception e)
            { }

            try
            {
                // connect to local server to support immediate log display
                // this is optional
                Client = new TcpClient();
                Client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1024));
                Stream = Client.GetStream();
                LogMessage("Connection success");
            }
            catch (Exception e)
            {
                LogMessage(e.StackTrace + "\n" + e.Message);
            }

            try
            {
                ReadOurSettings();
            }
            catch (Exception e)
            { }

            try
            {
                // load our audio clip translations
                int numAudioClips = 0;
                void LoadAudioClips(string directory)
                {
                    IEnumerable<string> files = Directory.EnumerateFiles(directory);
                    foreach (string f in files)
                    {
                        for (int index = 0; index < SupportedAudioFormats.Length; index++)
                        {
                            if (f.EndsWith(SupportedAudioFormats[index], StringComparison.OrdinalIgnoreCase))
                            {
                                UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip("file:///" + f, SupportedAudioTypes[index]);
                                req.SendWebRequest().completed += AudioClipLoadComplete;
                                numAudioClips++;
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
                // look for all .wav files recursively in the "Elements of Harmony/AudioClip" folder and all its sub folders
                LoadAudioClipsRecursive("Elements of Harmony/AudioClip");
                LogMessage(numAudioClips + " audio clips queued for loading");
            }
            catch (Exception e)
            {
                LogMessage(e.StackTrace + "\n" + e.Message);
            }

            try
            {
                // load our text translations in the "Elements of Harmony/Translations" folder
                IEnumerable<string> files = Directory.EnumerateFiles("Elements of Harmony/Translations");
                foreach (string f in files)
                {
                    if (f.ToLower().EndsWith(".txt"))
                    {
                        // file name of the txt should be the ISO language code
                        // the content should be tab-separated values (TSV)
                        // where first column is the term (is case sensitive)
                        // and second column is the translated text (other columns will be ignored)
                        // please also add your language code (as term) and your language name (as translated text)
                        // so that your language name can show up in the game menu correctly (also case sensitive)
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
                                        LogMessage("Translation added: term=" + term + " value=" + text);
                                    }
                                }
                            }
                        }
                        LogMessage("language " + langCode + " loaded, " + Translations.Count + " translations");
                    }
                }
                List<string> OurLanguageList = new List<string>();
                OurLanguageList.AddRange(OurTranslations.Keys);
                OurSupportedLanguageList = OurLanguageList.ToArray();
            }
            catch (Exception e)
            {
                LogMessage(e.StackTrace + "\n" + e.Message);
            }

            try
            {
                // apply all of our patches using Harmony API
                Harmony element = new Harmony("Elements_Of_Harmony");
                element.PatchAll();
                LogMessage("Harmony patch successful");
            }
            catch (Exception e)
            {
                LogMessage(e.StackTrace + "\n" + e.Message);
            }

            // attach our error handlers
            Application.logMessageReceived += LogCallback;
            AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;

            GlobalMutexA.ReleaseMutex();
        }

        private static void LogMessage(string message)
        {
            // everything we want to log will be written to "Elements of Harmony/Elements of Harmony.log"
            // and to the local server if connected
            GlobalMutexB.WaitOne();
            if (Log != null)
            {
                Log.WriteLine(message);
                Log.Flush();
            }
            if (Client != null && Stream != null && Client.Connected && Stream.CanWrite)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                Stream.Write(BitConverter.GetBytes(buffer.Length), 0, 4);
                Stream.Flush();
                Stream.Write(buffer, 0, buffer.Length);
                Stream.Flush();
            }
            GlobalMutexB.ReleaseMutex();
        }

        public static void ReadOurSettings()
        {
            // the only setting I have right now is the language override
            // this settings exist because the game can't save a language setting that it doesn't originally support into the game save
            // (so I took the matter into my own hooves)
            // example:
            // OurSelectedLanguageOverride=zh-CN
            // (do not add spaces before "=")
            // (the value is the ISO language code, same with your translation file name, case sensitive)
            using (StreamReader reader = new StreamReader("Elements of Harmony/Settings.txt"))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line.StartsWith("OurSelectedLanguageOverride="))
                    {
                        OurSelectedLanguageOverride_Internal = line.Substring("OurSelectedLanguageOverride=".Length).Trim();
                        LogMessage("OurSelectedLanguageOverride=" + OurSelectedLanguageOverride_Internal);
                    }
                }
            }
            WriteOurSettings();
        }
        public static void WriteOurSettings()
        {
            using (StreamWriter Settings = new StreamWriter("Elements of Harmony/Settings.txt", false))
            {
                Settings.WriteLine("OurSelectedLanguageOverride=" + OurSelectedLanguageOverride);
                Settings.Flush();
            }
        }

        public static void AudioClipLoadComplete(AsyncOperation op)
        {
            GlobalMutexC.WaitOne();
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
            OurAudioClips.Add(term, clip);
            LogMessage("Audio clip loaded: " + term);
            AudioClipLoadingOperations.Remove(req);
            if (AudioClipLoadingOperations.Count == 0)
            {
                AudioClipsLoaded = true;
                AudioClipsLoadedEvent.Set();
            }
            GlobalMutexC.ReleaseMutex();
        }

        #region error handlers, made to record any error that may or may not caused by our mod

        public static void LogCallback(string condition, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    LogMessage(Environment.StackTrace + "\n" +
                        condition + "\n" +
                        stackTrace + "\n");
                    break;
            }
        }
        public static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            LogMessage(Environment.StackTrace + "\n" +
                sender.ToString() + "\n" +
                args.ToString() + "\n");
        }

        [HarmonyPatch]
        class ExceptionConstructor
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                return new List<MethodBase> {
                    AccessTools.Constructor(typeof(Exception), new Type[0]),
                    AccessTools.Constructor(typeof(Exception), new Type[1]{ typeof(string) }),
                    AccessTools.Constructor(typeof(Exception), new Type[2]{ typeof(string), typeof(Exception) }),
                    AccessTools.Constructor(typeof(Exception), new Type[2]{ typeof(SerializationInfo), typeof(StreamingContext) }),
                };
            }
            public static void Postfix(Exception __instance)
            {
                if (!Environment.StackTrace.Contains("ElementsOfHarmony.LogMessage")) // prevent infinite loop
                {
                    LogMessage(Environment.StackTrace + "\n" + __instance.StackTrace + "\n" + __instance.Message + "\n");
                }
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Debug))]
        [HarmonyPatch(nameof(UnityEngine.Debug.LogError), typeof(object))]
        public class LogError
        {
            public static void Postfix(object message)
            {
                LogMessage(Environment.StackTrace + "\n" + message.ToString());
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Debug))]
        [HarmonyPatch(nameof(UnityEngine.Debug.LogException), typeof(Exception))]
        public class LogException
        {
            public static void Postfix(Exception exception)
            {
                LogMessage(Environment.StackTrace + "\n" +
                    exception.StackTrace + "\n" +
                    exception.GetType().FullName + "\n" +
                    exception.Message);
            }
        }

        #endregion

        [HarmonyPatch(typeof(SupportedLanguages))]
        [HarmonyPatch("GetSupportedLanguages")] // the game use this method to fetch a list of all supported languages
        public class GetSupportedLanguagesPatch
        {
            public static void Postfix(ref List<SupportedLanguageParams> __result)
            {
                SupportedLanguageParams en_US = null;
                foreach (string langCode in OurSupportedLanguageList)
                {
                    bool languageFound = false;
                    for (int x = 0; x < __result.Count; x++)
                    {
                        if (en_US == null && __result[x].code == "en-US")
                        {
                            en_US = __result[x];
                        }
                        if (langCode == __result[x].code)
                        {
                            languageFound = true;
                            break;
                        }
                    }
                    if (!languageFound)
                    {
                        // our language was not present in the original supported languages list
                        OurSupportedLanguageParams ourCurrentLang = new OurSupportedLanguageParams(en_US);
                        ourCurrentLang.code = langCode;
                        ourCurrentLang.localizedAudio = false;
                        if (ourCurrentLang.assetReference != null)
                        {
                            LanguageSourceAsset sourceAsset = ourCurrentLang.assetReference.Asset as LanguageSourceAsset;
                            if (sourceAsset != null)
                            {
                                // so we add our language to the list and copy paste asset data from English
                                // language asset data didn't really matter because
                                // when the game can't find our language in its fixed sized array of languages it will fallback to English anyway
                                // what matters is the language code we are setting here,
                                // which will be passed to LanguageSelector class where we have chance to fetch it from
                                sourceAsset.name = "I2Languages_" + langCode;
                                __result.Add(ourCurrentLang);
                                LogMessage("Language added " + langCode);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(LanguageSelector))]
        [HarmonyPatch("ShowLangname")] // the game calls this method to make language names appear/disapper in the menu when player moves the selection
        public class LoadLanguagePatch
        {
            public static void Prefix(LanguageSelector __instance, string langName)
            {
                Transform rootTransform = __instance.langNames;
                Transform firstChild = null;
                List<string> Existed_Languages = new List<string>();
                foreach (Transform child in rootTransform)
                {
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
                        // so that the name our language can show up in the menu
                        // otherwise our language name will just be showing as blank
                        string ourLanguageName = ourLang;
                        SortedDictionary<string, string> Translations = OurTranslations[ourLang];
                        Translations.TryGetValue(ourLang, out ourLanguageName);

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
                                    LogMessage("added our language " + ourLang + " display name: " + ourLanguageName);
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(LanguageSelector))]
        [HarmonyPatch("OnSelected")] // the game calls this method when player selects a language
        public class OnSelectedPatch
        {
            public static void Prefix(LanguageSelector __instance)
            {
                int index = Traverse.Create(__instance).Field("index").GetValue<int>();
                List<SupportedLanguageParams> OurSupportedLanguages = NonPersistentSingleton<BaseSystem>.Get().i2LManager.supportedLanguages.GetSupportedLanguages();
                string SelectedLangCode = OurSupportedLanguages[index].code;
                bool wasOverride = !string.IsNullOrEmpty(OurSelectedLanguageOverride);
                if (!OriginalSupportedLanguageList.Contains(SelectedLangCode))
                {
                    // if the language is not originally supported we will follow our override settings
                    OurSelectedLanguageOverride = SelectedLangCode;
                    LogMessage("language override: " + SelectedLangCode);
                    // and later the game settings itself will fallback to English (en-US)
                }
                else
                {
                    // but if the language is already originally supported then we will just follow the game settings
                    OurSelectedLanguageOverride = "";
                    LogMessage("language selected: " + SelectedLangCode);
                }
                if (wasOverride &&
                    SelectedLangCode == "en-US" &&
                    LocalizationManager.CurrentLanguageCode == "en-US")
                {
                    // when player is switching back from our "override" language to English,
                    // the game won't start changing because the game settings is already English at this time due to fallback
                    // (so the game will "believe" that it doesn't need to change)
                    // so in order to fix this we just change the game settings to another language beforehand
                    LocalizationManager.CurrentLanguageCode = "ru";
                }
            }
        }

        [HarmonyPatch(typeof(LocalizationManager))]
        [HarmonyPatch("GetTranslation")] // the game use this method to fetch text translations and to map the terms to reference translated audio clips
        public class GetTranslationPatch
        {
            public static bool Prefix(string Term, ref string __result, out string __state)
            {
                __state = LocalizationManager.CurrentLanguageCode; // save the current game language setting for Postfix function to reset to
                if (OriginalSupportedLanguageList == null)
                {
                    // save a list of the game's original supported langauges if we haven't already
                    SupportedLanguageParams[] OriginalSupportedLanguageParams = NonPersistentSingleton<BaseSystem>.Get().i2LManager.supportedLanguages.supportedLanguages;
                    OriginalSupportedLanguageList = new string[OriginalSupportedLanguageParams.Length];
                    for (int i = 0; i < OriginalSupportedLanguageParams.Length; i++)
                    {
                        OriginalSupportedLanguageList[i] = OriginalSupportedLanguageParams[i].code;
                    }
                }
                if (!string.IsNullOrEmpty(Term))
                {
                    // if we have a language override, use our override settings, otherwise we follow the game settings
                    string TargetLanguage = string.IsNullOrEmpty(OurSelectedLanguageOverride) ? LocalizationManager.CurrentLanguageCode : OurSelectedLanguageOverride;
                    if (OurTranslations.ContainsKey(TargetLanguage))
                    {
                        // search for the term in our translation list
                        if (OurTranslations[TargetLanguage].TryGetValue(Term, out __result))
                        {
                            LogMessage("Translation Matched! lang=" + TargetLanguage + " term=" + Term + " result=" + __result);
                            return false;
                        }
                    }
                }
                if (!OriginalSupportedLanguageList.Contains(LocalizationManager.CurrentLanguageCode))
                {
                    // if we are overriding the language but failed to match the specified term in our translation list,
                    // (or in case of a weird edge case when the game language is set to some one that the game does not support)
                    // we will let the game fetch its original English text (aka. language fallback)
                    LocalizationManager.CurrentLanguageCode = "en-US";
                }
                return true;
            }
            public static void Postfix(string Term, ref string __result, ref string __state)
            {
                LocalizationManager.CurrentLanguageCode = __state; // set the game language setting to what it was before
                LogMessage("Translation result: lang=" + LocalizationManager.CurrentLanguageCode + " term=" + Term + " result=" + __result);
            }
        }

        [HarmonyPatch(typeof(LocalizeTarget_UnityStandard_AudioSource))]
        [HarmonyPatch("DoLocalize")] // the game use this method to fetch translated audio clips
        public class UnityStandardAudioSourcePatch
        {
            // "mainTranslation" is the term for the specified audio clip
            public static bool Prefix(LocalizeTarget_UnityStandard_AudioSource __instance, Localize cmp, string mainTranslation)
            {
                // code copied from the original method
                bool num = (__instance.mTarget.isPlaying || __instance.mTarget.loop) && Application.isPlaying;
                AudioClip clip = __instance.mTarget.clip;

                AudioClip audioClip = null; // = cmp.FindTranslatedObject<AudioClip>(mainTranslation);

                // my replacement code
                bool AudioMatched = false;
                if (string.IsNullOrEmpty(mainTranslation))
                {
                    return true;
                }
                if (!AudioClipsLoaded)
                {
                    // if we haven't finished loading our audio clips, we wait until it does
                    AudioClipsLoadedEvent.WaitOne();
                }
                if (AudioMatched = OurAudioClips.TryGetValue(mainTranslation, out audioClip))
                {
                    LogMessage("Audio: term matched in OurAudioClips !!! " + mainTranslation);
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
                    if (!string.IsNullOrEmpty(OurSelectedLanguageOverride))
                    {
                        fallback = fallback.Replace(OurSelectedLanguageOverride, "en-US");
                    }
                    audioClip = cmp.FindTranslatedObject<AudioClip>(fallback);
                    AudioMatched = audioClip != null;
                }
                if (!AudioMatched)
                {
                    // and if all of the above didn't work, we'll let the game handle it
                    return true;
                }

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

        /// <summary>
        /// I just want a copy constructor
        /// </summary>
        public class OurSupportedLanguageParams : SupportedLanguageParams
        {
            public OurSupportedLanguageParams(SupportedLanguageParams source)
            {
                this.code = source.code;
                this.localizedAudio = source.localizedAudio;
                this.assetReference = source.assetReference;
            }
        }
    }
}
