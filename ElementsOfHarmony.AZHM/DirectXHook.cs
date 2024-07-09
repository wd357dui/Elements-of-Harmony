using DrakharStudio.Engine;
using HarmonyLib;
using System;
using System.Runtime.InteropServices;

namespace ElementsOfHarmony.AZHM
{
	public class DirectXHook
	{
		public static void Init()
		{
			// 2024-7-5
			// my initial diagnosis on compatibility with Steam overlay concludes that
			// the crash occurs because Steam has detour hooked MY IDXGISwapChain::Present callback
			// instead of the original one, switching to detour hook does NOT fix this issue;
			// I've disabled ResolutionManager yet it's still doing that
			// at this point it's just not worth it to deal with Steam overlay's stupidity anymore
			// I've already done so much to work around its Present and ResizeBuffers hooks
			// I can't possibly go and teach it how to do the most basic stuff
			// like AVOIDING DUPLICATE INITIALIZATION!!!
			/*
			if (DoNotInstallHook = GetModuleHandle("GameOverlayRenderer64.dll") != IntPtr.Zero)
			{
				Log.MessageBox(IntPtr.Zero,
					$"{typeof(DirectXHook)} - I can't find a way to make my hook to work alongside Steam overlay.\r\n" +
					$"I did with AMBA, but in this game, too much stuff is wrong " +
					$"I don't even know where to look...\r\n" +
					$"These features won't work when Steam overlay is enabled:\r\n" +
					$"1. Direct2D overlay (including KinectControl feedback overlay)\r\n" +
					$"2. HDR display support\r\n" +
					$"please disable Steam overlay if you want to use these features.\r\n" +
					$"the most reliable way I know to disable Steam overlay is to crack the game" +
					$"(replace steam api DLLs with cracked ones, can be from other games)" +
					$"to prevent it from launching from Steam",
					"Steam overlay hook not compatible", Log.MB_OK | Log.MB_ICONWARNING);
			}
			*/

			// 2024-7-8
			// well what do you know...
			// the same code that used to work on AMBA suddenly stopped working!
			// my sanity is completely broken...
			// I thought ResizeBuffers was the problem and I thought I fixed that...
		}

		[HarmonyPatch(typeof(ResolutionManager), methodName: "Update")]
		public static class TurnOffResolutionChangeEverySecond
		{
			public static bool Prefix()
			{
				return false;
			}
		}

		//[DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
		//internal extern static IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
	}
}
