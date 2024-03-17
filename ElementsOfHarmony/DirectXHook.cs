using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ElementsOfHarmony
{
	public static class DirectXHook
	{
		private static readonly Thread SynchornizationThread = new Thread(Sync);
		public static void Init()
		{
			try
			{
				int HResult = InstallHook();
				Log.Message($"InstallHook() returns {HResult:X}");
				if (HResult == Discord)
				{
					_ = new ThirdPartyNonDetourHookDetectedException();
				}
				else {
					Marshal.ThrowExceptionForHR(HResult);
				}
				UnityEngine.Application.quitting += Application_quitting;
				SynchornizationThread.Start();
				SetRunning(true);
				Log.Message("DirectX hook complete");
			}
			catch (Exception) { }
			finally
			{
				IntPtr DllAddress = Get_DXGI_DLL_Address();
				uint DllImageSize = Get_DXGI_DLL_ImageSize();
				Log.Message($"Address space for DXGI.dll {DllAddress.ToInt64():X} - {DllAddress.ToInt64() + DllImageSize:X}");
				Log.Message($"Get_IDXGISwapChain_Present_Original() {Get_IDXGISwapChain_Present_Original().ToInt64():X}");
				Log.Message($"Get_IDXGISwapChain1_Present1_Original() {Get_IDXGISwapChain1_Present1_Original().ToInt64():X}");
				Log.Message($"Get_IDXGIFactory_CreateSwapChain_Original() {Get_IDXGIFactory_CreateSwapChain_Original().ToInt64():X}");
				Log.Message($"Get_IDXGIFactory2_CreateSwapChainForHwnd_Original() {Get_IDXGIFactory2_CreateSwapChainForHwnd_Original().ToInt64():X}");
				Log.Message($"Get_Present_PreviousDetourHookDetected() {Get_Present_PreviousDetourHookDetected()}");
				Log.Message($"Get_Present1_PreviousDetourHookDetected() {Get_Present1_PreviousDetourHookDetected()}");
				Log.Message($"Get_Present_HasLoadedFirstFiveBytesOfInstruction() {Get_Present_HasLoadedFirstFiveBytesOfInstruction()}");
				Log.Message($"Get_Present_HasOriginalFirstFiveBytesOfInstruction() {Get_Present_HasOriginalFirstFiveBytesOfInstruction()}");
				Log.Message($"Get_Present1_HasLoadedFirstFiveBytesOfInstruction() {Get_Present1_HasLoadedFirstFiveBytesOfInstruction()}");
				Log.Message($"Get_Present1_HasOriginalFirstFiveBytesOfInstruction() {Get_Present1_HasOriginalFirstFiveBytesOfInstruction()}");

				DllAddress = Get_GameOverlayRenderer64_DLL_Address();
				DllImageSize = Get_GameOverlayRenderer64_DLL_ImageSize();
				bool HasGameOverlayRenderer64 = DllAddress != IntPtr.Zero;
				if (HasGameOverlayRenderer64)
				{
					Log.Message($"Address space for GameOverlayRenderer64.dll {DllAddress.ToInt64():X} - {DllAddress.ToInt64() + DllImageSize:X}");
				}

				if (Get_Present_HasLoadedFirstFiveBytesOfInstruction())
				{
					Refresh_Present_LoadedFirstFiveBytesOfInstruction();
					IntPtr Present_Loaded = Get_Present_LoadedFirstFiveBytesOfInstruction();
					byte[] Present_Loaded_Bytes = new byte[5];
					Marshal.Copy(Present_Loaded, Present_Loaded_Bytes, 0, 5);
					int? Addr = null;
					Log.Message($"Get_Present_LoadedFirstFiveBytesOfInstruction() " +
						$"{Present_Loaded_Bytes[0]:X2} " +
						$"{Present_Loaded_Bytes[1]:X2} " +
						$"{Present_Loaded_Bytes[2]:X2} " +
						$"{Present_Loaded_Bytes[3]:X2} " +
						$"{Present_Loaded_Bytes[4]:X2} " +
						((Present_Loaded_Bytes[0] == OpCode_jmp) ? $"(jmp {Addr = BitConverter.ToInt32(Present_Loaded_Bytes, 1):X})" : ""));
					if (Addr.HasValue && DllAddress != IntPtr.Zero)
					{
						int result = JmpEndsUpInRange(Get_IDXGISwapChain_Present_Original(), DllAddress, DllImageSize);
						if (result == 1)
						{
							Log.Message($"Present() jmp ends up in GameOverlayRenderer64.dll, which means this is a steam overlay hook");
						}
						else if (result == 0)
						{
							Log.Message($"Present() jmp did not end up in GameOverlayRenderer64.dll");
							Log.Message($"last instruction = {JmpEndsUpInRange_LastInstruction():X}");
							Log.Message($"last address = {JmpEndsUpInRange_LastAddress().ToInt64():X}");
						}
						else if (result == -1)
						{
							_ = Marshal.GetExceptionForHR((int)JmpEndsUpInRange_LastError());
						}
						Log.Message($"result = {result} (1=true,0=false,-1=error)");
						Log.Message($"last error = {JmpEndsUpInRange_LastError()}");
					}
				}

				if (Get_Present_HasOriginalFirstFiveBytesOfInstruction())
				{
					IntPtr Present_Original = Get_Present_OriginalFirstFiveBytesOfInstruction();
					byte[] Present_Original_Bytes = new byte[5];
					Marshal.Copy(Present_Original, Present_Original_Bytes, 0, 5);
					Log.Message($"Get_Present_OriginalFirstFiveBytesOfInstruction() " +
						$"{Present_Original_Bytes[0]:X2} " +
						$"{Present_Original_Bytes[1]:X2} " +
						$"{Present_Original_Bytes[2]:X2} " +
						$"{Present_Original_Bytes[3]:X2} " +
						$"{Present_Original_Bytes[4]:X2} ");
				}

				if (Get_Present1_HasLoadedFirstFiveBytesOfInstruction())
				{
					Refresh_Present1_LoadedFirstFiveBytesOfInstruction();
					IntPtr Present1_Loaded = Get_Present1_LoadedFirstFiveBytesOfInstruction();
					byte[] Present1_Loaded_Bytes = new byte[5];
					Marshal.Copy(Present1_Loaded, Present1_Loaded_Bytes, 0, 5);
					int? Addr = null;
					Log.Message($"Get_Present1_LoadedFirstFiveBytesOfInstruction() " +
						$"{Present1_Loaded_Bytes[0]:X2} " +
						$"{Present1_Loaded_Bytes[1]:X2} " +
						$"{Present1_Loaded_Bytes[2]:X2} " +
						$"{Present1_Loaded_Bytes[3]:X2} " +
						$"{Present1_Loaded_Bytes[4]:X2} " +
						((Present1_Loaded_Bytes[0] == OpCode_jmp) ? $"(jmp {Addr = BitConverter.ToInt32(Present1_Loaded_Bytes, 1):X})" : ""));
					if (Addr.HasValue && DllAddress != IntPtr.Zero)
					{
						int result = JmpEndsUpInRange(Get_IDXGISwapChain1_Present1_Original(), DllAddress, DllImageSize);
						if (result == 1)
						{
							Log.Message($"Present1() jmp ends up in GameOverlayRenderer64.dll, which means this is a steam overlay hook");
						}
						else if (result == 0)
						{
							Log.Message($"Present1() jmp did not end up in GameOverlayRenderer64.dll");
							Log.Message($"last instruction = {JmpEndsUpInRange_LastInstruction():X}");
							Log.Message($"last address = {JmpEndsUpInRange_LastAddress().ToInt64():X}");
						}
						else if (result == -1)
						{
							_ = Marshal.GetExceptionForHR((int)JmpEndsUpInRange_LastError());
						}
						Log.Message($"result = {result} (1=true,0=false,-1=error)");
						Log.Message($"last error = {JmpEndsUpInRange_LastError()}");
					}
				}

				if (Get_Present1_HasOriginalFirstFiveBytesOfInstruction())
				{
					IntPtr Present1_Original = Get_Present1_OriginalFirstFiveBytesOfInstruction();
					byte[] Present1_Original_Bytes = new byte[5];
					Marshal.Copy(Present1_Original, Present1_Original_Bytes, 0, 5);
					Log.Message($"Get_Present1_OriginalFirstFiveBytesOfInstruction() " +
						$"{Present1_Original_Bytes[0]:X2} " +
						$"{Present1_Original_Bytes[1]:X2} " +
						$"{Present1_Original_Bytes[2]:X2} " +
						$"{Present1_Original_Bytes[3]:X2} " +
						$"{Present1_Original_Bytes[4]:X2} ");
				}
			}
		}

		private static void Application_quitting()
		{
			SetRunning(false);
			Marshal.GetExceptionForHR(UninstallHook());
		}

		private static void Sync()
		{
			while (true)
			{
				int ID = BeginProcessCompletionEvent();
				switch (ID)
				{
					case 10: // CreateSwapChain
					case 15: // CreateSwapChainForHwnd
						SwapChainCreated?.Invoke();
						break;
					case 8: // Present
					case 22: // Present1
						SwapChainPresented?.Invoke();
						break;
					case int.MaxValue:
						return;
				}
				Marshal.ThrowExceptionForHR(EndProcessCompletionEvent());
			}
		}

		public static event Action SwapChainCreated;
		public static event Action SwapChainPresented;

		public static void SetSwapChainCreationArgsOverride(uint? Width, uint? Height, DXGI_RATIONAL? RefreshRate, DXGI_FORMAT? Format, DXGI_SAMPLE_DESC? SampleDesc, uint? BufferCount)
		{
			int RequiredSize = Marshal.SizeOf<uint>() + Marshal.SizeOf<uint>() +
				Marshal.SizeOf<DXGI_RATIONAL>() + Marshal.SizeOf<DXGI_FORMAT>() + Marshal.SizeOf<DXGI_SAMPLE_DESC>() + Marshal.SizeOf<uint>();
			IntPtr Memory = Marshal.AllocHGlobal(RequiredSize);
			IntPtr WidthPtr = IntPtr.Zero, HeightPtr = IntPtr.Zero, RefreshRatePtr = IntPtr.Zero, FormatPtr = IntPtr.Zero, SampleDescPtr = IntPtr.Zero, BufferCountPtr = IntPtr.Zero;
			IntPtr Pos = Memory;

			if (Width.HasValue)
			{
				WidthPtr = Pos;
				Marshal.WriteInt32(WidthPtr, (int)Width.Value);
			}
			Pos += Marshal.SizeOf<uint>();

			if (Height.HasValue)
			{
				HeightPtr = Pos;
				Marshal.WriteInt32(HeightPtr, (int)Height.Value);
			}
			Pos += Marshal.SizeOf<uint>();

			if (RefreshRate.HasValue)
			{
				RefreshRatePtr = Pos;
				Marshal.StructureToPtr(RefreshRate.Value, RefreshRatePtr, false);
			}
			Pos += Marshal.SizeOf<DXGI_RATIONAL>();

			if (Format.HasValue)
			{
				FormatPtr = Pos;
				Marshal.WriteInt32(FormatPtr, (int)Format.Value);
			}
			Pos += Marshal.SizeOf<DXGI_FORMAT>();

			if (SampleDesc.HasValue)
			{
				SampleDescPtr = Pos;
				Marshal.StructureToPtr(SampleDesc.Value, SampleDescPtr, false);
			}
			Pos += Marshal.SizeOf<DXGI_SAMPLE_DESC>();

			if (BufferCount.HasValue)
			{
				BufferCountPtr = Pos;
				Marshal.WriteInt32(BufferCountPtr, (int)BufferCount.Value);
			}

			SetSwapChainCreationArgsOverride(WidthPtr, HeightPtr, RefreshRatePtr, FormatPtr, SampleDescPtr, BufferCountPtr);
			Marshal.FreeHGlobal(Memory);
		}

		public static void SetSwapChainPresentArgsOverride(uint? SyncInterval)
		{
			IntPtr Memory = Marshal.AllocHGlobal(Marshal.SizeOf<IntPtr>());
			IntPtr Arg = IntPtr.Zero;
			if (SyncInterval.HasValue)
			{
				Marshal.WriteInt32(Memory, (int)SyncInterval.Value);
				Arg = Memory;
			}
			SetSwapChainPresentArgsOverride(Arg);
			Marshal.FreeHGlobal(Memory);
		}

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int InstallHook();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int UninstallHook();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int SetRunning(bool Running);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static void SetSwapChainCreationArgsOverride(IntPtr Width, IntPtr Height, IntPtr RefreshRate, IntPtr Format, IntPtr SampleDesc, IntPtr BufferCount);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static void SetSwapChainPresentArgsOverride(IntPtr SyncInterval);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int BeginProcessCompletionEvent();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int EndProcessCompletionEvent();

		#region functions for getting global variables in DirectXHook.dll

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_DXGI_DLL_Address();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static uint Get_DXGI_DLL_ImageSize();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_IDXGISwapChain_Present_Original();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_IDXGISwapChain1_Present1_Original();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_IDXGIFactory_CreateSwapChain_Original();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_IDXGIFactory2_CreateSwapChainForHwnd_Original();

		#region for dealing with other hooks

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static bool Get_Present_PreviousDetourHookDetected();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static bool Get_Present1_PreviousDetourHookDetected();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_GameOverlayRenderer64_DLL_Address();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static uint Get_GameOverlayRenderer64_DLL_ImageSize();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static bool Get_Present_HasOriginalFirstFiveBytesOfInstruction();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static bool Get_Present1_HasOriginalFirstFiveBytesOfInstruction();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static bool Get_Present_HasLoadedFirstFiveBytesOfInstruction();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static bool Get_Present1_HasLoadedFirstFiveBytesOfInstruction();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static void Refresh_Present_LoadedFirstFiveBytesOfInstruction();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static void Refresh_Present1_LoadedFirstFiveBytesOfInstruction();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_Present_OriginalFirstFiveBytesOfInstruction();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_Present1_OriginalFirstFiveBytesOfInstruction();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_Present_LoadedFirstFiveBytesOfInstruction();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_Present1_LoadedFirstFiveBytesOfInstruction();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int JmpEndsUpInRange(IntPtr SrcAddr, IntPtr RangeStart, uint Size);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static byte JmpEndsUpInRange_LastInstruction();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr JmpEndsUpInRange_LastAddress();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static uint JmpEndsUpInRange_LastError();

		#endregion

		#endregion

		public const byte OpCode_jmp = 0xE9;

		public const uint Discord = 0xD15C03D;
		public class ThirdPartyNonDetourHookDetectedException : Exception
		{
			public ThirdPartyNonDetourHookDetectedException() : base($"A third-party non-detour DirectX hook detected!")
			{
				MessageBox(IntPtr.Zero,
					$"{typeof(DirectXHook).FullName}: A third-party non-detour DirectX hook detected!" + "\r\n" +
					"Keep in mind that if this other hook causes stack overflow exception I won't be able to fix it!",
					"I curse the name, the one behind it all", MB_ICONWARNING | MB_OK);
			}
		}

		[DllImport("User32.dll", CharSet = CharSet.Unicode)]
		internal extern static int MessageBox(IntPtr hWnd, string Text, string Caption, uint uType);

		internal const uint MB_OK = 0x00000000;
		internal const uint MB_ICONWARNING = 0x00000030;
	}

	public enum DXGI_FORMAT : uint
	{
		DXGI_FORMAT_UNKNOWN = 0,
		DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
		DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
		DXGI_FORMAT_R32G32B32A32_UINT = 3,
		DXGI_FORMAT_R32G32B32A32_SINT = 4,
		DXGI_FORMAT_R32G32B32_TYPELESS = 5,
		DXGI_FORMAT_R32G32B32_FLOAT = 6,
		DXGI_FORMAT_R32G32B32_UINT = 7,
		DXGI_FORMAT_R32G32B32_SINT = 8,
		DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
		DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
		DXGI_FORMAT_R16G16B16A16_UNORM = 11,
		DXGI_FORMAT_R16G16B16A16_UINT = 12,
		DXGI_FORMAT_R16G16B16A16_SNORM = 13,
		DXGI_FORMAT_R16G16B16A16_SINT = 14,
		DXGI_FORMAT_R32G32_TYPELESS = 15,
		DXGI_FORMAT_R32G32_FLOAT = 16,
		DXGI_FORMAT_R32G32_UINT = 17,
		DXGI_FORMAT_R32G32_SINT = 18,
		DXGI_FORMAT_R32G8X24_TYPELESS = 19,
		DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
		DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
		DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
		DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
		DXGI_FORMAT_R10G10B10A2_UNORM = 24,
		DXGI_FORMAT_R10G10B10A2_UINT = 25,
		DXGI_FORMAT_R11G11B10_FLOAT = 26,
		DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
		DXGI_FORMAT_R8G8B8A8_UNORM = 28,
		DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
		DXGI_FORMAT_R8G8B8A8_UINT = 30,
		DXGI_FORMAT_R8G8B8A8_SNORM = 31,
		DXGI_FORMAT_R8G8B8A8_SINT = 32,
		DXGI_FORMAT_R16G16_TYPELESS = 33,
		DXGI_FORMAT_R16G16_FLOAT = 34,
		DXGI_FORMAT_R16G16_UNORM = 35,
		DXGI_FORMAT_R16G16_UINT = 36,
		DXGI_FORMAT_R16G16_SNORM = 37,
		DXGI_FORMAT_R16G16_SINT = 38,
		DXGI_FORMAT_R32_TYPELESS = 39,
		DXGI_FORMAT_D32_FLOAT = 40,
		DXGI_FORMAT_R32_FLOAT = 41,
		DXGI_FORMAT_R32_UINT = 42,
		DXGI_FORMAT_R32_SINT = 43,
		DXGI_FORMAT_R24G8_TYPELESS = 44,
		DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
		DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
		DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
		DXGI_FORMAT_R8G8_TYPELESS = 48,
		DXGI_FORMAT_R8G8_UNORM = 49,
		DXGI_FORMAT_R8G8_UINT = 50,
		DXGI_FORMAT_R8G8_SNORM = 51,
		DXGI_FORMAT_R8G8_SINT = 52,
		DXGI_FORMAT_R16_TYPELESS = 53,
		DXGI_FORMAT_R16_FLOAT = 54,
		DXGI_FORMAT_D16_UNORM = 55,
		DXGI_FORMAT_R16_UNORM = 56,
		DXGI_FORMAT_R16_UINT = 57,
		DXGI_FORMAT_R16_SNORM = 58,
		DXGI_FORMAT_R16_SINT = 59,
		DXGI_FORMAT_R8_TYPELESS = 60,
		DXGI_FORMAT_R8_UNORM = 61,
		DXGI_FORMAT_R8_UINT = 62,
		DXGI_FORMAT_R8_SNORM = 63,
		DXGI_FORMAT_R8_SINT = 64,
		DXGI_FORMAT_A8_UNORM = 65,
		DXGI_FORMAT_R1_UNORM = 66,
		DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
		DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
		DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
		DXGI_FORMAT_BC1_TYPELESS = 70,
		DXGI_FORMAT_BC1_UNORM = 71,
		DXGI_FORMAT_BC1_UNORM_SRGB = 72,
		DXGI_FORMAT_BC2_TYPELESS = 73,
		DXGI_FORMAT_BC2_UNORM = 74,
		DXGI_FORMAT_BC2_UNORM_SRGB = 75,
		DXGI_FORMAT_BC3_TYPELESS = 76,
		DXGI_FORMAT_BC3_UNORM = 77,
		DXGI_FORMAT_BC3_UNORM_SRGB = 78,
		DXGI_FORMAT_BC4_TYPELESS = 79,
		DXGI_FORMAT_BC4_UNORM = 80,
		DXGI_FORMAT_BC4_SNORM = 81,
		DXGI_FORMAT_BC5_TYPELESS = 82,
		DXGI_FORMAT_BC5_UNORM = 83,
		DXGI_FORMAT_BC5_SNORM = 84,
		DXGI_FORMAT_B5G6R5_UNORM = 85,
		DXGI_FORMAT_B5G5R5A1_UNORM = 86,
		DXGI_FORMAT_B8G8R8A8_UNORM = 87,
		DXGI_FORMAT_B8G8R8X8_UNORM = 88,
		DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
		DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
		DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
		DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
		DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
		DXGI_FORMAT_BC6H_TYPELESS = 94,
		DXGI_FORMAT_BC6H_UF16 = 95,
		DXGI_FORMAT_BC6H_SF16 = 96,
		DXGI_FORMAT_BC7_TYPELESS = 97,
		DXGI_FORMAT_BC7_UNORM = 98,
		DXGI_FORMAT_BC7_UNORM_SRGB = 99,
		DXGI_FORMAT_AYUV = 100,
		DXGI_FORMAT_Y410 = 101,
		DXGI_FORMAT_Y416 = 102,
		DXGI_FORMAT_NV12 = 103,
		DXGI_FORMAT_P010 = 104,
		DXGI_FORMAT_P016 = 105,
		DXGI_FORMAT_420_OPAQUE = 106,
		DXGI_FORMAT_YUY2 = 107,
		DXGI_FORMAT_Y210 = 108,
		DXGI_FORMAT_Y216 = 109,
		DXGI_FORMAT_NV11 = 110,
		DXGI_FORMAT_AI44 = 111,
		DXGI_FORMAT_IA44 = 112,
		DXGI_FORMAT_P8 = 113,
		DXGI_FORMAT_A8P8 = 114,
		DXGI_FORMAT_B4G4R4A4_UNORM = 115,

		DXGI_FORMAT_P208 = 130,
		DXGI_FORMAT_V208 = 131,
		DXGI_FORMAT_V408 = 132,

		DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE = 189,
		DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 190,

		DXGI_FORMAT_A4B4G4R4_UNORM = 191,
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct DXGI_RATIONAL
	{
		public uint Numerator;
		public uint Denominator;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct DXGI_SAMPLE_DESC
	{
		public uint Count;
		public uint Quality;
	}
}
