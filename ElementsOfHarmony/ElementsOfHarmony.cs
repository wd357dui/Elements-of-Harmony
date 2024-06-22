using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace ElementsOfHarmony
{
	// Yes, I named the project "Elements of Harmony",
	// because I'm using Harmony API and I'm modding a MLP game (sorry no sorry lol)
	public static class ElementsOfHarmony
	{
		private static readonly object ExistenceMutex = new object();
		private static volatile bool Existed = false;

		[UnityEngine.RuntimeInitializeOnLoadMethod(loadType: UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
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
					UnityEngine.Debug.LogError(e.GetType());
					UnityEngine.Debug.LogError(UnityEngine.StackTraceUtility.ExtractStackTrace());
					UnityEngine.Debug.LogError($"e.StackTrace\r\n{e.StackTrace}");
					UnityEngine.Debug.LogError($"e.Message {e.Message}");
				}

				try
				{
					Settings.ReadOurSettings();
					Settings.WriteOurSettings();
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogError(e.GetType());
					UnityEngine.Debug.LogError(UnityEngine.StackTraceUtility.ExtractStackTrace());
					UnityEngine.Debug.LogError($"e.StackTrace\r\n{e.StackTrace}");
					UnityEngine.Debug.LogError($"e.Message {e.Message}");
				}

				Log.InitDebug();

				Localization.Init();

				if (Settings.DirectXHook.Enabled)
				{
					DirectXHook.Init();
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
						Log.Message($"{MethodBase.GetCurrentMethod()} -> Settings.Loyalty.KinectControl - {e.GetType()}\n{e.StackTrace}\n{e.Message}");
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
			}
		}

		public static bool IsAMBA => UnityEngine.Application.companyName == "Melbot Studios" && UnityEngine.Application.productName == "MLP";
		public static bool IsAZHM => UnityEngine.Application.companyName == "DrakharStudio" && UnityEngine.Application.productName == "MyLittlePonyZephyrHeights";

		public static string AssemblyDirectory => Path.Combine(Directory.GetCurrentDirectory(), "Elements of Harmony/Managed/");
	}
}
