using System;
using System.CodeDom;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ElementsOfHarmony
{
	public static class Settings
	{
		private static EnvFile Config;

		public static bool Debug = false;
		public static bool DebugTCPEnabled = false;
		public static string DebugTCPIP = "localhost";
		public static int DebugTCPPort = 1024;
		public static bool DebugLog = false;
		public static string DebugLogFile = "Elements of Harmony/Elements of Harmony.log";

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

		public static class DirectXHook
		{
			public static bool Enabled = true;
			public static int? Width = null;
			public static int? Height = null;
			public static FullScreenMode? FullScreenMode = null;
			public static int? RefreshRate = null;
			public static int? MSAA = null;
			public static int? VSyncInterval = null;
			public static int? TargetFrameRate = null;
			public static class URP
			{
				public static TonemappingMode? TonemappingMode = null;
				public static bool FabricateNewGlobalTonemappingProfile = false;
			}
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

			Debug = Config.ReadBoolean("Debug", Debug);

			DebugTCPEnabled = Config.ReadBoolean("Debug.TCP.Enabled", DebugTCPEnabled);
			DebugTCPIP = Config.ReadString("Debug.TCP.IP", DebugTCPIP);
			DebugTCPPort = Config.ReadInteger("Debug.TCP.Port", DebugTCPPort);

			DebugLog = Config.ReadBoolean("Debug.Log.Enabled", DebugLog);
			DebugLogFile = Config.ReadString("Debug.Log.File", DebugLogFile);

			void ReadSettingsForClass(Type ClassType)
			{
				string ClassName = ClassType.Name;
				Type RootClass = ClassType;
				while (RootClass.IsNested && RootClass.DeclaringType != typeof(Settings))
				{
					RootClass = RootClass.DeclaringType;
					ClassName = $"{RootClass.Name}.{ClassName}";
				}
				foreach (var Field in ClassType.GetFields())
				{
					if (Field.FieldType == typeof(bool))
					{
						Field.SetValue(null, Config.ReadBoolean($"{ClassName}.{Field.Name}", (bool)Field.GetValue(null)));
					}
					else if (Field.FieldType == typeof(int))
					{
						Field.SetValue(null, Config.ReadInteger($"{ClassName}.{Field.Name}", (int)Field.GetValue(null)));
					}
					else if (Field.FieldType == typeof(bool?))
					{
						Field.SetValue(null, Config.ReadBooleanOptional($"{ClassName}.{Field.Name}", (bool?)Field.GetValue(null)));
					}
					else if (Field.FieldType == typeof(int?))
					{
						Field.SetValue(null, Config.ReadIntegerOptional($"{ClassName}.{Field.Name}", (int?)Field.GetValue(null)));
					}
					else if (Field.FieldType == typeof(string))
					{
						Field.SetValue(null, Config.ReadString($"{ClassName}.{Field.Name}", Field.GetValue(null) as string));
					}
					else if (Field.FieldType.IsEnum || Nullable.GetUnderlyingType(Field.FieldType)?.IsEnum == true)
					{
						Type EnumType = Field.FieldType.IsEnum ? Field.FieldType : Nullable.GetUnderlyingType(Field.FieldType);
						string[] EnumNames = Enum.GetNames(EnumType);
						string Read = Config.ReadString($"{ClassName}.{Field.Name}", Field.GetValue(null)?.ToString());
						if (Read != null &&
							EnumNames.FirstOrDefault(N => N.Equals(Read, StringComparison.InvariantCultureIgnoreCase))
							is string MatchedEnumName)
						{
							Field.SetValue(null, Enum.Parse(EnumType, MatchedEnumName));
						}
					}
				}
			}
			ReadSettingsForClass(typeof(DirectXHook));
			ReadSettingsForClass(typeof(DirectXHook.URP));

			WriteOurSettings();
		}
		public static void WriteOurSettings()
		{
			Config.WriteString("OurSelectedLanguageOverride", OurSelectedLanguageOverride);
			Config.WriteString("OurFallbackLanguage", OurFallbackLanguage);

			Config.WriteBoolean("Debug", Debug);

			Config.WriteBoolean("Debug.TCP.Enabled", DebugTCPEnabled);
			Config.WriteString("Debug.TCP.IP", DebugTCPIP);
			Config.WriteInteger("Debug.TCP.Port", DebugTCPPort);

			Config.WriteBoolean("Debug.Log.Enabled", DebugLog);
			Config.WriteString("Debug.Log.File", DebugLogFile);

			void WriteSettingsForClass(Type ClassType)
			{
				string ClassName = ClassType.Name;
				Type RootClass = ClassType;
				while (RootClass.IsNested && RootClass.DeclaringType != typeof(Settings))
				{
					RootClass = RootClass.DeclaringType;
					ClassName = $"{RootClass.Name}.{ClassName}";
				}
				foreach (var Field in ClassType.GetFields())
				{
					if (Field.FieldType == typeof(bool))
					{
						Config.WriteBoolean($"{ClassName}.{Field.Name}", (bool)Field.GetValue(null));
					}
					else if (Field.FieldType == typeof(int))
					{
						Config.WriteInteger($"{ClassName}.{Field.Name}", (int)Field.GetValue(null));
					}
					else if (Field.FieldType == typeof(bool?))
					{
						Config.WriteBooleanOptional($"{ClassName}.{Field.Name}", (bool?)Field.GetValue(null));
					}
					else if (Field.FieldType == typeof(int?))
					{
						Config.WriteIntegerOptional($"{ClassName}.{Field.Name}", (int?)Field.GetValue(null));
					}
					else if (Field.FieldType == typeof(string))
					{
						Config.WriteString($"{ClassName}.{Field.Name}", (string)Field.GetValue(null));
					}
					else if (Field.FieldType.IsEnum || Nullable.GetUnderlyingType(Field.FieldType)?.IsEnum == true)
					{
						Config.WriteString($"{ClassName}.{Field.Name}", Field.GetValue(null)?.ToString());
					}
				}
			}
			WriteSettingsForClass(typeof(DirectXHook));
			WriteSettingsForClass(typeof(DirectXHook.URP));

			Config.SaveConfig();
		}
	}
}
