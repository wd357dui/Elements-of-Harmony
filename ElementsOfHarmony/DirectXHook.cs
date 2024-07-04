using ElementsOfHarmony.NativeInterface;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static ElementsOfHarmony.NativeInterface.D2D1;
using static ElementsOfHarmony.NativeInterface.D3D11;
using static ElementsOfHarmony.NativeInterface.DXGI;
using static ElementsOfHarmony.Settings.DirectXHook;
using static ElementsOfHarmony.Settings.DirectXHook.Magic;

namespace ElementsOfHarmony
{
	public static class DirectXHook
	{
		public static SortedDictionary<string, string> VertexShaderFiles = new SortedDictionary<string, string>();
		public static SortedDictionary<string, string> PixelShaderFiles = new SortedDictionary<string, string>();

		public static event EventHandler<CreateSwapChainEventArgs>? PreCreateSwapChain, PostCreateSwapChain;
		public static event EventHandler<CreateSwapChainForHwndEventArgs>? PreCreateSwapChainForHwnd, PostCreateSwapChainForHwnd;
		public static event EventHandler<PresentEventArgs>? PrePresent, PostPresent;
		public static event EventHandler<ResizeBuffersEventArgs>? PreResizeBuffers, PostResizeBuffers;
		public static event EventHandler<ResizeTargetEventArgs>? PreResizeTarget, PostResizeTarget;
		public static event EventHandler<Present1EventArgs>? PrePresent1, PostPresent1;
		public static event EventHandler<CreateShaderResourceViewEventArgs>? PreCreateShaderResourceView, PostCreateShaderResourceView;
		public static event EventHandler<CreateRenderTargetViewEventArgs>? PreCreateRenderTargetView, PostCreateRenderTargetView;
		public static event EventHandler<CreatePixelShaderEventArgs>? PreCreatePixelShader, PostCreatePixelShader;
		public static event EventHandler<PSSetShaderEventArgs>? PrePSSetShader, PostPSSetShader;
		public static event EventHandler<DrawIndexedEventArgs>? PreDrawIndexed, PostDrawIndexed;
		public static event EventHandler<DrawEventArgs>? PreDraw, PostDraw;
		public static event EventHandler<PSSetConstantBuffersEventArgs>? PrePSSetConstantBuffers, PostPSSetConstantBuffers;
		public static event EventHandler<DrawIndexedInstancedEventArgs>? PreDrawIndexedInstanced, PostDrawIndexedInstanced;
		public static event EventHandler<DrawInstancedEventArgs>? PreDrawInstanced, PostDrawInstanced;
		public static event EventHandler<OMSetRenderTargetsEventArgs>? PreOMSetRenderTargets, PostOMSetRenderTargets;
		public static event EventHandler<OMSetRenderTargetsAndUnorderedAccessViewsEventArgs>? PreOMSetRenderTargetsAndUnorderedAccessViews, PostOMSetRenderTargetsAndUnorderedAccessViews;
		public static event EventHandler<DrawAutoEventArgs>? PreDrawAuto, PostDrawAuto;
		public static event EventHandler<DrawIndexedInstancedIndirectEventArgs>? PreDrawIndexedInstancedIndirect, PostDrawIndexedInstancedIndirect;
		public static event EventHandler<DrawInstancedIndirectEventArgs>? PreDrawInstancedIndirect, PostDrawInstancedIndirect;
		public static event EventHandler<SetColorSpace1EventArgs>? PreSetColorSpace1, PostSetColorSpace1;
		public static event EventHandler<SetHDRMetaDataEventArgs>? PreSetHDRMetaData, PostSetHDRMetaData;
		public static event EventHandler<HookCallbackEventArgs>? PreAnyEvent, PostAnyEvent;

		public static event EventHandler? OverlayDraw;

