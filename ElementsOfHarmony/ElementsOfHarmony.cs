using System;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace ElementsOfHarmony
{
	// Yes, I named the project "Elements of Harmony",
	// because I'm using Harmony API and I'm modding a MLP game (sorry no sorry lol)
	public static class ElementsOfHarmony
	{
		private static readonly object ExistenceMutex = new object();
		private static volatile bool Existed = false;

		[RuntimeInitializeOnLoadMethod(loadType: RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		public static void Exist() // this is our "main" function
		{
			lock (ExistenceMutex)
			{
				if (Existed)
				{
					return;
				}
				else Existed = true;

				try
				{
					if (!Directory.Exists("Elements of Harmony"))
					{
						Directory.CreateDirectory("Elements of Harmony");
					}
				}
				catch (Exception e)
				{
					Debug.LogError(e.GetType());
					Debug.LogError(StackTraceUtility.ExtractStackTrace());
					Debug.LogError($"e.StackTrace\r\n{e.StackTrace}");
					Debug.LogError($"e.Message {e.Message}");
				}

				try
				{
					Settings.ReadOurSettings();
					Settings.WriteOurSettings();
				}
				catch (Exception e)
				{
					Debug.LogError(e.GetType());
					Debug.LogError(StackTraceUtility.ExtractStackTrace());
					Debug.LogError($"e.StackTrace\r\n{e.StackTrace}");
					Debug.LogError($"e.Message {e.Message}");
				}

				Log.InitDebug();

				Localization.Init();

				Action? DelayInit = null;
				if (Settings.DirectXHook.Enabled)
				{
					DirectXHook.Init(out DelayInit);
				}

				if (Settings.Loyalty.KinectControl.Enabled)
				{
					// 2024.4.30
					// I was going to use Kinect NuGet Package (for C#.Net Framework 4.5),
					// then after some failed tests and crushes,
					// I remembered why I didn't end up using it and went for C++ instead back in 2022
					// it was because Microsoft.Kinect.dll has native/managed mixed code in it
					// and Unity have zero tolerance for mixed code...
					Assembly KinectControl;
					try
					{
						KinectControl = Assembly.LoadFile($"{AssemblyDirectory}ElementsOfHarmony.KinectControl.dll");
						KinectControl.GetType("ElementsOfHarmony.KinectControl.KinectControl")
							.GetMethod("Init", BindingFlags.Public | BindingFlags.Static)
							.Invoke(null, Array.Empty<object>());
					}
					catch (Exception e) when (e.InnerException is DllNotFoundException dll)
					{
						if (dll.Message.EndsWith("Kinect20.dll", StringComparison.InvariantCultureIgnoreCase))
						{
							new Thread(() =>
							{
								int result = Log.MessageBox(IntPtr.Zero,
									"Unable to start `Elements of Harmony -> Loyalty -> Kinect Control` module, because Kinect20.dll was not found.\r\n" +
									"Please install Kinect V2 Runtime (or SDK) before running this module.\r\n" +
									"Would you like to go to the download page for Kinect V2 Runtime?",
									"Unable to load Kinect V2 library", Log.MB_YESNO | Log.MB_ICONWARNING);
								if (result == Log.IDYES)
								{
									System.Diagnostics.Process.Start("https://www.microsoft.com/download/details.aspx?id=44559");
								}
							}).Start();
						}
					}
					catch (Exception e)
					{
					repeat:
						Log.Message(StackTraceUtility.ExtractStackTrace());
						Log.Message($"{e.GetType()}\n{e.StackTrace}\n{e.Message}");
						if (e.InnerException != null)
						{
							e = e.InnerException;
							goto repeat;
						}
					}
				}

				if (Settings.Dance.Enabled)
				{
					Dance.Init();
				}

				DelayInit?.Invoke();
			}
		}

		public static bool IsAMBA => Application.companyName == "Melbot Studios" && Application.productName == "MLP";
		public static bool IsAZHM => Application.companyName == "DrakharStudio" && Application.productName == "MyLittlePonyZephyrHeights";

		public static string AssemblyDirectory => Path.Combine(Directory.GetCurrentDirectory(), "Elements of Harmony/Managed/");
	}
}
