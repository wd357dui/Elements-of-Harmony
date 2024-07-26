using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static ElementsOfHarmony.NativeInterface.DXGI;

namespace ElementsOfHarmony
{
	public static class Settings
	{
		private static EnvFile? Config;

		public static bool Debug = true;
		public static bool DebugTCPEnabled = false;
		public static string DebugTCPIP = "localhost";
		public static int DebugTCPPort = 1024;
		public static bool DebugLog = true;
		public static string DebugLogFile = "Elements of Harmony/Elements of Harmony.log";

		private static string OurSelectedLanguageOverride_Internal = "";
		private static string OurSelectedAudioLanguageOverride_Internal = "";
		public static string OurSelectedLanguageOverride
		{
			get => OurSelectedLanguageOverride_Internal;
			set => OurSelectedLanguageOverride_Internal = value;
		}
		public static string OurSelectedAudioLanguageOverride
		{
			get => OurSelectedAudioLanguageOverride_Internal;
			set => OurSelectedAudioLanguageOverride_Internal = value;
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
			public static bool? AllowHDR = null;
			public static bool? AllowDynamicResolution = null;

			public static DXGI_FORMAT? ForceSwapChainFormat = null;

			public static class Magic
			{
				public static class Overlay
				{
					public static bool Enabled = true;
					public static bool SolveCompatibilityWithHDR = true;
					public static bool ShowFrameTime = false;
				}
				public static class HDR
				{
					public static bool Forced = false;
					public static bool TakeOverOutputFormat = true;
					public static float DynamicRangeFactor = 1.0f;
				}
			}

			public static class URP
			{
				public static bool FabricateNewGlobalVolumeProfile = true;
				public static float? NewGlobalVolumeProfilePriority = null;
				public static float? NewGlobalVolumeProfileBlendDistance = null;
				public static float? NewGlobalVolumeProfileWeight = null;
				public static class Tonemapping
				{
					public static bool? Active = null;
					public static class Mode
					{
						public static bool? Override = null;
						public static TonemappingMode? Value = null;
					}
				}
				public static class ColorAdjustments
				{
					public static bool? Active = null;
					public static class PostExposure
					{
						public static bool? Override = null;
						public static float? Value = null;
					}
					public static class Contrast
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
						public static float? Max = null;
					}
					public static class ColorFilter
					{
						public static bool? Override = null;
						public static string? Value = null;
						public static bool? HDR = null;
						public static bool? ShowAlpha = null;
						public static bool? ShowEyeDropper = null;
					}
					public static class HueShift
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
						public static float? Max = null;
					}
					public static class Saturation
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
						public static float? Max = null;
					}
				}
				public static class Bloom
				{
					public static bool? Active = null;
					public static class Threshold
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
					}
					public static class Intensity
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
					}
					public static class Scatter
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
						public static float? Max = null;
					}
					public static class Clamp
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
					}
					public static class Tint
					{
						public static bool? Override = null;
						public static string? Value = null;
						public static bool? HDR = null;
						public static bool? ShowAlpha = null;
						public static bool? ShowEyeDropper = null;
					}
					public static class HighQualityFiltering
					{
						public static bool? Override = null;
						public static bool? Value = null;
					}
					public static class SkipIterations
					{
						public static bool? Override = null;
						public static int? Value = null;
						public static int? Min = null;
						public static int? Max = null;
					}
					public static class DirtTexture
					{
						public static bool? Override = null;
						public static string? Value = null;
					}
					public static class DirtIntensity
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
					}
				}
				public static class Vignette
				{
					public static bool? Active = null;
					public static class Color
					{
						public static bool? Override = null;
						public static string? Value = null;
						public static bool? HDR = null;
						public static bool? ShowAlpha = null;
						public static bool? ShowEyeDropper = null;
					}
					public static class Center
					{
						public static bool? Override = null;
						public static string? Value = null;
					}
					public static class Intensity
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
						public static float? Max = null;
					}
					public static class Smoothness
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
						public static float? Max = null;
					}
					public static class Rounded
					{
						public static bool? Override = null;
						public static bool? Value = null;
					}
				}
				public static class ShadowsMidtonesHighlights
				{
					public static bool? Active = null;
					public static class Shadows
					{
						public static bool? Override = null;
						public static string? Value = null;
					}
					public static class Midtones
					{
						public static bool? Override = null;
						public static string? Value = null;
					}
					public static class Highlights
					{
						public static bool? Override = null;
						public static string? Value = null;
					}
					public static class ShadowsStart
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
					}
					public static class ShadowsEnd
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
					}
					public static class HighlightsStart
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
					}
					public static class HighlightsEnd
					{
						public static bool? Override = null;
						public static float? Value = null;
						public static float? Min = null;
					}
				}
			}
		}

		public static class Loyalty
		{
			public static class KinectControl
			{
				public static bool Enabled = false;
				public static bool ShowOverlay = true;
				public static float StickSensitivity = 2.5f;
				public static float AnkleJumpThreshold = 0.2f;
				public static float HeadTiltMaxAngle = 30.0f;
			}
		}

		public static class Dance
		{
			public static bool Enabled = true;
			public static class Customization
			{
				public static bool Enabled = true;
			}
		}

        public static void ReadOurSettings()
		{
			// this settings exist originally because the game can't save a language setting that it doesn't originally support
			// into the game save
			// (so I took the matter into my own hooves)
			// example:
			// OurSelectedLanguageOverride=zh-CN
			// (the value is the ISO language code, same with your translation file name, case sensitive)
			// nowadays I use this to store all kinds of settings

			Config = new EnvFile("Elements of Harmony/Settings.txt"); // thanks Ponywka for writing `EnvFile` by the way
			OurSelectedLanguageOverride_Internal = Config!.ReadString("OurSelectedLanguageOverride", OurSelectedLanguageOverride_Internal)!;
			OurSelectedAudioLanguageOverride_Internal = Config!.ReadString("OurSelectedAudioLanguageOverride", OurSelectedAudioLanguageOverride_Internal)!;

			Debug = Config!.ReadBoolean("Debug", Debug);

			DebugTCPEnabled = Config!.ReadBoolean("Debug.TCP.Enabled", DebugTCPEnabled);
			DebugTCPIP = Config!.ReadString("Debug.TCP.IP", DebugTCPIP)!;
			DebugTCPPort = Config!.ReadInteger("Debug.TCP.Port", DebugTCPPort)!;

			DebugLog = Config!.ReadBoolean("Debug.Log.Enabled", DebugLog);
			DebugLogFile = Config!.ReadString("Debug.Log.File", DebugLogFile)!;

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
					try
					{
						if (Field.FieldType == typeof(bool?))
						{
							Field.SetValue(null, Config!.ReadBooleanOptional($"{ClassName}.{Field.Name}"));
						}
						else if (Field.FieldType == typeof(int?))
						{
							Field.SetValue(null, Config!.ReadIntegerOptional($"{ClassName}.{Field.Name}"));
						}
						else if (Field.FieldType == typeof(float?))
						{
							Field.SetValue(null, Config!.ReadFloatOptional($"{ClassName}.{Field.Name}"));
						}
						else if (Field.FieldType == typeof(string))
						{
							Field.SetValue(null, Config!.ReadString($"{ClassName}.{Field.Name}", Field.GetValue(null) as string));
						}
						else if (Field.FieldType == typeof(bool))
						{
							Field.SetValue(null, Config!.ReadBoolean($"{ClassName}.{Field.Name}", (bool)Field.GetValue(null)));
						}
						else if (Field.FieldType == typeof(int))
						{
							Field.SetValue(null, Config!.ReadInteger($"{ClassName}.{Field.Name}", (int)Field.GetValue(null)));
						}
						else if (Field.FieldType == typeof(float))
						{
							Field.SetValue(null, Config!.ReadFloat($"{ClassName}.{Field.Name}", (float)Field.GetValue(null)));
						}
						else if (Field.FieldType.IsEnum || Nullable.GetUnderlyingType(Field.FieldType)?.IsEnum == true)
						{
							Type EnumType = Field.FieldType.IsEnum ? Field.FieldType : Nullable.GetUnderlyingType(Field.FieldType);
							string[] EnumNames = Enum.GetNames(EnumType);
							object FinalEnumValue = Field.GetValue(null);
							if (Config!.ReadString($"{ClassName}.{Field.Name}", Field.GetValue(null)?.ToString()) is string Read)
							{
								string[] Flags = Read.Split('|');
								foreach (string Flag in Flags)
								{
									if ((from string EnumName in EnumNames
										where EnumName.Equals(Flag.Trim(), StringComparison.InvariantCultureIgnoreCase)
										select Enum.Parse(EnumType, EnumName, ignoreCase: true)).FirstOrDefault() is object Value)
									{
										FinalEnumValue = (int)(FinalEnumValue ?? 0) | (int)Value;
									}
								}
							}
							if (FinalEnumValue is int NumValue)
							{
								var ResultValue = Enum.ToObject(EnumType, NumValue);
								Field.SetValue(null, ResultValue);
							}
							else if (FinalEnumValue != null)
							{
								Field.SetValue(null, FinalEnumValue);
							}
							else
							{
								Field.SetValue(null, null);
							}
						}
					}
					catch (Exception e)
					{
					repeat:
						UnityEngine.Debug.LogError(StackTraceUtility.ExtractStackTrace());
						UnityEngine.Debug.LogError($"{e.GetType()}\n{e.StackTrace}\n{e.Message}");
						if (e.InnerException != null)
						{
							e = e.InnerException;
							goto repeat;
						}
					}
				}
				foreach (var Nested in ClassType.GetNestedTypes())
				{
					ReadSettingsForClass(Nested);
				}
			}
			foreach (var Nested in typeof(Settings).GetNestedTypes())
			{
				ReadSettingsForClass(Nested);
			}
		}
		public static void WriteOurSettings()
		{
			Config!.WriteString("OurSelectedLanguageOverride", OurSelectedLanguageOverride);
			Config!.WriteString("OurSelectedAudioLanguageOverride", OurSelectedAudioLanguageOverride);

			Config!.WriteBoolean("Debug", Debug);

			Config!.WriteBoolean("Debug.TCP.Enabled", DebugTCPEnabled);
			Config!.WriteString("Debug.TCP.IP", DebugTCPIP);
			Config!.WriteInteger("Debug.TCP.Port", DebugTCPPort);

			Config!.WriteBoolean("Debug.Log.Enabled", DebugLog);
			Config!.WriteString("Debug.Log.File", DebugLogFile);

			static void WriteSettingsForClass(Type ClassType)
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
					try
					{
						if (Field.FieldType == typeof(bool?))
						{
							Config!.WriteBooleanOptional($"{ClassName}.{Field.Name}", (bool?)Field.GetValue(null));
						}
						else if (Field.FieldType == typeof(int?))
						{
							Config!.WriteIntegerOptional($"{ClassName}.{Field.Name}", (int?)Field.GetValue(null));
						}
						else if (Field.FieldType == typeof(float?))
						{
							Config!.WriteFloatOptional($"{ClassName}.{Field.Name}", (float?)Field.GetValue(null));
						}
						else if (Field.FieldType == typeof(string))
						{
							Config!.WriteString($"{ClassName}.{Field.Name}", (string)Field.GetValue(null));
						}
						else if (Field.FieldType == typeof(bool))
						{
							Config!.WriteBoolean($"{ClassName}.{Field.Name}", (bool)Field.GetValue(null));
						}
						else if (Field.FieldType == typeof(int))
						{
							Config!.WriteInteger($"{ClassName}.{Field.Name}", (int)Field.GetValue(null));
						}
						else if (Field.FieldType == typeof(float))
						{
							Config!.WriteFloat($"{ClassName}.{Field.Name}", (float)Field.GetValue(null));
						}
						else if (Field.FieldType.IsEnum)
						{
							Config!.WriteString($"{ClassName}.{Field.Name}", Field.GetValue(null)?.ToString() ?? "");
						}
						else if (Nullable.GetUnderlyingType(Field.FieldType) is Type EnumType && EnumType.IsEnum)
						{
							Config!.WriteString($"{ClassName}.{Field.Name}", Field.GetValue(null)?.ToString() ?? "");
						}
					}
					catch { }
				}
				foreach (var Nested in ClassType.GetNestedTypes())
				{
					WriteSettingsForClass(Nested);
				}
			}
			foreach (var Nested in typeof(Settings).GetNestedTypes())
			{
				WriteSettingsForClass(Nested);
			}

			Config!.SaveConfig();
		}
	}
}
