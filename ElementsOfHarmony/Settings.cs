using System;

namespace ElementsOfHarmony
{
	public static class Settings
	{
		private static EnvFile Config;
		private static bool Debug_Internal = false;
		private static bool DebugTCPEnabled_Internal = false;
		private static string DebugTCPIP_Internal = "localhost";
		private static int DebugTCPPort_Internal = 1024;
		private static bool DebugLog_Internal = false;
		private static string DebugLogFile_Internal = "Elements of Harmony/Elements of Harmony.log";
		
		public static bool Debug => Debug_Internal;
		public static bool DebugTCPEnabled => DebugTCPEnabled_Internal;
		public static string DebugTCPIP => DebugTCPIP_Internal;
		public static int DebugTCPPort => DebugTCPPort_Internal;
		public static bool DebugLog => DebugLog_Internal;
		public static string DebugLogFile => DebugLogFile_Internal;

		private static string OurSelectedLanguageOverride_Internal = "";
		private static string OurFallbackLanguage_Internal = "en-US";
		public static string OurSelectedLanguageOverride
		{
			get { return OurSelectedLanguageOverride_Internal; }
			set { OurSelectedLanguageOverride_Internal = value; try { WriteOurSettings(); } catch (Exception) { } }
		}
		public static string OurFallbackLanguage
		{
			get { return OurFallbackLanguage_Internal; }
			set { OurFallbackLanguage_Internal = value; try { WriteOurSettings(); } catch (Exception) { } }
		}

		public static void ReadOurSettings()
		{
			// this settings exist because the game can't save a language setting that it doesn't originally support into the game save
			// (so I took the matter into my own hooves)
			// example:
			// OurSelectedLanguageOverride=zh-CN
			// (the value is the ISO language code, same with your translation file name, case sensitive)
			Config = new EnvFile("Elements of Harmony/Settings.txt");
			OurSelectedLanguageOverride_Internal = Config.ReadString("OurSelectedLanguageOverride", OurSelectedLanguageOverride_Internal);
			OurFallbackLanguage_Internal = Config.ReadString("OurFallbackLanguage", OurFallbackLanguage_Internal);

			Debug_Internal = Config.ReadBoolean("Debug", Debug_Internal);

			DebugTCPEnabled_Internal = Config.ReadBoolean("Debug.TCP.Enabled", DebugTCPEnabled_Internal);
			DebugTCPIP_Internal = Config.ReadString("Debug.TCP.IP", DebugTCPIP_Internal);
			DebugTCPPort_Internal = Config.ReadInteger("Debug.TCP.Port", DebugTCPPort_Internal);

			DebugLog_Internal = Config.ReadBoolean("Debug.Log.Enabled", DebugLog_Internal);
			DebugLogFile_Internal = Config.ReadString("Debug.Log.File", DebugLogFile_Internal);

			WriteOurSettings();
		}
		public static void WriteOurSettings()
		{
			Config.WriteString("OurSelectedLanguageOverride", OurSelectedLanguageOverride);
			Config.WriteString("OurFallbackLanguage", OurFallbackLanguage);

			Config.WriteBoolean("Debug", Debug_Internal);

			Config.WriteBoolean("Debug.TCP.Enabled", DebugTCPEnabled_Internal);
			Config.WriteString("Debug.TCP.IP", DebugTCPIP_Internal);
			Config.WriteInteger("Debug.TCP.Port", DebugTCPPort_Internal);

			Config.WriteBoolean("Debug.Log.Enabled", DebugLog_Internal);
			Config.WriteString("Debug.Log.File", DebugLogFile_Internal);

			Config.SaveConfig();
		}
	}
}
