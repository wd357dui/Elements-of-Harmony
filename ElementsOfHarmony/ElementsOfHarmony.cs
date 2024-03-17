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
				catch (Exception) { }

				try
				{
					Settings.ReadOurSettings();
				}
				catch (Exception) { }

				Log.InitDebug();

				Localization.Init();

				if (Settings.DirectXHookEnabled)
				{
					DirectXHook.Init();
				}
			}
		}

		public static bool IsAMBA => UnityEngine.Application.productName == "MLP";
		public static bool IsAZHM => false; // wait for the game to come out
	}
}