		public static void Init()
		{
			try
			{
				if (Directory.Exists("Elements of Harmony/Assets/Shader/VertexShader"))
				{
					foreach (string File in Directory.EnumerateFiles("Elements of Harmony/Assets/Shader/VertexShader"))
					{
						VertexShaderFiles.Add(Path.GetFileNameWithoutExtension(File), File);
						Log.Message($"vertex shader file {File} found");
					}
				}
				if (Directory.Exists("Elements of Harmony/Assets/Shader/PixelShader"))
				{
					foreach (string File in Directory.EnumerateFiles("Elements of Harmony/Assets/Shader/PixelShader"))
					{
						PixelShaderFiles.Add(Path.GetFileNameWithoutExtension(File), File);
						Log.Message($"pixel shader file \"{File}\" found");
					}
				}

				Application.quitting += Application_quitting;

				SetLogCallback(Log.Message);

				SetHookCallback1(HookCallback);

				SetCallbacks(
					VertexShader: (Name) => {
						if (VertexShaderFiles.TryGetValue(Name, out string File))
						{
							return File;
						}
						return null;
					},
					PixelShader: (Name) => {
						if (PixelShaderFiles.TryGetValue(Name, out string File))
						{
							return File;
						}
						return null;
					});

				// overlay hooks
				PrePresent += OnPrePresent;
				PrePresent1 += OnPrePresent1;

				// HDR format hooks
				PreCreateSwapChain += DirectXHook_PreCreateSwapChain_Format_Hook;
				PreCreateSwapChainForHwnd += DirectXHook_PreCreateSwapChainForHwnd_Format_Hook;
				PreResizeBuffers += DirectXHook_PreResizeBuffers_Format_Hook;
				PreResizeTarget += DirectXHook_PreResizeTarget_Format_Hook;
				PreCreateShaderResourceView += DirectXHook_PreCreateShaderResourceView_Format_Fix;
				PreCreateRenderTargetView += DirectXHook_PreCreateRenderTargetView_Format_Fix;

				PreSetHDRMetaData += DirectXHook_PreSetHDRMetaData_Hook;

				/* HDR shader hooks moved to ElementsOfHarmony.Native in favor of performance
				PostPresent += DirectXHook_PostPresent_Hook;
				PostPresent1 += DirectXHook_PostPresent1_Hook;
				PostCreateRenderTargetView += DirectXHook_PostCreateRenderTargetView_Hook;
				PrePSSetShader += DirectXHook_PrePSSetShader_Hook;
				PreDraw += DirectXHook_PreDraw_Hook;
				PreDrawAuto += DirectXHook_PreDrawAuto_Hook;
				PreDrawIndexed += DirectXHook_PreDrawIndexed_Hook;
				PreDrawIndexedInstanced += DirectXHook_PreDrawIndexedInstanced_Hook;
				PreDrawIndexedInstancedIndirect += DirectXHook_PreDrawIndexedInstancedIndirect_Hook;
				PreDrawInstanced += DirectXHook_PreDrawInstanced_Hook;
				PreDrawInstancedIndirect += DirectXHook_PreDrawInstancedIndirect_Hook;
				*/

				PreAnyEvent += ApplyRenderSettings;

				SetRunning(true);

				// apply all of our patch procedures using Harmony API
				Harmony element = new Harmony($"{typeof(DirectXHook).FullName}");
				int Num = 0;
				if (ElementsOfHarmony.IsAMBA)
				{
					Assembly ElementsOfHarmony_AMBA =
						AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(A => A.GetName().Name == "ElementsOfHarmony.AMBA") ??
						Assembly.LoadFile(Path.Combine(ElementsOfHarmony.AssemblyDirectory, "ElementsOfHarmony.AMBA.dll"));
					if (ElementsOfHarmony_AMBA.GetType("ElementsOfHarmony.AMBA.DirectXHook") is Type DirectXHook_AMBA)
					{
						if (DirectXHook_AMBA.GetMethod("Init") is MethodInfo InitMethod)
						{
							InitMethod.Invoke(null, Array.Empty<object>());
						}
						Num = 0;
						foreach (var Patch in DirectXHook_AMBA.GetNestedTypes())
						{
							new PatchClassProcessor(element, Patch).Patch();
							Num++;
						}
						if (Num > 0)
						{
							Log.Message($"Harmony patch for {DirectXHook_AMBA.FullName} successful - {Num} Patches");
						}
					}
				}
				if (ElementsOfHarmony.IsAZHM)
				{
					Assembly ElementsOfHarmony_AZHM =
						AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(A => A.GetName().Name == "ElementsOfHarmony.AZHM") ??
						Assembly.LoadFile(Path.Combine(ElementsOfHarmony.AssemblyDirectory, "ElementsOfHarmony.AZHM.dll"));
					if (ElementsOfHarmony_AZHM.GetType("ElementsOfHarmony.AZHM.DirectXHook") is Type DirectXHook_AZHM)
					{
						if (DirectXHook_AZHM.GetMethod("Init") is MethodInfo InitMethod)
						{
							InitMethod.Invoke(null, Array.Empty<object>());
						}
						Num = 0;
						foreach (var Patch in DirectXHook_AZHM.GetNestedTypes())
						{
							new PatchClassProcessor(element, Patch).Patch();
							Num++;
						}
						if (Num > 0)
						{
							Log.Message($"Harmony patch for {DirectXHook_AZHM.FullName} successful - {Num} Patches");
						}
					}
				}

				Num = 0;
				foreach (var Patch in typeof(RenderHooks).GetNestedTypes())
				{
					new PatchClassProcessor(element, Patch).Patch();
					Num++;
				}
				if (Num > 0)
				{
					Log.Message($"Harmony patch for {typeof(RenderHooks).FullName} successful - {Num} Patches");
				}

				int HResult = InstallHook();
				Log.Message($"InstallHook() returns {HResult:X}");
				Marshal.ThrowExceptionForHR(HResult);

				HResult = InitOverlay();
				Log.Message($"InitOverlay() returns {HResult:X}");
				Marshal.ThrowExceptionForHR(HResult);
				
				HResult = InitNativeCallbacks(HDR.TakeOverOutputFormat, HDR.DynamicRangeFactor);
				Log.Message($"InitNativeCallbacks() returns {HResult:X}");
				Marshal.ThrowExceptionForHR(HResult);

				IntPtr DXGI_DLL = Get_DXGI_DLL_BaseAddress();
				IntPtr GameOverlayRenderer64_DLL = Get_GameOverlayRenderer64_DLL_BaseAddress();
				uint GameOverlayRenderer64_DLL_ImageSize = Get_GameOverlayRenderer64_DLL_ImageSize();

				if (DXGI_DLL != IntPtr.Zero && GameOverlayRenderer64_DLL != IntPtr.Zero)
				{
					IntPtr PresentProc = Get_Present_MemoryOriginal_Proc();
					IntPtr Present1Proc = Get_Present1_MemoryOriginal_Proc();
					var PresentBytes = new NativeArrayAccess<byte>(Get_Present_MemoryOriginal_Bytes(), 5);
					var Present1Bytes = new NativeArrayAccess<byte>(Get_Present1_MemoryOriginal_Bytes(), 5);

					if (PresentProc != IntPtr.Zero && Get_Present_DetourHookDetected())
					{
						Log.Message($"detour hook detected on IDXGISwapChain::Present - " +
							$"{PresentBytes[0]:X2} {PresentBytes[1]:X2} {PresentBytes[2]:X2} {PresentBytes[3]:X2} {PresentBytes[4]:X2}");

						if (JmpEndsUpInRange(PresentProc, GameOverlayRenderer64_DLL, GameOverlayRenderer64_DLL_ImageSize))
						{
							Log.Message($"jmp ends up in GameOverlayRenderer64.dll, which means this is a steam overlay hook");
						}
						else
						{
							Log.Message($"jmp did not end up in GameOverlayRenderer64.dll, this is (probably) not a steam overlay hook");
						}
					}
					if (Present1Proc != IntPtr.Zero && Get_Present1_DetourHookDetected())
					{
						Log.Message($"detour hook detected on IDXGISwapChain1::Present1 - " +
							$"{Present1Bytes[0]:X2} {Present1Bytes[1]:X2} {Present1Bytes[2]:X2} {Present1Bytes[3]:X2} {Present1Bytes[4]:X2}");

						if (JmpEndsUpInRange(Present1Proc, GameOverlayRenderer64_DLL, GameOverlayRenderer64_DLL_ImageSize))
						{
							Log.Message($"jmp ends up in GameOverlayRenderer64.dll, which means this is a steam overlay hook");
						}
						else
						{
							Log.Message($"jmp did not end up in GameOverlayRenderer64.dll, this is (probably) not a steam overlay hook");
						}
					}
				}

				Log.Message("DirectX hook complete");
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

		public static class RenderHooks
		{
			[HarmonyPatch(typeof(Volume), methodName: "profileRef", methodType: MethodType.Getter)]
			public static class VolumnProfile_profileRef_getter
			{
				public static void Postfix(Volume __instance, VolumeProfile __result)
				{
					if (__instance.IsGlobal())
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

			[HarmonyPatch(typeof(UniversalRenderPipeline), methodName: "UpdateVolumeFramework")]
			public static class UpdateVolumeFrameworkPatch
			{
				public static GameObject? NewGlobalVolumeGameObject;
				public static VolumeProfile? NewGlobalVolumeProfile;
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
						NewGlobalVolume.IsGlobal(true);
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

						static void OnActiveSceneChanged(Scene Old, Scene New)
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

		private static readonly EventHandler<HookCallbackEventArgs> ApplyRenderSettings = (object sender, HookCallbackEventArgs e) =>
		{
			if (VSyncInterval != null) QualitySettings.vSyncCount = VSyncInterval.Value;
			if (TargetFrameRate != null) Application.targetFrameRate = TargetFrameRate.Value;

			// Obsolete in AZHM's Unity version but is still usable
			Screen.SetResolution(Width ?? Screen.width, Height ?? Screen.height,
				Settings.DirectXHook.FullScreenMode ?? Screen.fullScreenMode,
				RefreshRate ?? 0);

			foreach (Camera camera in Camera.allCameras)
			{
				if (MSAA != null && MSAA > 0)
				{
					camera.allowMSAA = true;
					QualitySettings.antiAliasing = MSAA.Value;
				}
				if (AllowHDR is bool Allow)
				{
					camera.allowHDR = Allow;
				}
				if (AllowDynamicResolution is bool DR)
				{
					camera.allowDynamicResolution = DR;
				}
			}

			PreAnyEvent -= ApplyRenderSettings;
		};

		private static void Application_quitting()
		{
			SetRunning(false);
			Marshal.GetExceptionForHR(UninstallHook());
			ReleaseOverlay();
		}

		#region Overlay and HDR color space hook

		private static volatile bool _IsHDR = false;
		private static volatile bool _WithinOverlayPass = false;
		public static bool IsHDR
		{ 
			get => _IsHDR;
			set
			{
				SetIsHDR(_IsHDR = value);
			}
		}
		public static bool WithinOverlayPass
		{
			get =>_WithinOverlayPass;
			set
			{
				SetWithinOverlayPass(_WithinOverlayPass = value);
			}
		}
		private static void SetColorSpace(IntPtr pSwapChain, DXGI_FORMAT Format)
		{
			using Unknown SwapChain = new Unknown(pSwapChain);
			using Unknown? SwapChain3 = SwapChain.As<Unknown>(IID_IDXGISwapChain3);
			SwapChain3?.Invoke<IDXGISwapChain3_SetColorSpace1_Proc>(IDXGISwapChain3_SetColorSpace1_VTableIndex,
				IsHDR ? DXGI_COLOR_SPACE_TYPE.DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709 :
				Format == DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM ? DXGI_COLOR_SPACE_TYPE.DXGI_COLOR_SPACE_RGB_FULL_G2084_NONE_P2020 :
				DXGI_COLOR_SPACE_TYPE.DXGI_COLOR_SPACE_RGB_FULL_G22_NONE_P709);
		}
		private static void OnPrePresent(object sender, PresentEventArgs _)
		{
			IntPtr SwapChain = (IntPtr)sender;
			Marshal.ThrowExceptionForHR(DetermineOutputHDR(SwapChain, out DXGI_FORMAT Format, out bool HDR));
			IsHDR = HDR;
			SetColorSpace(SwapChain, Format);
			if (Overlay.Enabled)
			{
				InvokeOverlay(SwapChain);
			}
		}
		private static void OnPrePresent1(object sender, Present1EventArgs _)
		{
			IntPtr SwapChain = (IntPtr)sender;
			Marshal.ThrowExceptionForHR(DetermineOutputHDR(SwapChain, out DXGI_FORMAT Format, out bool HDR));
			IsHDR = HDR;
			SetColorSpace(SwapChain, Format);
			if (Overlay.Enabled)
			{
				InvokeOverlay(SwapChain);
			}
		}
		private static void InvokeOverlay(IntPtr pSwapChain)
		{
			int HResult;
			IntPtr DeviceInstance;
			unsafe
			{
				HResult = SwapChainBeginDraw(pSwapChain, 0, &DeviceInstance);
			}
			if (HResult == unchecked((int)0x800010BF)) // 10 bit HDR format is not compatible with Direct2D
			{
				if (Overlay.SolveCompatibilityWithHDR)
				{
					Screen.SetResolution(Width ?? Screen.width, Height ?? Screen.height, Settings.DirectXHook.FullScreenMode ?? Screen.fullScreenMode);
					Log.Message($"10 bit HDR format is not compatible with Direct2D, attempting to change to 16 bit format");
				}

				return;
			}
			Marshal.ThrowExceptionForHR(HResult);
			WithinOverlayPass = true;
			OverlayDraw?.Invoke(DeviceInstance, EventArgs.Empty);
			if (Overlay.ShowFrameTime)
			{
				PrintFrameTime(DeviceInstance, IsHDR);
			}
			Marshal.ThrowExceptionForHR(SwapChainEndDraw(pSwapChain, 0, DeviceInstance));
			WithinOverlayPass = false;
		}

		#endregion

		#region HDR buffer format hooks

		private static void ChangeFormat(ref DXGI_FORMAT Format)
		{
			if (Format == DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM && (Overlay.SolveCompatibilityWithHDR || HDR.TakeOverOutputFormat))
			{
				Format = DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT;
				Log.Message($"swap chain is resizing, changing HDR format DXGI_FORMAT_R10G10B10A2_UNORM to DXGI_FORMAT_R16G16B16A16_FLOAT");
			}
			else if (HDR.Forced && Format != DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT)
			{
				Format = DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT;
				Log.Message($"swap chain is resizing, forcing HDR format DXGI_FORMAT_R16G16B16A16_FLOAT");
			}
			if (ForceSwapChainFormat is DXGI_FORMAT ForceFormat)
			{
				Format = ForceFormat;
				Log.Message($"applying forced swap chain format {Format}");
			}
		}
		private static void DirectXHook_PreCreateSwapChain_Format_Hook(object sender, CreateSwapChainEventArgs e)
		{
			var Desc = e.Desc;
			ChangeFormat(ref Desc.BufferDesc.Format);
			e.Desc = Desc;
		}
		private static void DirectXHook_PreCreateSwapChainForHwnd_Format_Hook(object sender, CreateSwapChainForHwndEventArgs e)
		{
			var Desc = e.Desc;
			ChangeFormat(ref Desc.Format);
			e.Desc = Desc;
		}
		private static void DirectXHook_PreResizeBuffers_Format_Hook(object sender, ResizeBuffersEventArgs e)
		{
			Marshal.ThrowExceptionForHR(ReleaseSwapChainResources((IntPtr)sender));
			DXGI_FORMAT Format = e.NewFormat;
			ChangeFormat(ref Format);
			e.NewFormat = Format;
		}
		private static void DirectXHook_PreResizeTarget_Format_Hook(object sender, ResizeTargetEventArgs e)
		{
			Marshal.ThrowExceptionForHR(ReleaseSwapChainResources((IntPtr)sender));
			var Desc = e.NewTargetParameters;
			ChangeFormat(ref Desc.Format);
			e.NewTargetParameters = Desc;
		}

		private static void DirectXHook_PreCreateShaderResourceView_Format_Fix(object sender, CreateShaderResourceViewEventArgs e)
		{
			if (!e.Desc.HasValue) return;

			using Unknown Resource = new Unknown(e.Resource);
			using Unknown? Texture2D = Resource.As<Unknown>(IID_ID3D11Texture2D);
			D3D11_SHADER_RESOURCE_VIEW_DESC SRVDesc = e.Desc.Value;
			if (Texture2D != null)
			{
				D3D11_TEXTURE2D_DESC TextureDesc;
				unsafe
				{
					D3D11_TEXTURE2D_DESC* pDesc = &TextureDesc;
					Texture2D.Invoke<ID3D11Texture2D_GetDesc_Proc>(ID3D11Texture2D_GetDesc_VTableIndex, (IntPtr)pDesc);
				}
				if ((SRVDesc.Format != TextureDesc.Format) && !SRVDesc.Format.CanCastFrom(TextureDesc.Format))
				{
					Log.Message($"fixing ID3D11Device::CreateShaderResourceView DXGI format argument, " +
						$"was {SRVDesc.Format}, should be {TextureDesc.Format}");
					SRVDesc.Format = TextureDesc.Format;
					e.Desc = SRVDesc;
				}
			}
		}
		private static void DirectXHook_PreCreateRenderTargetView_Format_Fix(object sender, CreateRenderTargetViewEventArgs e)
		{
			if (!e.Desc.HasValue) return;

			using Unknown Resource = new Unknown(e.Resource);
			using Unknown? Texture2D = Resource.As<Unknown>(IID_ID3D11Texture2D);
			D3D11_RENDER_TARGET_VIEW_DESC RTVDesc = e.Desc.Value;
			if (Texture2D != null)
			{
				D3D11_TEXTURE2D_DESC TextureDesc;
				unsafe
				{
					D3D11_TEXTURE2D_DESC* pDesc = &TextureDesc;
					Texture2D.Invoke<ID3D11Texture2D_GetDesc_Proc>(ID3D11Texture2D_GetDesc_VTableIndex, (IntPtr)pDesc);
				}
				if ((RTVDesc.Format != TextureDesc.Format) && !RTVDesc.Format.CanCastFrom(TextureDesc.Format))
				{
					Log.Message($"fixing ID3D11Device::CreateRenderTargetView DXGI format argument, " +
						$"was {RTVDesc.Format}, should be {TextureDesc.Format}");
					RTVDesc.Format = TextureDesc.Format;
					e.Desc = RTVDesc;
				}
			}
		}

		#endregion

		private static void DirectXHook_PreSetHDRMetaData_Hook(object sender, SetHDRMetaDataEventArgs e)
		{
			if (HDR.TakeOverOutputFormat)
			{
				// this function seems to have no impact on the display,
				// but still better disable it in case it cause problem some day
				e.Args.Stop = true;
			}
		}

		/* HDR shader hooks moved to ElementsOfHarmony.Native in favor of performance

		// create the pixel shader if not created already
		private static bool EnsurePixelShader(IntPtr Device, ref Unknown? Instance, string Name)
		{
			if (Instance == null)
			{
				if (PixelShaderFiles.TryGetValue(Name, out string ShaderFile))
				{
					IntPtr pPixelShader;
					unsafe
					{
						Marshal.ThrowExceptionForHR(CompilePixelShader(Device, ShaderFile, "main", Name, &pPixelShader));
					}
					Instance = new Unknown(pPixelShader, false);
					Log.Message($"shader \"{Name}\" initailized");
				}
				else
				{
					Log.Message($"shader \"{Name}\" not found!");
					return false;
				}
			}
			return true;
		}

		// keep track of swap chain back buffers and the render target views created for them
		private static readonly HashSet<IntPtr> SwapChainBackBufferTextures = new HashSet<IntPtr>();
		private static void SwapChainGetBackBufferTexture(IntPtr pSwapChain)
		{
			using Unknown SwapChain = new Unknown(pSwapChain);
			IntPtr pResource;
			unsafe
			{
				IntPtr* ppResource = &pResource;
				Marshal.ThrowExceptionForHR((int)SwapChain.Invoke<IDXGISwapChain_GetBuffer_Proc>(IDXGISwapChain_GetBuffer_VTableIndex, 0u, IID_ID3D11Texture2D, (IntPtr)ppResource));
			}
			using Unknown BackBufferTexture = new Unknown(pResource, false);
			SwapChainBackBufferTextures.Add(BackBufferTexture.Instance);
		}
		private static void DirectXHook_PostPresent1_Hook(object sender, Present1EventArgs e)
		{
			SwapChainGetBackBufferTexture((IntPtr)sender);
		}
		private static void DirectXHook_PostPresent_Hook(object sender, PresentEventArgs e)
		{
			SwapChainGetBackBufferTexture((IntPtr)sender);
		}
		private static readonly Dictionary<IntPtr, IntPtr> RenderTargetViewTexturesMap = new Dictionary<IntPtr, IntPtr>();
		private static void DirectXHook_PostCreateRenderTargetView_Hook(object sender, CreateRenderTargetViewEventArgs e)
		{
			using Unknown Texture = new Unknown(e.Resource);
			using Unknown? Texture2D = Texture.As<Unknown>(IID_ID3D11Texture2D);
			if (Texture2D != null)
			{
				RenderTargetViewTexturesMap[e.RenderTargetView] = Texture2D.Instance;
			}
		}

		private static Unknown? BlitCopyHDRTonemap_Replacement = null;
		private static void DirectXHook_PrePSSetShader_Hook(object sender, PSSetShaderEventArgs e)
		{
			if (WithinOverlayPass) return; // don't change any state set by Direct2D
			if (!HDR.TakeOverOutputFormat) return;
			if (e.PixelShader.GetName() == "Hidden/BlitCopyHDRTonemap")
			{
				Unknown DeviceContext = new Unknown((IntPtr)sender);
				IntPtr pDevice;
				unsafe
				{
					IntPtr* ppDevice = &pDevice;
					DeviceContext.Invoke<ID3D11DeviceChild_GetDevice_Proc>(ID3D11DeviceChild_GetDevice_VTableIndex, (IntPtr)ppDevice);
				}
				Unknown Device = new Unknown(pDevice, false);
				if (EnsurePixelShader(Device.Instance, ref BlitCopyHDRTonemap_Replacement, nameof(BlitCopyHDRTonemap_Replacement)))
				{
					e.PixelShader = BlitCopyHDRTonemap_Replacement!.Instance;
				}
			}
		}

		private static Unknown? CopyTextureToSwapChainHDR = null;
		private static Unknown? CopyTextureToSwapChainHDR_ConstantBuffer = null;
		private static void OnDrawCall(IntPtr pDeviceContext)
		{
			if (WithinOverlayPass) return; // don't change any state set by Direct2D
			if (!HDR.TakeOverOutputFormat) return;
			if (!IsHDR) return;

			using Unknown DeviceContext = new Unknown(pDeviceContext);
			IntPtr pDevice;
			unsafe
			{
				IntPtr* ppDevice = &pDevice;
				DeviceContext.Invoke<ID3D11DeviceChild_GetDevice_Proc>(ID3D11DeviceChild_GetDevice_VTableIndex, (IntPtr)ppDevice);
			}
			using Unknown Device = new Unknown(pDevice, false);

			IntPtr pRTV;
			unsafe
			{
				IntPtr* ppRTV = stackalloc IntPtr[1];
				DeviceContext.Invoke<ID3D11DeviceCOntext_OMGetRenderTargets_Proc>(ID3D11DeviceContext_OMGetRenderTargets_VTableIndex, 1u, (IntPtr)ppRTV, IntPtr.Zero);
				pRTV = ppRTV[0];
			}
			using Unknown RTV = new Unknown(pRTV, false);

			if (RTV.Instance != IntPtr.Zero &&
				RenderTargetViewTexturesMap.TryGetValue(RTV.Instance, out IntPtr Texture) &&
				SwapChainBackBufferTextures.Contains(Texture))
			{
				// compile and create (if not created already) and set pixel shader "CopyTextureToSwapChainHDR"
				EnsurePixelShader(Device.Instance, ref CopyTextureToSwapChainHDR, nameof(CopyTextureToSwapChainHDR));
				DeviceContext.Invoke<ID3D11DeviceContext_PSSetShader_Proc>(ID3D11DeviceContext_PSSetShader_VTableIndex, CopyTextureToSwapChainHDR!.Instance, IntPtr.Zero, 0u);

				// create (if not created already) and upload constant buffer
				// to deliver the HDR.DynamicRangeFactor parameter to the shader
				IntPtr ConstantBufferPtr = IntPtr.Zero;
				if (CopyTextureToSwapChainHDR_ConstantBuffer != null)
				{
					ConstantBufferPtr = CopyTextureToSwapChainHDR_ConstantBuffer.Instance;
				}

				unsafe
				{
					var values = stackalloc float[4]
					{
						HDR.DynamicRangeFactor, // multiplicator for color values, default is 1
						0.0f, 0.0f, 0.0f // obligatory padding, values are unused
					};
					IntPtr* ppConstantBuffer = &ConstantBufferPtr;
					EnsureConstantBuffer(Device.Instance, DeviceContext.Instance, ppConstantBuffer, values);
				}

				CopyTextureToSwapChainHDR_ConstantBuffer ??= new Unknown(ConstantBufferPtr, false);

				unsafe
				{
					void** Buffers = stackalloc void*[1]
					{
						CopyTextureToSwapChainHDR_ConstantBuffer.Instance.ToPointer()
					};
					DeviceContext.Invoke<ID3D11DeviceCOntext_PSSetConstantBuffers_Proc>(ID3D11DeviceContext_PSSetConstantBuffers_VTableIndex, 0u, 1u, (IntPtr)Buffers);
				}
			}
		}
		private static void DirectXHook_PreDraw_Hook(object sender, DrawEventArgs e)
		{
			OnDrawCall((IntPtr)sender);
		}
		private static void DirectXHook_PreDrawAuto_Hook(object sender, DrawAutoEventArgs e)
		{
			OnDrawCall((IntPtr)sender);
		}
		private static void DirectXHook_PreDrawIndexed_Hook(object sender, DrawIndexedEventArgs e)
		{
			OnDrawCall((IntPtr)sender);
		}
		private static void DirectXHook_PreDrawIndexedInstanced_Hook(object sender, DrawIndexedInstancedEventArgs e)
		{
			OnDrawCall((IntPtr)sender);
		}
		private static void DirectXHook_PreDrawIndexedInstancedIndirect_Hook(object sender, DrawIndexedInstancedIndirectEventArgs e)
		{
			OnDrawCall((IntPtr)sender);
		}
		private static void DirectXHook_PreDrawInstanced_Hook(object sender, DrawInstancedEventArgs e)
		{
			OnDrawCall((IntPtr)sender);
		}
		private static void DirectXHook_PreDrawInstancedIndirect_Hook(object sender, DrawInstancedIndirectEventArgs e)
		{
			OnDrawCall((IntPtr)sender);
		}
		*/



		private unsafe static void HookCallback(IntPtr Ptr)
		{
			Arguments Args = Marshal.PtrToStructure<Arguments>(Ptr);
			switch (Args.VTableIndex)
			{
				case IDXGIFactory_CreateSwapChain_VTableIndex when Args.IID == IID_IDXGIFactory:
					var CreateSwapChainEventArgs = new CreateSwapChainEventArgs() { Args = Args };
					if (Args.Post) PostCreateSwapChain?.Invoke(Args.PPV, CreateSwapChainEventArgs);
					else PreCreateSwapChain?.Invoke(Args.PPV, CreateSwapChainEventArgs);
					Args = CreateSwapChainEventArgs.Args;
					break;
				case IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex when Args.IID == IID_IDXGIFactory2:
					var CreateSwapChainForHwndEventArgs = new CreateSwapChainForHwndEventArgs() { Args = Args };
					if (Args.Post) PostCreateSwapChainForHwnd?.Invoke(Args.PPV, CreateSwapChainForHwndEventArgs);
					else PreCreateSwapChainForHwnd?.Invoke(Args.PPV, CreateSwapChainForHwndEventArgs);
					Args = CreateSwapChainForHwndEventArgs.Args;
					break;
				case IDXGISwapChain_Present_VTableIndex when Args.IID == IID_IDXGISwapChain:
					var PresentEventArgs = new PresentEventArgs() { Args = Args };
					if (Args.Post) PostPresent?.Invoke(Args.PPV, PresentEventArgs);
					else PrePresent?.Invoke(Args.PPV, PresentEventArgs);
					Args = PresentEventArgs.Args;
					break;
				case IDXGISwapChain_ResizeBuffers_VTableIndex when Args.IID == IID_IDXGISwapChain:
					var ResizeBuffersEventArgs = new ResizeBuffersEventArgs() { Args = Args };
					if (Args.Post) PostResizeBuffers?.Invoke(Args.PPV, ResizeBuffersEventArgs);
					else PreResizeBuffers?.Invoke(Args.PPV, ResizeBuffersEventArgs);
					Args = ResizeBuffersEventArgs.Args;
					break;
				case IDXGISwapChain_ResizeTarget_VTableIndex when Args.IID == IID_IDXGISwapChain:
					var ResizeTargetEventArgs = new ResizeTargetEventArgs() { Args = Args };
					if (Args.Post) PostResizeTarget?.Invoke(Args.PPV, ResizeTargetEventArgs);
					else PreResizeTarget?.Invoke(Args.PPV, ResizeTargetEventArgs);
					Args = ResizeTargetEventArgs.Args;
					break;
				case IDXGISwapChain1_Present1_VTableIndex when Args.IID == IID_IDXGISwapChain1:
					var Present1EventArgs = new Present1EventArgs() { Args = Args };
					if (Args.Post) PostPresent1?.Invoke(Args.PPV, Present1EventArgs);
					else PrePresent1?.Invoke(Args.PPV, Present1EventArgs);
					Args = Present1EventArgs.Args;
					break;
				case ID3D11Device_CreateShaderResourceView_VTableIndex when Args.IID == IID_ID3D11Device:
					var CreateShaderResourceViewEventArgs = new CreateShaderResourceViewEventArgs() { Args = Args };
					if (Args.Post) PostCreateShaderResourceView?.Invoke(Args.PPV, CreateShaderResourceViewEventArgs);
					else PreCreateShaderResourceView?.Invoke(Args.PPV, CreateShaderResourceViewEventArgs);
					Args = CreateShaderResourceViewEventArgs.Args;
					break;
				case ID3D11Device_CreateRenderTargetView_VTableIndex when Args.IID == IID_ID3D11Device:
					var CreateRenderTargetViewEventArgs = new CreateRenderTargetViewEventArgs() { Args = Args };
					if (Args.Post) PostCreateRenderTargetView?.Invoke(Args.PPV, CreateRenderTargetViewEventArgs);
					else PreCreateRenderTargetView?.Invoke(Args.PPV, CreateRenderTargetViewEventArgs);
					Args = CreateRenderTargetViewEventArgs.Args;
					break;
				case ID3D11Device_CreatePixelShader_VTableIndex when Args.IID == IID_ID3D11Device:
					var CreatePixelShaderEventArgs = new CreatePixelShaderEventArgs() { Args = Args };
					if (Args.Post) PostCreatePixelShader?.Invoke(Args.PPV, CreatePixelShaderEventArgs);
					else PreCreatePixelShader?.Invoke(Args.PPV, CreatePixelShaderEventArgs);
					Args = CreatePixelShaderEventArgs.Args;
					break;
				case ID3D11DeviceContext_PSSetShader_VTableIndex when Args.IID == IID_ID3D11DeviceContext:
					var PSSetShaderEventArgs = new PSSetShaderEventArgs() { Args = Args };
					if (Args.Post) PostPSSetShader?.Invoke(Args.PPV, PSSetShaderEventArgs);
					else PrePSSetShader?.Invoke(Args.PPV, PSSetShaderEventArgs);
					Args = PSSetShaderEventArgs.Args;
					break;
				case ID3D11DeviceContext_DrawIndexed_VTableIndex when Args.IID == IID_ID3D11DeviceContext:
					var DrawIndexedEventArgs = new DrawIndexedEventArgs() { Args = Args };
					if (Args.Post) PostDrawIndexed?.Invoke(Args.PPV, DrawIndexedEventArgs);
					else PreDrawIndexed?.Invoke(Args.PPV, DrawIndexedEventArgs);
					Args = DrawIndexedEventArgs.Args;
					break;
				case ID3D11DeviceContext_Draw_VTableIndex when Args.IID == IID_ID3D11DeviceContext:
					var DrawEventArgs = new DrawEventArgs() { Args = Args };
					if (Args.Post) PostDraw?.Invoke(Args.PPV, DrawEventArgs);
					else PreDraw?.Invoke(Args.PPV, DrawEventArgs);
					Args = DrawEventArgs.Args;
					break;
				case ID3D11DeviceContext_PSSetConstantBuffers_VTableIndex when Args.IID == IID_ID3D11DeviceContext:
					var PSSetConstantBuffersEventArgs = new PSSetConstantBuffersEventArgs() { Args = Args };
					if (Args.Post) PostPSSetConstantBuffers?.Invoke(Args.PPV, PSSetConstantBuffersEventArgs);
					else PrePSSetConstantBuffers?.Invoke(Args.PPV, PSSetConstantBuffersEventArgs);
					Args = PSSetConstantBuffersEventArgs.Args;
					break;
				case ID3D11DeviceContext_DrawIndexedInstanced_VTableIndex when Args.IID == IID_ID3D11DeviceContext:
					var DrawIndexedInstancedEventArgs = new DrawIndexedInstancedEventArgs() { Args = Args };
					if (Args.Post) PostDrawIndexedInstanced?.Invoke(Args.PPV, DrawIndexedInstancedEventArgs);
					else PreDrawIndexedInstanced?.Invoke(Args.PPV, DrawIndexedInstancedEventArgs);
					Args = DrawIndexedInstancedEventArgs.Args;
					break;
				case ID3D11DeviceContext_DrawInstanced_VTableIndex when Args.IID == IID_ID3D11DeviceContext:
					var DrawInstancedEventArgs = new DrawInstancedEventArgs() { Args = Args };
					if (Args.Post) PostDrawInstanced?.Invoke(Args.PPV, DrawInstancedEventArgs);
					else PreDrawInstanced?.Invoke(Args.PPV, DrawInstancedEventArgs);
					Args = DrawInstancedEventArgs.Args;
					break;
				case ID3D11DeviceContext_OMSetRenderTargets_VTableIndex when Args.IID == IID_ID3D11DeviceContext:
					var OMSetRenderTargetsEventArgs = new OMSetRenderTargetsEventArgs() { Args = Args };
					if (Args.Post) PostOMSetRenderTargets?.Invoke(Args.PPV, OMSetRenderTargetsEventArgs);
					else PreOMSetRenderTargets?.Invoke(Args.PPV, OMSetRenderTargetsEventArgs);
					Args = OMSetRenderTargetsEventArgs.Args;
					break;
				case ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_VTableIndex when Args.IID == IID_ID3D11DeviceContext:
					var OMSetRenderTargetsAndUnorderedAccessViewsEventArgs = new OMSetRenderTargetsAndUnorderedAccessViewsEventArgs() { Args = Args };
					if (Args.Post) PostOMSetRenderTargetsAndUnorderedAccessViews?.Invoke(Args.PPV, OMSetRenderTargetsAndUnorderedAccessViewsEventArgs);
					else PreOMSetRenderTargetsAndUnorderedAccessViews?.Invoke(Args.PPV, OMSetRenderTargetsAndUnorderedAccessViewsEventArgs);
					Args = OMSetRenderTargetsAndUnorderedAccessViewsEventArgs.Args;
					break;
				case ID3D11DeviceContext_DrawAuto_VTableIndex when Args.IID == IID_ID3D11DeviceContext:
					var DrawAutoEventArgs = new DrawAutoEventArgs() { Args = Args };
					if (Args.Post) PostDrawAuto?.Invoke(Args.PPV, DrawAutoEventArgs);
					else PreDrawAuto?.Invoke(Args.PPV, DrawAutoEventArgs);
					Args = DrawAutoEventArgs.Args;
					break;
				case ID3D11DeviceContext_DrawIndexedInstancedIndirect_VTableIndex when Args.IID == IID_ID3D11DeviceContext:
					var DrawIndexedInstancedIndirectEventArgs = new DrawIndexedInstancedIndirectEventArgs() { Args = Args };
					if (Args.Post) PostDrawIndexedInstancedIndirect?.Invoke(Args.PPV, DrawIndexedInstancedIndirectEventArgs);
					else PreDrawIndexedInstancedIndirect?.Invoke(Args.PPV, DrawIndexedInstancedIndirectEventArgs);
					Args = DrawIndexedInstancedIndirectEventArgs.Args;
					break;
				case ID3D11DeviceContext_DrawInstancedIndirect_VTableIndex when Args.IID == IID_ID3D11DeviceContext:
					var DrawInstancedIndirectEventArgs = new DrawInstancedIndirectEventArgs() { Args = Args };
					if (Args.Post) PostDrawInstancedIndirect?.Invoke(Args.PPV, DrawInstancedIndirectEventArgs);
					else PreDrawInstancedIndirect?.Invoke(Args.PPV, DrawInstancedIndirectEventArgs);
					Args = DrawInstancedIndirectEventArgs.Args;
					break;
				case IDXGISwapChain3_SetColorSpace1_VTableIndex when Args.IID == IID_IDXGISwapChain3:
					var SetColorSpace1EventArgs = new SetColorSpace1EventArgs() { Args = Args };
					if (Args.Post) PostSetColorSpace1?.Invoke(Args.PPV, SetColorSpace1EventArgs);
					else PreSetColorSpace1?.Invoke(Args.PPV, SetColorSpace1EventArgs);
					Args = SetColorSpace1EventArgs.Args;
					break;
				case IDXGISwapChain4_SetHDRMetaData_VTableIndex when Args.IID == IID_IDXGISwapChain4:
					var SetHDRMetaDataEventArgs = new SetHDRMetaDataEventArgs() { Args = Args };
					if (Args.Post) PostSetHDRMetaData?.Invoke(Args.PPV, SetHDRMetaDataEventArgs);
					else PreSetHDRMetaData?.Invoke(Args.PPV, SetHDRMetaDataEventArgs);
					Args = SetHDRMetaDataEventArgs.Args;
					break;
			}

			var HookCallbackEventArgs = new HookCallbackEventArgs() { Args = Args };
			if (Args.Post) PostAnyEvent?.Invoke(Args.PPV, HookCallbackEventArgs);
			else PreAnyEvent?.Invoke(Args.PPV, HookCallbackEventArgs);
			Args = HookCallbackEventArgs.Args;
			Marshal.StructureToPtr(Args, Ptr, false);
		}

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct Arguments
		{
			public Guid IID;
			public IntPtr PPV;
			public uint VTableIndex;
			[MarshalAs(UnmanagedType.Bool)]
			public bool Stop;
			[MarshalAs(UnmanagedType.Bool)]
			public bool Post;
			public int Result;
			public fixed long Args[11]; // cannot use IntPtr here, but we only target 64-bit so, the `long` type is alright

			[StructLayout(LayoutKind.Sequential)]
			public struct OptionalStruct<T> where T : struct
			{
				[MarshalAs(UnmanagedType.Bool)]
				public bool Exist;
				public T Value;
			}

			public readonly IntPtr this[int index] => new IntPtr(Args[index]);
			public readonly T Get<T>(int index) where T : struct
			{
				return Marshal.PtrToStructure<T>(this[index]);
			}
			public readonly void Set<T>(int index, T value) where T : struct
			{
				Marshal.StructureToPtr(value, this[index], false);
			}
		}

		public class HookCallbackEventArgs : EventArgs
		{
			public Arguments Args;
		}

		#region Hook Callback Event Args

#pragma warning disable IDE1006

		public class CreateSwapChainEventArgs : HookCallbackEventArgs
		{
			public IntPtr Device
			{
				get => Args.Get<IntPtr>(0);
				set => Args.Set(0, value);
			}
			public DXGI_SWAP_CHAIN_DESC Desc
			{
				get => Args.Get<DXGI_SWAP_CHAIN_DESC>(1);
				set => Args.Set(1, value);
			}
			public IntPtr SwapChain
			{
				get => Args.Get<IntPtr>(2);
				set => Args.Set(2, value);
			}
		}
		public class CreateSwapChainForHwndEventArgs : HookCallbackEventArgs
		{
			public IntPtr Device
			{
				get => Args.Get<IntPtr>(0);
				set => Args.Set(0, value);
			}
			public IntPtr hWnd
			{
				get => Args.Get<IntPtr>(1);
				set => Args.Set(1, value);
			}
			public DXGI_SWAP_CHAIN_DESC1 Desc
			{
				get => Args.Get<DXGI_SWAP_CHAIN_DESC1>(2);
				set => Args.Set(2, value);
			}
			public DXGI_SWAP_CHAIN_FULLSCREEN_DESC? FullscreenDesc
			{
				get
				{
					var FullscreenDesc = Args.Get<Arguments.OptionalStruct<DXGI_SWAP_CHAIN_FULLSCREEN_DESC>>(3);
					return FullscreenDesc.Exist ? FullscreenDesc.Value : (DXGI_SWAP_CHAIN_FULLSCREEN_DESC?)null;
				}
				set
				{
					var FullscreenDesc = Args.Get<Arguments.OptionalStruct<DXGI_SWAP_CHAIN_FULLSCREEN_DESC>>(3);
					if (value.HasValue)
					{
						FullscreenDesc.Exist = true;
						FullscreenDesc.Value = value.Value;
					}
					else
					{
						FullscreenDesc.Exist = false;
					}
					Args.Set(3, FullscreenDesc);
				}
			}
			public IntPtr RestrictToOutput
			{
				get => Args.Get<IntPtr>(4);
				set => Args.Set(4, value);
			}
			public IntPtr SwapChain
			{
				get => Args.Get<IntPtr>(5);
				set => Args.Set(5, value);
			}
		}
		public class PresentEventArgs : HookCallbackEventArgs
		{
			public uint SyncInterval
			{
				get => Args.Get<uint>(0);
				set => Args.Set(0, value);
			}
			public uint Flags
			{
				get => Args.Get<uint>(1);
				set => Args.Set(1, value);
			}
		}
		public class ResizeBuffersEventArgs : HookCallbackEventArgs
		{
			public uint BufferCount
			{
				get => Args.Get<uint>(0);
				set => Args.Set(0, value);
			}
			public uint Width
			{
				get => Args.Get<uint>(1);
				set => Args.Set(1, value);
			}
			public uint Height
			{
				get => Args.Get<uint>(2);
				set => Args.Set(2, value);
			}
			public DXGI_FORMAT NewFormat
			{
				get => Args.Get<DXGI_FORMAT>(3);
				set => Args.Set(3, value);
			}
			public DXGI_SWAP_CHAIN_FLAG SwapChainFlags
			{
				get => Args.Get<DXGI_SWAP_CHAIN_FLAG>(4);
				set => Args.Set(4, value);
			}
		}
		public class ResizeTargetEventArgs : HookCallbackEventArgs
		{
			public DXGI_MODE_DESC NewTargetParameters
			{
				get => Args.Get<DXGI_MODE_DESC>(0);
				set => Args.Set(0, value);
			}
		}
		public class Present1EventArgs : HookCallbackEventArgs
		{
			public uint SyncInterval
			{
				get => Args.Get<uint>(0);
				set => Args.Set(0, value);
			}
			public uint Flags
			{
				get => Args.Get<uint>(1);
				set => Args.Set(1, value);
			}
			public DXGI_PRESENT_PARAMETERS PresentParameters
			{
				get => Args.Get<DXGI_PRESENT_PARAMETERS>(2);
				set => Args.Set(2, value);
			}
		}
		public class CreateShaderResourceViewEventArgs : HookCallbackEventArgs
		{
			public IntPtr Resource
			{
				get => Args.Get<IntPtr>(0);
				set => Args.Set(0, value);
			}
			public D3D11_SHADER_RESOURCE_VIEW_DESC? Desc
			{
				get
				{
					var SRVDesc = Args.Get<Arguments.OptionalStruct<D3D11_SHADER_RESOURCE_VIEW_DESC>>(1);
					return SRVDesc.Exist ? SRVDesc.Value : (D3D11_SHADER_RESOURCE_VIEW_DESC?)null;
				}
				set
				{
					var SRVDesc = Args.Get<Arguments.OptionalStruct<D3D11_SHADER_RESOURCE_VIEW_DESC>>(1);
					if (value.HasValue)
					{
						SRVDesc.Exist = true;
						SRVDesc.Value = value.Value;
					}
					else
					{
						SRVDesc.Exist = false;
					}
					Args.Set(1, SRVDesc);
				}
			}
			public IntPtr ShaderResourceView
			{
				get => Args.Get<IntPtr>(2);
				set => Args.Set(2, value);
			}
		}
		public class CreateRenderTargetViewEventArgs : HookCallbackEventArgs
		{
			public IntPtr Resource
			{
				get => Args.Get<IntPtr>(0);
				set => Args.Set(0, value);
			}
			public D3D11_RENDER_TARGET_VIEW_DESC? Desc
			{
				get
				{
					var RTVDesc = Args.Get<Arguments.OptionalStruct<D3D11_RENDER_TARGET_VIEW_DESC>>(1);
					return RTVDesc.Exist ? RTVDesc.Value : (D3D11_RENDER_TARGET_VIEW_DESC?)null;
				}
				set
				{
					var RTVDesc = Args.Get<Arguments.OptionalStruct<D3D11_RENDER_TARGET_VIEW_DESC>>(1);
					if (value.HasValue)
					{
						RTVDesc.Exist = true;
						RTVDesc.Value = value.Value;
					}
					else
					{
						RTVDesc.Exist = false;
					}
					Args.Set(1, RTVDesc);
				}
			}
			public IntPtr RenderTargetView
			{
				get => Args.Get<IntPtr>(2);
				set => Args.Set(2, value);
			}
		}
		public class CreatePixelShaderEventArgs : HookCallbackEventArgs
		{
			public NativeArrayAccess<byte> ShaderBytecode
			{
				get => new NativeArrayAccess<byte>(Args.Get<IntPtr>(0), (int)BytecodeLength);
				set => Args.Set(0, value.FixedAddress);
			}
			public ulong BytecodeLength
			{
				get => Args.Get<ulong>(1);
				set => Args.Set(1, value);
			}
			public IntPtr ClassLinkage
			{
				get => Args.Get<IntPtr>(2);
				set => Args.Set(2, value);
			}
			public IntPtr PixelShader
			{
				get => Args.Get<IntPtr>(3);
				set => Args.Set(3, value);
			}
		}
		public class PSSetShaderEventArgs : HookCallbackEventArgs
		{
			public IntPtr PixelShader
			{
				get => Args.Get<IntPtr>(0);
				set => Args.Set(0, value);
			}
			public NativeArrayAccess<IntPtr> ClassInstances
			{
				get => new NativeArrayAccess<IntPtr>(Args.Get<IntPtr>(1), (int)NumClassInstances);
				set => Args.Set(1, value.FixedAddress);
			}
			public uint NumClassInstances
			{
				get => Args.Get<uint>(2);
				set => Args.Set(2, value);
			}
		}
		public class DrawIndexedEventArgs : HookCallbackEventArgs
		{
			public uint IndexCount
			{
				get => Args.Get<uint>(0);
				set => Args.Set(0, value);
			}
			public uint StartIndexLocation
			{
				get => Args.Get<uint>(1);
				set => Args.Set(1, value);
			}
			public int BaseVertexLocation
			{
				get => Args.Get<int>(2);
				set => Args.Set(2, value);
			}
		}
		public class DrawEventArgs : HookCallbackEventArgs
		{
			public uint VertexCount
			{
				get => Args.Get<uint>(0);
				set => Args.Set(0, value);
			}
			public uint StartVertexLocation
			{
				get => Args.Get<uint>(1);
				set => Args.Set(1, value);
			}
		}
		public class PSSetConstantBuffersEventArgs : HookCallbackEventArgs
		{
			public uint StartSlot
			{
				get => Args.Get<uint>(0);
				set => Args.Set(0, value);
			}
			public uint NumBuffers
			{
				get => Args.Get<uint>(1);
				set => Args.Set(1, value);
			}
			public NativeArrayAccess<IntPtr> ConstantBuffers
			{
				get => new NativeArrayAccess<IntPtr>(Args.Get<IntPtr>(2), (int)NumBuffers);
				set => Args.Set(2, value.FixedAddress);
			}
		}
		public class DrawIndexedInstancedEventArgs : HookCallbackEventArgs
		{
			public uint IndexCountPerInstance
			{
				get => Args.Get<uint>(0);
				set => Args.Set(0, value);
			}
			public uint InstanceCount
			{
				get => Args.Get<uint>(1);
				set => Args.Set(1, value);
			}
			public uint StartIndexLocation
			{
				get => Args.Get<uint>(2);
				set => Args.Set(2, value);
			}
			public int BaseVertexLocation
			{
				get => Args.Get<int>(3);
				set => Args.Set(3, value);
			}
			public uint StartInstanceLocation
			{
				get => Args.Get<uint>(4);
				set => Args.Set(4, value);
			}
		}
		public class DrawInstancedEventArgs : HookCallbackEventArgs
		{
			public uint VertexCountPerInstance
			{
				get => Args.Get<uint>(0);
				set => Args.Set(0, value);
			}
			public uint InstanceCount
			{
				get => Args.Get<uint>(1);
				set => Args.Set(1, value);
			}
			public uint StartVertexLocation
			{
				get => Args.Get<uint>(2);
				set => Args.Set(2, value);
			}
			public uint StartInstanceLocation
			{
				get => Args.Get<uint>(3);
				set => Args.Set(3, value);
			}
		}
		public class OMSetRenderTargetsEventArgs : HookCallbackEventArgs
		{
			public uint NumViews
			{
				get => Args.Get<uint>(0);
				set => Args.Set(0, value);
			}
			public NativeArrayAccess<IntPtr> RenderTargetViews
			{
				get => new NativeArrayAccess<IntPtr>(Args.Get<IntPtr>(1), (int)NumViews);
				set => Args.Set(1, value.FixedAddress);
			}
			public IntPtr DepthStencilView
			{
				get => Args.Get<IntPtr>(2);
				set => Args.Set(2, value);
			}
		}
		public class OMSetRenderTargetsAndUnorderedAccessViewsEventArgs : HookCallbackEventArgs
		{
			public uint NumViews
			{
				get => Args.Get<uint>(0);
				set => Args.Set(0, value);
			}
			public NativeArrayAccess<IntPtr> RenderTargetViews
			{
				get => new NativeArrayAccess<IntPtr>(Args.Get<IntPtr>(1), (int)NumViews);
				set => Args.Set(1, value.FixedAddress);
			}
			public IntPtr DepthStencilView
			{
				get => Args.Get<IntPtr>(2);
				set => Args.Set(2, value);
			}
			public uint UAVStartSlot
			{
				get => Args.Get<uint>(3);
				set => Args.Set(3, value);
			}
			public uint NumUAVs
			{
				get => Args.Get<uint>(4);
				set => Args.Set(4, value);
			}
			public NativeArrayAccess<IntPtr> UnorderedAccessViews
			{
				get => new NativeArrayAccess<IntPtr>(Args.Get<IntPtr>(5), (int)NumViews);
				set => Args.Set(5, value.FixedAddress);
			}
			public NativeArrayAccess<uint> UAVInitialCounts
			{
				get => new NativeArrayAccess<uint>(Args.Get<IntPtr>(6), (int)NumViews);
				set => Args.Set(6, value.FixedAddress);
			}
		}
		public class DrawAutoEventArgs : HookCallbackEventArgs
		{
		}
		public class DrawIndexedInstancedIndirectEventArgs : HookCallbackEventArgs
		{
			public IntPtr BufferForArgs
			{
				get => Args.Get<IntPtr>(0);
				set => Args.Set(0, value);
			}
			public uint AlignedByteOffsetForArgs
			{
				get => Args.Get<uint>(1);
				set => Args.Set(1, value);
			}
		}
		public class DrawInstancedIndirectEventArgs : HookCallbackEventArgs
		{
			public IntPtr BufferForArgs
			{
				get => Args.Get<IntPtr>(0);
				set => Args.Set(0, value);
			}
			public uint AlignedByteOffsetForArgs
			{
				get => Args.Get<uint>(1);
				set => Args.Set(1, value);
			}
		}
		public class SetColorSpace1EventArgs : HookCallbackEventArgs
		{
			public DXGI_COLOR_SPACE_TYPE ColorSpace
			{
				get => Args.Get<DXGI_COLOR_SPACE_TYPE>(0);
				set => Args.Set(0, value);
			}
		}
		public class SetHDRMetaDataEventArgs : HookCallbackEventArgs
		{
			public DXGI_HDR_METADATA_TYPE Type
			{
				get => Args.Get<DXGI_HDR_METADATA_TYPE>(0);
				set => Args.Set(0, value);
			}
			public uint Size
			{
				get => Args.Get<uint>(1);
				set => Args.Set(1, value);
			}
			public DXGI_HDR_METADATA_HDR10? MetaData10
			{
				get
				{
					var Data = Args.Get<Arguments.OptionalStruct<DXGI_HDR_METADATA_HDR10>>(2);
					return Data.Exist ? Data.Value : (DXGI_HDR_METADATA_HDR10?)null;
				}
				set
				{
					var Data = Args.Get<Arguments.OptionalStruct<DXGI_HDR_METADATA_HDR10>>(2);
					if (value.HasValue)
					{
						Data.Exist = true;
						Data.Value = value.Value;
					}
					else
					{
						Data.Exist = false;
					}
					Args.Set(2, Data);
				}
			}
			public DXGI_HDR_METADATA_HDR10PLUS? MetaData10Plus
			{
				get
				{
					var Data = Args.Get<Arguments.OptionalStruct<DXGI_HDR_METADATA_HDR10PLUS>>(3);
					return Data.Exist ? Data.Value : (DXGI_HDR_METADATA_HDR10PLUS?)null;
				}
				set
				{
					var Data = Args.Get<Arguments.OptionalStruct<DXGI_HDR_METADATA_HDR10PLUS>>(3);
					if (value.HasValue)
					{
						Data.Exist = true;
						Data.Value = value.Value;
					}
					else
					{
						Data.Exist = false;
					}
					Args.Set(3, Data);
				}
			}
		}

#pragma warning restore IDE1006

		#endregion

		#region DirectXHook.cpp 

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate void CallbackProc(IntPtr Args);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void LogCallbackProc([MarshalAs(UnmanagedType.LPWStr)] string Message);



		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int InstallHook();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int UninstallHook();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static void SetRunning(bool Running);



		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static void SetHookCallback1([MarshalAs(UnmanagedType.FunctionPtr)] CallbackProc HookCallback1);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static void SetLogCallback([MarshalAs(UnmanagedType.FunctionPtr)] LogCallbackProc LogCallback);



		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int InstallHookForDevice(IntPtr Device);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int InstallHookForDeviceContext(IntPtr DeviceContext);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int InstallHookForFactory(IntPtr Factory, IntPtr Factory2);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int InstallHookForSwapChain(IntPtr SwapChain, IntPtr SwapChain1, IntPtr SwapChain3, IntPtr SwapChain4);



		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int UninstallHookForDevice();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int UninstallHookForDeviceContext();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int UninstallHookForFactory();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int UninstallHookForSwapChain();



		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int DetermineOutputHDR(IntPtr SwapChain, out DXGI_FORMAT Format, out bool IsHDR);



		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_Present_MemoryOriginal_Proc();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_Present1_MemoryOriginal_Proc();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_Present_MemoryOriginal_Bytes();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_Present1_MemoryOriginal_Bytes();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static bool Get_Present_DetourHookDetected();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static bool Get_Present1_DetourHookDetected();



		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_D3D11_DLL_BaseAddress();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_DXGI_DLL_BaseAddress();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static IntPtr Get_GameOverlayRenderer64_DLL_BaseAddress();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static uint Get_D3D11_DLL_ImageSize();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static uint Get_DXGI_DLL_ImageSize();

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static uint Get_GameOverlayRenderer64_DLL_ImageSize();



		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern unsafe static int GetName(IntPtr D3D11_Interface, IntPtr* CharArray);

		private static readonly object GetNameMutex = new object();
		/// <summary>
		/// get debug name for a D3D11 interface
		/// </summary>
		/// <param name="D3D11_Interface">ID3D11Device or ID3D11DeviceChild</param>
		public static string? GetName(this IntPtr D3D11_Interface)
		{
			lock (GetNameMutex)
			{
				unsafe
				{
					IntPtr CharArray;
					if (GetName(D3D11_Interface, &CharArray) == 0)
					{
						return Marshal.PtrToStringUTF8(CharArray);
					}
					else return null;
				}
			}
		}

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern unsafe static int CompileVertexShader(IntPtr Device,
			[MarshalAs(UnmanagedType.LPWStr)] string FileName,
			[MarshalAs(UnmanagedType.LPStr)] string EntryPoint,
			[MarshalAs(UnmanagedType.LPStr)] string? DebugName,
			IntPtr* VertexShader);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern unsafe static int CompilePixelShader(IntPtr Device,
			[MarshalAs(UnmanagedType.LPWStr)] string FileName,
			[MarshalAs(UnmanagedType.LPStr)] string EntryPoint,
			[MarshalAs(UnmanagedType.LPStr)] string? DebugName,
			IntPtr* PixelShader);

		/*
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern unsafe static int EnsureConstantBuffer(IntPtr Device, IntPtr DeviceContext,
			IntPtr* ConstantBuffer, float* Values);
		*/

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static bool JmpEndsUpInRange(IntPtr SrcAddr, IntPtr RangeStart, uint Size);

		#endregion

		#region OverlayDirect2D.cpp

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int InitOverlay();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static void ReleaseOverlay();
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal unsafe extern static int SwapChainBeginDraw(IntPtr SwapChain, uint Index,
			IntPtr* ppInstance);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal unsafe extern static int SurfaceBeginDraw(IntPtr Surface,
			IntPtr* ppInstance);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal unsafe extern static int StereoSurfaceBeginDraw(IntPtr SurfaceLeft, IntPtr SurfaceRight,
			IntPtr* ppInstance);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int SwapChainEndDraw(IntPtr SwapChain, uint Index,
			IntPtr pInstance);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int SurfaceEndDraw(IntPtr Surface,
			IntPtr pInstance);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int StereoSurfaceEndDraw(IntPtr SurfaceLeft, IntPtr SurfaceRight,
			IntPtr pInstance);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int ReleaseSwapChainResources(IntPtr SwapChain);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static int SetColor(this IntPtr pInstance, D2D1_COLOR_F Color);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static int SetOpacity(this IntPtr pInstance, float Opacity);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static int SetFont(this IntPtr pInstance, [MarshalAs(UnmanagedType.LPWStr)] string FontFamily,
			DWRITE_FONT_WEIGHT FontWeight, DWRITE_FONT_STYLE FontStyle, DWRITE_FONT_STRETCH FontStretch,
			float FontSize, [MarshalAs(UnmanagedType.LPWStr)] string FontLocale);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static int SetFontParams(this IntPtr pInstance,
			DWRITE_TEXT_ALIGNMENT TextAlignment = DWRITE_TEXT_ALIGNMENT.NULL,
			DWRITE_PARAGRAPH_ALIGNMENT ParagraphAlignment = DWRITE_PARAGRAPH_ALIGNMENT.NULL,
			DWRITE_WORD_WRAPPING WordWrapping = DWRITE_WORD_WRAPPING.NULL,
			DWRITE_READING_DIRECTION ReadingDirection = DWRITE_READING_DIRECTION.NULL,
			DWRITE_FLOW_DIRECTION FlowDirection = DWRITE_FLOW_DIRECTION.NULL,
			float IncrementalTabStop = float.NaN,
			DWRITE_LINE_SPACING_METHOD LineSpacingMethod = DWRITE_LINE_SPACING_METHOD.NULL,
			float LineSpacing = float.NaN, float Baseline = float.NaN);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static int SetGDICompatibleText(this IntPtr pInstance, [MarshalAs(UnmanagedType.LPWStr)] string Str,
			float LayoutWidth, float LayoutHeight, float PixelsPerDip);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static void DrawEllipse(this IntPtr pInstance, D2D1_POINT_2F Point, float RadiusX, float RadiusY, float StrokeWidth);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static void DrawLine(this IntPtr pInstance, D2D1_POINT_2F Src, D2D1_POINT_2F Dst, float StrokeWidth);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static void DrawRectangle(this IntPtr pInstance, D2D1_RECT_F Rect, float RoundedRadiusX, float RoundedRadiusY, float StrokeWidth);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static void DrawPlainText(this IntPtr pInstance, [MarshalAs(UnmanagedType.LPWStr)] string Str, D2D1_RECT_F Rect);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static int DrawGDICompatibleText(this IntPtr pInstance, D2D1_POINT_2F Origin);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static int DrawGDICompatibleTextMetrics(this IntPtr pInstance, uint Index, uint Length, float OriginX, float OriginY);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static int DrawGDICompatibleTextCaret(this IntPtr pInstance, bool Trailing, float StrokeWidth);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static int BeginDrawBezier(this IntPtr pInstance, D2D1_POINT_2F StartPoint);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static void AddBezier(this IntPtr pInstance, D2D1_POINT_2F Start, D2D1_POINT_2F Reference, D2D1_POINT_2F End);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static int EndDrawBezier(this IntPtr pInstance, D2D1_POINT_2F EndPoint, float StrokeWidth);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static void FillEllipse(this IntPtr pInstance, D2D1_POINT_2F Point, float RadiusX, float RadiusY);

		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static void FillRectangle(this IntPtr pInstance, D2D1_RECT_F Rect, float RoundedRadiusX, float RoundedRadiusY);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static void SetTransform(this IntPtr pInstance, Matrix3x2F Matrix);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static void SetTransformIdentity(this IntPtr pInstance);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static void SetTransformScale(this IntPtr pInstance, D2D1_POINT_2F CenterPoint, float ScaleX, float ScaleY);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static void SetDpi(this IntPtr pInstance, float DpiX, float DpiY);
		
		[DllImport("DirectXHook.dll", CallingConvention = CallingConvention.StdCall)]
		public extern static void PrintFrameTime(this IntPtr pInstance, bool IsHDR);

		#endregion

		#region ElementsOfHarmony.Native

		[DllImport("ElementsOfHarmony.Native.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static int InitNativeCallbacks(
			bool HDRTakeOverOutputFormat,
			float HDRDynamicRangeFactor);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal delegate bool BoolVariableCallback();
		
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		internal delegate float FloatVariableCallback();
		
		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		internal delegate string? StringVariableCallback([MarshalAs(UnmanagedType.LPWStr)] string Name);

		[DllImport("ElementsOfHarmony.Native.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static void SetCallbacks(
			[MarshalAs(UnmanagedType.FunctionPtr)] StringVariableCallback VertexShader,
			[MarshalAs(UnmanagedType.FunctionPtr)] StringVariableCallback PixelShader);

		[DllImport("ElementsOfHarmony.Native.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static void SetIsHDR(bool IsHDR);

		[DllImport("ElementsOfHarmony.Native.dll", CallingConvention = CallingConvention.StdCall)]
		internal extern static void SetWithinOverlayPass(bool WithinOverlayPass);

		#endregion
	}
}
