using HarmonyLib;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static ElementsOfHarmony.Settings.DirectXHook;

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
				else
				{
					Marshal.ThrowExceptionForHR(HResult);
				}
				Application.quitting += Application_quitting;
				SynchornizationThread.Start();
				SetRunning(true);

				// apply all of our patch procedures using Harmony API
				Harmony element = new Harmony($"{typeof(DirectXHook).FullName}");
				int Num = 0;
				foreach (var Patch in typeof(RenderHooks).GetNestedTypes())
				{
					element.CreateClassProcessor(Patch).Patch();
					Num++;
				}
				if (Num > 0)
				{
					Log.Message($"Harmony patch for {typeof(RenderHooks).FullName} successful - {Num} Patches");
				}

				SceneManager.sceneLoaded += SceneManager_sceneLoaded;

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
						Log.Message($"JmpEndsUpInRange(Get_IDXGISwapChain_Present_Original(), DllAddress, DllImageSize) = {result} (1=true,0=false,-1=error)");
						if (result == 1)
						{
							Log.Message($"jmp ends up in GameOverlayRenderer64.dll, which means this is a steam overlay hook");
						}
						else if (result == 0)
						{
							Log.Message($"jmp did not end up in GameOverlayRenderer64.dll");
							Log.Message($"JmpEndsUpInRange_LastInstruction() = {JmpEndsUpInRange_LastInstruction():X}");
							Log.Message($"JmpEndsUpInRange_LastAddress() = {JmpEndsUpInRange_LastAddress().ToInt64():X}");
						}
						else if (result == -1)
						{
							Log.Message($"JmpEndsUpInRange_LastError() = {JmpEndsUpInRange_LastError()}");
							_ = Marshal.GetExceptionForHR((int)JmpEndsUpInRange_LastError());
						}
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
						Log.Message($"JmpEndsUpInRange(Get_IDXGISwapChain1_Present1_Original(), DllAddress, DllImageSize) = {result} (1=true,0=false,-1=error)");
						if (result == 1)
						{
							Log.Message($"jmp ends up in GameOverlayRenderer64.dll, which means this is a steam overlay hook");
						}
						else if (result == 0)
						{
							Log.Message($"jmp did not end up in GameOverlayRenderer64.dll");
							Log.Message($"JmpEndsUpInRange_LastInstruction() = {JmpEndsUpInRange_LastInstruction():X}");
							Log.Message($"JmpEndsUpInRange_LastAddress() = {JmpEndsUpInRange_LastAddress().ToInt64():X}");
						}
						else if (result == -1)
						{
							Log.Message($"JmpEndsUpInRange_LastError() = {JmpEndsUpInRange_LastError()}");
							_ = Marshal.GetExceptionForHR((int)JmpEndsUpInRange_LastError());
						}
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

		private static readonly UnityEngine.Events.UnityAction<Scene, LoadSceneMode> SceneManager_sceneLoaded = (Scene arg0, LoadSceneMode arg1) =>
		{
			Screen.SetResolution(Width ?? Screen.width, Height ?? Screen.height,
				Settings.DirectXHook.FullScreenMode ?? Screen.fullScreenMode,
				RefreshRate ?? 0);
			QualitySettings.antiAliasing = MSAA ?? QualitySettings.antiAliasing;
			QualitySettings.vSyncCount = VSyncInterval ?? QualitySettings.vSyncCount;
			Application.targetFrameRate = TargetFrameRate ?? Application.targetFrameRate;
			SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
		};

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
					case 2: // a new swap chain is being created on the same hWnd,
							// so the old one should be released
							// meaning anyone else who is still referencing this IDXGISwapChain
							// should revoke their reference
						SwapChainShouldRelease?.Invoke();
						break;
					case -10: // CreateSwapChain
						SwapChainCreating?.Invoke();
						break;
					case 10: // CreateSwapChain
						SwapChainCreated?.Invoke();
						break;
					case -15: // CreateSwapChainForHwnd
						SwapChainForHwndCreating?.Invoke();
						break;
					case 15: // CreateSwapChainForHwnd
						SwapChainForHwndCreated?.Invoke();
						break;
					case -8: // Present
						SwapChainPresenting?.Invoke();
						break;
					case 8: // Present
						SwapChainPresented?.Invoke();
						break;
					case -22: // Present1
						SwapChain1Presenting?.Invoke();
						break;
					case 22: // Present1
						SwapChain1Presented?.Invoke();
						break;
					case int.MaxValue:
						return;
				}
				Marshal.ThrowExceptionForHR(EndProcessCompletionEvent());
			}
		}

		public static event Action SwapChainShouldRelease;
		public static event Action SwapChainCreating;
		public static event Action SwapChainCreated;
		public static event Action SwapChainForHwndCreating;
		public static event Action SwapChainForHwndCreated;
		public static event Action SwapChainPresenting;
		public static event Action SwapChainPresented;
		public static event Action SwapChain1Presenting;
		public static event Action SwapChain1Presented;

		#region DirectXHook.dll

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int InstallHook();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int UninstallHook();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int SetRunning(bool Running);

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

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_LocalVariablesArray();

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

		#endregion

		public static class RenderHooks
		{
			[HarmonyPatch(typeof(Volume))]
			[HarmonyPatch(methodName: "profileRef", methodType: MethodType.Getter)]
			public static class VolumnProfile_profileRef_getter
			{
				public static void Postfix(Volume __instance, VolumeProfile __result)
				{
					if (__instance.isGlobal)
					{
						if (UpdateVolumeFrameworkPatch.NewGlobalVolumeProfile != null &&
							!ReferenceEquals(UpdateVolumeFrameworkPatch.NewGlobalVolumeProfile, __result))
						{
							// there can only be one princess in equestria I mean... one global volume component of the same type active in an application
							foreach (var OurComponent in UpdateVolumeFrameworkPatch.NewGlobalVolumeProfile.components)
							{
								if (OurComponent.active &&
									__result.TryGet(OurComponent.GetType(), out VolumeComponent ConflictedComponent) &&
									!ReferenceEquals(ConflictedComponent, OurComponent) &&
									ConflictedComponent.active)
								{
									ConflictedComponent.active = false;
									Log.Message($"Conflicting volume component detected, " +
										$"OurComponent.name={OurComponent.name}, " +
										$"ConflictedComponent.name={ConflictedComponent.name}, " +
										$"ConflictedComponent.parameters.Count={ConflictedComponent.parameters.Count}, " +
										$"ConflictedComponent.parameters={{{string.Join(", ", ConflictedComponent.parameters)}}}, " +
										$"setting it to inactive");
								}
							}
						}
					}
				}
			}

			[HarmonyPatch(typeof(UniversalRenderPipeline))]
			[HarmonyPatch("UpdateVolumeFramework")]
			public static class UpdateVolumeFrameworkPatch
			{
				public static GameObject NewGlobalVolumeGameObject;
				public static VolumeProfile NewGlobalVolumeProfile;
				public static void Postfix()
				{
					if (URP.FabricateNewGlobalVolumeProfile &&
						NewGlobalVolumeProfile == null)
					{
						Log.Message($"fabricating a new global tonemapping profile");

						NewGlobalVolumeGameObject = new GameObject(nameof(NewGlobalVolumeGameObject))
						{
							layer = 0
						};

						Volume NewGlobalVolume = NewGlobalVolumeGameObject.AddComponent<Volume>();
						NewGlobalVolume.name = nameof(NewGlobalVolume);
						NewGlobalVolume.isGlobal = true;
						NewGlobalVolume.profile = NewGlobalVolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
						NewGlobalVolumeProfile.name = nameof(NewGlobalVolumeProfile);
						Log.Message($"fabricated a new global volume profile - " +
							$"GameObject={NewGlobalVolumeGameObject.name}, " +
							$"Volume={NewGlobalVolume.name}, " +
							$"VolumeProfile={NewGlobalVolumeProfile.name}");

						Tonemapping NewTonemapping = NewGlobalVolumeProfile.Add<Tonemapping>(false);
						NewTonemapping.active = false;
						NewTonemapping.name = nameof(NewTonemapping);
						if (URP.TonemappingMode is TonemappingMode mode)
						{
							NewTonemapping.mode.value = mode;
							NewTonemapping.mode.overrideState = true;
							NewTonemapping.active = true;
						}
						Log.Message($"fabricated a new tonemapping component - " +
							$"Tonemapping={NewTonemapping.name}, " +
							$"active={NewTonemapping.active}, " +
							$"value={NewTonemapping.mode.value}");

						ColorAdjustments NewColorAdjustments = NewGlobalVolumeProfile.Add<ColorAdjustments>(false);
						NewColorAdjustments.active = false;
						NewColorAdjustments.name = nameof(NewColorAdjustments);
						Log.Message($"fabricated a new color adjustments component - " +
							$"ColorAdjustments={NewColorAdjustments.name}");
						if (URP.ColorAdjustments.PostExposure is float postExposure)
						{
							NewColorAdjustments.postExposure.value = postExposure;
							NewColorAdjustments.postExposure.overrideState = true;
							NewColorAdjustments.active = true;
							Log.Message($"postExposure={NewColorAdjustments.postExposure.value}");
						}
						if (URP.ColorAdjustments.Contrast.Value is float contrast)
						{
							NewColorAdjustments.contrast.value = contrast;
							NewColorAdjustments.contrast.min = URP.ColorAdjustments.Contrast.Min ?? NewColorAdjustments.contrast.min;
							NewColorAdjustments.contrast.max = URP.ColorAdjustments.Contrast.Max ?? NewColorAdjustments.contrast.max;
							NewColorAdjustments.contrast.overrideState = true;
							NewColorAdjustments.active = true;
							Log.Message($"contrast={NewColorAdjustments.contrast.value}, " +
								$"min={NewColorAdjustments.contrast.min}, " +
								$"max={NewColorAdjustments.contrast.max}");
						}
						if (URP.ColorAdjustments.ColorFilter.Color is string color &&
							color.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries) is string[] RGBA &&
							RGBA.Length >= 3 && RGBA.Length <= 4 &&
							float.TryParse(RGBA[0].Trim(), out float R) &&
							float.TryParse(RGBA[1].Trim(), out float G) &&
							float.TryParse(RGBA[2].Trim(), out float B))
						{
							if (RGBA.Length == 4 && float.TryParse(RGBA[3].Trim(), out float A))
							{
								NewColorAdjustments.colorFilter.value = new Color(R, G, B, A);
							}
							else
							{
								NewColorAdjustments.colorFilter.value = new Color(R, G, B);
							}
							NewColorAdjustments.colorFilter.hdr = URP.ColorAdjustments.ColorFilter.HDR ?? NewColorAdjustments.colorFilter.hdr;
							NewColorAdjustments.colorFilter.showAlpha = URP.ColorAdjustments.ColorFilter.ShowAlpha ?? NewColorAdjustments.colorFilter.showAlpha;
							NewColorAdjustments.colorFilter.showEyeDropper = URP.ColorAdjustments.ColorFilter.ShowEyeDropper ?? NewColorAdjustments.colorFilter.showEyeDropper;
							NewColorAdjustments.colorFilter.overrideState = true;
							NewColorAdjustments.active = true;
							Log.Message($"colorFilter={NewColorAdjustments.colorFilter.value}, " +
								$"hdr={NewColorAdjustments.colorFilter.hdr}, " +
								$"showAlpha={NewColorAdjustments.colorFilter.showAlpha}, " +
								$"showEyeDropper={NewColorAdjustments.colorFilter.showEyeDropper}");
						}
						if (URP.ColorAdjustments.HueShift.Value is float hueShift)
						{
							NewColorAdjustments.hueShift.value = hueShift;
							NewColorAdjustments.hueShift.min = URP.ColorAdjustments.Contrast.Min ?? NewColorAdjustments.hueShift.min;
							NewColorAdjustments.hueShift.max = URP.ColorAdjustments.Contrast.Max ?? NewColorAdjustments.hueShift.max;
							NewColorAdjustments.hueShift.overrideState = true;
							NewColorAdjustments.active = true;
							Log.Message($"hueShift={NewColorAdjustments.hueShift.value}, " +
								$"min={NewColorAdjustments.hueShift.min}, " +
								$"max={NewColorAdjustments.hueShift.max}");
						}
						if (URP.ColorAdjustments.Saturation.Value is float saturation)
						{
							NewColorAdjustments.saturation.value = saturation;
							NewColorAdjustments.saturation.min = URP.ColorAdjustments.Saturation.Min ?? NewColorAdjustments.saturation.min;
							NewColorAdjustments.saturation.max = URP.ColorAdjustments.Saturation.Max ?? NewColorAdjustments.saturation.max;
							NewColorAdjustments.saturation.overrideState = true;
							NewColorAdjustments.active = true;
							Log.Message($"saturation={NewColorAdjustments.saturation.value}, " +
								$"min={NewColorAdjustments.saturation.min}, " +
								$"max={NewColorAdjustments.saturation.max}");
						}
						Log.Message($"NewColorAdjustments.active={NewColorAdjustments.active}");

						void OnActiveSceneChanged(Scene Old, Scene New)
						{
							SceneManager.MoveGameObjectToScene(NewGlobalVolumeGameObject, New);
							UnityEngine.Object.DontDestroyOnLoad(NewGlobalVolumeGameObject);
							Log.Message($"moving NewGlobalVolumeGameObject to the new scene {New.name}");
						}
						SceneManager.activeSceneChanged += OnActiveSceneChanged;
						if (SceneManager.sceneCount > 0)
						{
							OnActiveSceneChanged(SceneManager.GetSceneAt(0), SceneManager.GetSceneAt(0));
						}
						else
						{
							Log.Message("there are no scenes? wtf?");
						}
					}
				}
			}
		}

		public static class SwapChainShouldReleaseEventArgs
		{
			public static IntPtr IDXGISwapChain_SwapChain
			{
				get => Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size);
			}
		}

		public static class SwapChainCreatingEventArgs
		{
			public static IntPtr IDXGIFactory_This
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size));
				set => Marshal.WriteIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size), value);
			}
			public static IntPtr IUnknown_pDevice
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 1 * IntPtr.Size));
				set => Marshal.WriteIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 1 * IntPtr.Size), value);
			}
			public static IntPtr DXGI_SWAP_CHAIN_DESC_pDesc
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 2 * IntPtr.Size));
				set => Marshal.WriteIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 2 * IntPtr.Size), value);
			}
			public static DXGI_SWAP_CHAIN_DESC Desc
			{
				get => Marshal.PtrToStructure<DXGI_SWAP_CHAIN_DESC>(DXGI_SWAP_CHAIN_DESC_pDesc);
				set => Marshal.StructureToPtr(value, DXGI_SWAP_CHAIN_DESC_pDesc, false);
			}
		}

		public static class SwapChainCreatedEventArgs
		{
			public static IntPtr IDXGIFactory_This
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size));
			}
			public static IntPtr IUnknown_pDevice
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 1 * IntPtr.Size));
			}
			public static IntPtr DXGI_SWAP_CHAIN_DESC_pDesc
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 2 * IntPtr.Size));
			}
			public static IntPtr IDXGISwapChain_ppSwapChain
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 3 * IntPtr.Size));
			}
			public static DXGI_SWAP_CHAIN_DESC Desc
			{
				get => Marshal.PtrToStructure<DXGI_SWAP_CHAIN_DESC>(DXGI_SWAP_CHAIN_DESC_pDesc);
			}
			public static IntPtr IDXGISwapChain_ppSwapChain_Deref
			{
				get => Marshal.ReadIntPtr(IDXGISwapChain_ppSwapChain);
				set => Marshal.WriteIntPtr(IDXGISwapChain_ppSwapChain, value);
			}
		}

		public static class SwapChainForHwndCreatingEventArgs
		{
			public static IntPtr IDXGIFactory2_This
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size));
				set => Marshal.WriteIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size), value);
			}
			public static IntPtr IUnknown_pDevice
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 1 * IntPtr.Size));
				set => Marshal.WriteIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 1 * IntPtr.Size), value);
			}
			public static IntPtr HWND_hWnd
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 2 * IntPtr.Size));
				set => Marshal.WriteIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 2 * IntPtr.Size), value);
			}
			public static IntPtr DXGI_SWAP_CHAIN_DESC1_pDesc
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 3 * IntPtr.Size));
				set => Marshal.WriteIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 3 * IntPtr.Size), value);
			}
			public static FullscreenDescOptionalPointers DXGI_SWAP_CHAIN_FULLSCREEN_DESC_pFullscreenDesc
			{
				get => Marshal.PtrToStructure<FullscreenDescOptionalPointers>(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 4 * IntPtr.Size));
			}
			public static IntPtr IDXGIOutput_pRestrictToOutput
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 5 * IntPtr.Size));
				set => Marshal.WriteIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 5 * IntPtr.Size), value);
			}
			public static DXGI_SWAP_CHAIN_DESC1 Desc
			{
				get => Marshal.PtrToStructure<DXGI_SWAP_CHAIN_DESC1>(DXGI_SWAP_CHAIN_DESC1_pDesc);
				set => Marshal.StructureToPtr(value, DXGI_SWAP_CHAIN_DESC1_pDesc, false);
			}
			public struct FullscreenDescOptionalPointers
			{
				public IntPtr _pFullscreenDesc;
				public IntPtr _OriginalFullscreenDesc;
				public IntPtr _LocalFullscreenDesc;

				[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006: (Naming rule violation)", Justification = "This is intentional")]
				public IntPtr pFullscreenDesc
				{
					get => Marshal.ReadIntPtr(_pFullscreenDesc);
					set => Marshal.WriteIntPtr(_pFullscreenDesc, value);
				}
				public DXGI_SWAP_CHAIN_FULLSCREEN_DESC? OriginalFullscreenDesc
				{
					get
					{
						if (_OriginalFullscreenDesc != IntPtr.Zero)
						{
							return Marshal.PtrToStructure<DXGI_SWAP_CHAIN_FULLSCREEN_DESC>(_OriginalFullscreenDesc);
						}
						else
						{
							return null;
						}
					}
				}
				public DXGI_SWAP_CHAIN_FULLSCREEN_DESC LocalFullscreenDesc
				{
					get => Marshal.PtrToStructure<DXGI_SWAP_CHAIN_FULLSCREEN_DESC>(_LocalFullscreenDesc);
					set => Marshal.StructureToPtr(value, _LocalFullscreenDesc, false);
				}
			}
		}

		public static class SwapChainForHwndCreatedEventArgs
		{
			public static IntPtr IDXGIFactory2_This
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size));
			}
			public static IntPtr IUnknown_pDevice
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 1 * IntPtr.Size));
			}
			public static IntPtr HWND_hWnd
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 2 * IntPtr.Size));
			}
			public static IntPtr DXGI_SWAP_CHAIN_DESC1_pDesc
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 3 * IntPtr.Size));
			}
			public static IntPtr DXGI_SWAP_CHAIN_FULLSCREEN_DESC_pFullscreenDesc
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 4 * IntPtr.Size));
			}
			public static IntPtr IDXGIOutput_pRestrictToOutput
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 5 * IntPtr.Size));
			}
			public static IntPtr IDXGISwapChain1_ppSwapChain
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 6 * IntPtr.Size));
			}
			public static IntPtr IDXGISwapChain1_ppSwapChain_Deref
			{
				get => Marshal.ReadIntPtr(IDXGISwapChain1_ppSwapChain);
				set => Marshal.WriteIntPtr(IDXGISwapChain1_ppSwapChain, value);
			}
		}

		public static class SwapChainPresentingEventArgs
		{
			public static IntPtr IDXGISwapChain_This
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size));
				set => Marshal.WriteIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size), value);
			}
			public static uint SyncInterval
			{
				get => (uint)Marshal.ReadInt32(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 1 * IntPtr.Size));
				set => Marshal.WriteInt32(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 1 * IntPtr.Size), (int)value);
			}
			public static uint Flags
			{
				get => (uint)Marshal.ReadInt32(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 2 * IntPtr.Size));
				set => Marshal.WriteInt32(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 2 * IntPtr.Size), (int)value);
			}
		}

		public static class SwapChainPresentedEventArgs
		{
			public static IntPtr IDXGISwapChain_This
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size));
			}
			public static uint SyncInterval
			{
				get => (uint)Marshal.ReadInt32(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 1 * IntPtr.Size));
			}
			public static uint Flags
			{
				get => (uint)Marshal.ReadInt32(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 2 * IntPtr.Size));
			}
		}

		public static class SwapChain1PresentingEventArgs
		{
			public static IntPtr IDXGISwapChain_This
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size));
				set => Marshal.WriteIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size), value);
			}
			public static uint SyncInterval
			{
				get => (uint)Marshal.ReadInt32(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 1 * IntPtr.Size));
				set => Marshal.WriteInt32(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 1 * IntPtr.Size), (int)value);
			}
			public static uint Flags
			{
				get => (uint)Marshal.ReadInt32(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 2 * IntPtr.Size));
				set => Marshal.WriteInt32(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 2 * IntPtr.Size), (int)value);
			}
			public static IntPtr DXGI_PRESENT_PARAMETERS_pPresentParameters
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 3 * IntPtr.Size));
			}
			public static DXGI_PRESENT_PARAMETERS? PresentParameters
			{
				get
				{
					if (DXGI_PRESENT_PARAMETERS_pPresentParameters != IntPtr.Zero)
					{
						return Marshal.PtrToStructure<DXGI_PRESENT_PARAMETERS>(DXGI_PRESENT_PARAMETERS_pPresentParameters);
					}
					else
					{
						return null;
					}
				}
				set
				{
					if (value == null)
					{
						Marshal.WriteIntPtr(DXGI_PRESENT_PARAMETERS_pPresentParameters, IntPtr.Zero);
					}
					else throw new NotImplementedException(); // not yet
				}
			}
		}

		public static class SwapChain1PresentedEventArgs
		{
			public static IntPtr IDXGISwapChain_This
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 0 * IntPtr.Size));
			}
			public static uint SyncInterval
			{
				get => (uint)Marshal.ReadInt32(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 1 * IntPtr.Size));
			}
			public static uint Flags
			{
				get => (uint)Marshal.ReadInt32(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 2 * IntPtr.Size));
			}
			public static IntPtr DXGI_PRESENT_PARAMETERS_pPresentParameters
			{
				get => Marshal.ReadIntPtr(Marshal.ReadIntPtr(Get_LocalVariablesArray(), 3 * IntPtr.Size));
			}
			public static DXGI_PRESENT_PARAMETERS? PresentParameters
			{
				get => Marshal.PtrToStructure<DXGI_PRESENT_PARAMETERS>(DXGI_PRESENT_PARAMETERS_pPresentParameters);
			}
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

		public enum DXGI_MODE_SCANLINE_ORDER : uint
		{
			DXGI_MODE_SCANLINE_ORDER_UNSPECIFIED = 0,
			DXGI_MODE_SCANLINE_ORDER_PROGRESSIVE = 1,
			DXGI_MODE_SCANLINE_ORDER_UPPER_FIELD_FIRST = 2,
			DXGI_MODE_SCANLINE_ORDER_LOWER_FIELD_FIRST = 3
		}

		public enum DXGI_MODE_SCALING : uint
		{
			DXGI_MODE_SCALING_UNSPECIFIED = 0,
			DXGI_MODE_SCALING_CENTERED = 1,
			DXGI_MODE_SCALING_STRETCHED = 2
		}

		[Flags]
		public enum DXGI_USAGE : uint
		{
			DXGI_USAGE_SHADER_INPUT = 0x00000010,
			DXGI_USAGE_RENDER_TARGET_OUTPUT = 0x00000020,
			DXGI_USAGE_BACK_BUFFER = 0x00000040,
			DXGI_USAGE_SHARED = 0x00000080,
			DXGI_USAGE_READ_ONLY = 0x00000100,
			DXGI_USAGE_DISCARD_ON_PRESENT = 0x00000200,
			DXGI_USAGE_UNORDERED_ACCESS = 0x00000400
		}

		public enum DXGI_SWAP_EFFECT : uint
		{
			DXGI_SWAP_EFFECT_DISCARD = 0,
			DXGI_SWAP_EFFECT_SEQUENTIAL = 1,
			DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL = 3,
			DXGI_SWAP_EFFECT_FLIP_DISCARD = 4
		}

		[Flags]
		public enum DXGI_SWAP_CHAIN_FLAG : uint
		{
			DXGI_SWAP_CHAIN_FLAG_NONPREROTATED = 0x1,
			DXGI_SWAP_CHAIN_FLAG_ALLOW_MODE_SWITCH = 0x2,
			DXGI_SWAP_CHAIN_FLAG_GDI_COMPATIBLE = 0x4,
			DXGI_SWAP_CHAIN_FLAG_RESTRICTED_CONTENT = 0x8,
			DXGI_SWAP_CHAIN_FLAG_RESTRICT_SHARED_RESOURCE_DRIVER = 0x10,
			DXGI_SWAP_CHAIN_FLAG_DISPLAY_ONLY = 0x20,
			DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT = 0x40,
			DXGI_SWAP_CHAIN_FLAG_FOREGROUND_LAYER = 0x80,
			DXGI_SWAP_CHAIN_FLAG_FULLSCREEN_VIDEO = 0x100,
			DXGI_SWAP_CHAIN_FLAG_YUV_VIDEO = 0x200,
			DXGI_SWAP_CHAIN_FLAG_HW_PROTECTED = 0x400,
			DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING = 0x800,
			DXGI_SWAP_CHAIN_FLAG_RESTRICTED_TO_ALL_HOLOGRAPHIC_DISPLAYS = 0x1000
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DXGI_RATIONAL
		{
			public uint Numerator;
			public uint Denominator;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DXGI_MODE_DESC
		{
			public uint Width;
			public uint Height;
			public DXGI_RATIONAL RefreshRate;
			public DXGI_FORMAT Format;
			public DXGI_MODE_SCANLINE_ORDER ScanlineOrdering;
			public DXGI_MODE_SCALING Scaling;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DXGI_SAMPLE_DESC
		{
			public uint Count;
			public uint Quality;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DXGI_SWAP_CHAIN_DESC
		{
			public DXGI_MODE_DESC BufferDesc;
			public DXGI_SAMPLE_DESC SampleDesc;
			public DXGI_USAGE BufferUsage;
			public uint BufferCount;
			public IntPtr OutputWindow;
			[MarshalAs(UnmanagedType.Bool)]
			public bool Windowed;
			public DXGI_SWAP_EFFECT SwapEffect;
			public uint Flags;
		}

		public enum DXGI_SCALING : int
		{
			DXGI_SCALING_STRETCH = 0,
			DXGI_SCALING_NONE = 1,
			DXGI_SCALING_ASPECT_RATIO_STRETCH = 2
		}

		public enum DXGI_ALPHA_MODE : int
		{
			DXGI_ALPHA_MODE_UNSPECIFIED = 0,
			DXGI_ALPHA_MODE_PREMULTIPLIED = 1,
			DXGI_ALPHA_MODE_STRAIGHT = 2,
			DXGI_ALPHA_MODE_IGNORE = 3,
			DXGI_ALPHA_MODE_FORCE_DWORD = -1
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DXGI_SWAP_CHAIN_DESC1
		{
			public uint Width;
			public uint Height;
			public DXGI_FORMAT Format;
			[MarshalAs(UnmanagedType.Bool)]
			public bool Stereo;
			public DXGI_SAMPLE_DESC SampleDesc;
			public DXGI_USAGE BufferUsage;
			public uint BufferCount;
			public DXGI_SCALING Scaling;
			public DXGI_SWAP_EFFECT SwapEffect;
			public DXGI_ALPHA_MODE AlphaMode;
			public uint Flags;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DXGI_SWAP_CHAIN_FULLSCREEN_DESC
		{
			public DXGI_RATIONAL RefreshRate;
			public DXGI_MODE_SCANLINE_ORDER ScanlineOrdering;
			public DXGI_MODE_SCALING Scaling;
			[MarshalAs(UnmanagedType.Bool)]
			public bool Windowed;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int Left, Top, Right, Bottom;
			public int Width
			{
				get => Right - Left;
				set => Right = Left + value;
			}
			public int Height
			{
				get => Bottom - Top;
				set => Bottom = Top + value;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X, Y;
		}

		public readonly struct NativeArrayAccess<T>
			where T : struct
		{
			public NativeArrayAccess(IntPtr Address)
			{
				this.Address = Address;
			}
			private readonly IntPtr Address;
			public T this[int index]
			{
				get => Marshal.PtrToStructure<T>(IntPtr.Add(Address, index * Marshal.SizeOf<T>()));
				set => Marshal.StructureToPtr(value, IntPtr.Add(Address, index * Marshal.SizeOf<T>()), false);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DXGI_PRESENT_PARAMETERS
		{
			public uint DirtyRectsCount;
			public IntPtr pDirtyRects;
			public IntPtr pScrollRect;
			public IntPtr pScrollOffset;
			public NativeArrayAccess<RECT>? DirtyRects
			{
				get
				{
					if (pDirtyRects != IntPtr.Zero)
					{
						return new NativeArrayAccess<RECT>(pDirtyRects);
					}
					else
					{
						return null;
					}
				}
			}
			public RECT? ScrollRect
			{
				get
				{
					if (pScrollRect != IntPtr.Zero)
					{
						return Marshal.PtrToStructure<RECT>(pScrollRect);
					}
					else
					{
						return null;
					}
				}
				set
				{
					if (value == null) throw new ArgumentNullException("value");
					if (pScrollRect != IntPtr.Zero) throw new NullReferenceException();
					Marshal.StructureToPtr(value.Value, pScrollRect, false);
				}
			}
			public POINT? ScrollOffset
			{
				get
				{
					if (pScrollRect != IntPtr.Zero)
					{
						return Marshal.PtrToStructure<POINT>(pScrollRect);
					}
					else
					{
						return null;
					}
				}
				set
				{
					if (value == null) throw new ArgumentNullException("value");
					if (pScrollRect != IntPtr.Zero) throw new NullReferenceException();
					Marshal.StructureToPtr(value.Value, pScrollRect, false);
				}
			}
		}

		public const byte OpCode_jmp = 0xE9;
		public const uint Discord = 0xD15C03D;
		public class ThirdPartyNonDetourHookDetectedException : Exception
		{
			public ThirdPartyNonDetourHookDetectedException() : base($"A third-party non-detour DirectX hook detected!")
			{
				Log.MessageBox(IntPtr.Zero,
					$"{typeof(DirectXHook).FullName}: A third-party non-detour DirectX hook detected!" + "\r\n" +
					"Keep in mind that if this other hook somehow causes stack overflow exception I won't be able to fix it!",
					"I curse the name, the one behind it all", Log.MB_ICONWARNING | Log.MB_OK);
			}
		}
	}
}
