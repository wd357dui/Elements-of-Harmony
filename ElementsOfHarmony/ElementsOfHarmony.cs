using System;
using System.IO;

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
					UnityEngine.Debug.LogError($"UnityEngine.StackTraceUtility.ExtractStackTrace()\r\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");
					UnityEngine.Debug.LogError($"e.StackTrace\r\n{e.StackTrace}");
					UnityEngine.Debug.LogError($"e.Message {e.Message}");
				}

				try
				{
					Settings.ReadOurSettings();
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogError($"UnityEngine.StackTraceUtility.ExtractStackTrace()\r\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");
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
					KinectControl.Init();
				}
			}
		}

		public static bool IsAMBA => UnityEngine.Application.productName == "MLP";
		public static bool IsAZHM => false; // wait for the game to come out
	}
}
