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
using static ElementsOfHarmony.Settings.DirectXHook;

namespace ElementsOfHarmony
{
	public static class DirectXHook
	{
		public static void Init()
		{
			try
			{
				unsafe
				{
					SetCallbacks(HookCallback, Log.Message);
				}

				int HResult = InstallHook();
				Log.Message($"InstallHook() returns {HResult:X}");
				Marshal.ThrowExceptionForHR(HResult);

				HResult = InitOverlay();
				Log.Message($"InitOverlay() returns {HResult:X}");
				Marshal.ThrowExceptionForHR(HResult);

				Application.quitting += Application_quitting;
				SceneManager.sceneLoaded += SceneManager_sceneLoaded;
				PrePresent += OnPrePresent;
				PrePresent1 += OnPrePresent1;

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

				Log.Message("DirectX hook complete");
			}
			catch (Exception e)
			{
			repeat:
				Log.Message($"{typeof(DirectXHook).FullName} - {e.GetType()}\n{e.StackTrace}\n{e.Message}");
				if (e.InnerException != null)
				{
					e = e.InnerException;
					goto repeat;
				}
			}
			finally
			{
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

		private unsafe static void HookCallback(IntPtr Ptr)
		{
			Arguments Args = Marshal.PtrToStructure<Arguments>(Ptr);
			switch (Args.VTableIndex)
			{
				case IDXGIFactory_CreateSwapChain_VTableIndex when Args.IID == IDXGIFactory_IID:
					if (Args.Post) PostCreateSwapChain?.Invoke(Args.PPV, new CreateSwapChainEventArgs() { Args = Args });
					else PreCreateSwapChain?.Invoke(Args.PPV, new CreateSwapChainEventArgs() { Args = Args });
					break;
				case IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex when Args.IID == IDXGIFactory2_IID:
					if (Args.Post) PostCreateSwapChainForHwnd?.Invoke(Args.PPV, new CreateSwapChainForHwndEventArgs() { Args = Args });
					else PreCreateSwapChainForHwnd?.Invoke(Args.PPV, new CreateSwapChainForHwndEventArgs() { Args = Args });
					break;
				case IDXGISwapChain_Present_VTableIndex when Args.IID == IDXGISwapChain_IID:
					if (Args.Post) PostPresent?.Invoke(Args.PPV, new PresentEventArgs() { Args = Args });
					else PrePresent?.Invoke(Args.PPV, new PresentEventArgs() { Args = Args });
					break;
				case IDXGISwapChain1_Present1_VTableIndex when Args.IID == IDXGISwapChain1_IID:
					if (Args.Post) PostPresent1?.Invoke(Args.PPV, new Present1EventArgs() { Args = Args });
					else PrePresent1?.Invoke(Args.PPV, new Present1EventArgs() { Args = Args });
					break;
				case ID3D11DeviceContext_OMSetRenderTargets_VTableIndex when Args.IID == ID3D11DeviceContext_IID:
					if (Args.Post) PostOMSetRenderTargets?.Invoke(Args.PPV, new OMSetRenderTargetsEventArgs() { Args = Args });
					else PreOMSetRenderTargets?.Invoke(Args.PPV, new OMSetRenderTargetsEventArgs() { Args = Args });
					break;
				case ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_VTableIndex when Args.IID == ID3D11DeviceContext_IID:
					if (Args.Post) PostOMSetRenderTargetsAndUnorderedAccessViews?.Invoke(Args.PPV, new OMSetRenderTargetsAndUnorderedAccessViewsEventArgs() { Args = Args });
					else PreOMSetRenderTargetsAndUnorderedAccessViews?.Invoke(Args.PPV, new OMSetRenderTargetsAndUnorderedAccessViewsEventArgs() { Args = Args });
					break;
			}
		}

		private static readonly UnityEngine.Events.UnityAction<Scene, LoadSceneMode> SceneManager_sceneLoaded = (Scene arg0, LoadSceneMode arg1) =>
		{
			if (AllowHDR != null)
			{
				foreach (Camera camera in Camera.allCameras)
				{
					camera.allowHDR = AllowHDR.Value;
				}
			}
			if (MSAA != null) QualitySettings.antiAliasing = MSAA.Value;
			if (MSAA != null && MSAA > 0)
			{
				foreach (Camera camera in Camera.allCameras)
				{
					camera.allowMSAA = true;
				}
			}
			if (VSyncInterval != null) QualitySettings.vSyncCount = VSyncInterval.Value;
			if (TargetFrameRate != null) Application.targetFrameRate = TargetFrameRate.Value;

			// Obsolete in AZHM's Unity version but is still usable
			Screen.SetResolution(Width ?? Screen.width, Height ?? Screen.height,
				Settings.DirectXHook.FullScreenMode ?? Screen.fullScreenMode,
				RefreshRate ?? 0);

			SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
		};

		private static void Application_quitting()
		{
			SetRunning(false);
			Marshal.GetExceptionForHR(UninstallHook());

			ReleaseOverlay();
		}

		private static void OnPrePresent(object sender, PresentEventArgs _)
		{
			InvokeOverlay((IntPtr)sender);
		}
		private static void OnPrePresent1(object sender, Present1EventArgs _)
		{
			InvokeOverlay((IntPtr)sender);
		}
		private static void InvokeOverlay(IntPtr pSwapChain)
		{
			IntPtr DeviceInstance;
			unsafe
			{
				Marshal.ThrowExceptionForHR(SwapChainBeginDraw(pSwapChain, 0, &DeviceInstance));
			}
			OverlayDraw?.Invoke(DeviceInstance, EventArgs.Empty);
			Marshal.ThrowExceptionForHR(SwapChainEndDraw(pSwapChain, 0, DeviceInstance));
		}



		public const ulong IDXGIFactory_CreateSwapChain_VTableIndex = 10;
		public const ulong IDXGIFactory2_CreateSwapChainForHwnd_VTableIndex = 15;
		public const ulong IDXGISwapChain_Present_VTableIndex = 8;
		public const ulong IDXGISwapChain1_Present1_VTableIndex = 22;
		public const ulong ID3D11DeviceContext_OMSetRenderTargets_VTableIndex = 33;
		public const ulong ID3D11DeviceContext_OMSetRenderTargetsAndUnorderedAccessViews_VTableIndex = 34;
		public static readonly Guid IUnknown_IID = new Guid("00000000-0000-0000-C000-000000000046");
		public static readonly Guid IDXGIFactory_IID = new Guid("7b7166ec-21c7-44ae-b21a-c9ae321ae369");
		public static readonly Guid IDXGIFactory2_IID = new Guid("50c83a1c-e072-4c48-87b0-3630fa36a6d0");
		public static readonly Guid IDXGISwapChain_IID = new Guid("310d36a0-d2e7-4c0a-aa04-6a9d23b8886a");
		public static readonly Guid IDXGISwapChain1_IID = new Guid("790a45f7-0d42-4876-983a-0a55cfe6f4aa");
		public static readonly Guid ID3D11DeviceContext_IID = new Guid("c0bfa96c-e089-44fb-8eaf-26f8796190da");
		public static event EventHandler<CreateSwapChainEventArgs>? PreCreateSwapChain, PostCreateSwapChain;
		public static event EventHandler<CreateSwapChainForHwndEventArgs>? PreCreateSwapChainForHwnd, PostCreateSwapChainForHwnd;
		public static event EventHandler<PresentEventArgs>? PrePresent, PostPresent;
		public static event EventHandler<Present1EventArgs>? PrePresent1, PostPresent1;
		public static event EventHandler<OMSetRenderTargetsEventArgs>? PreOMSetRenderTargets, PostOMSetRenderTargets;
		public static event EventHandler<OMSetRenderTargetsAndUnorderedAccessViewsEventArgs>? PreOMSetRenderTargetsAndUnorderedAccessViews, PostOMSetRenderTargetsAndUnorderedAccessViews;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public unsafe delegate int IDXGIDeviceSubObject_GetDevice_Proc(IntPtr pInstance, Guid IID, IntPtr* PPV);

		public static readonly Dictionary<IntPtr, int> D3D11DeviceRefCountThreashold = new Dictionary<IntPtr, int>();
		public static event EventHandler? OverlayDraw;



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
		internal extern static void SetCallbacks(
			[MarshalAs(UnmanagedType.FunctionPtr)] CallbackProc HookCallback,
			[MarshalAs(UnmanagedType.FunctionPtr)] LogCallbackProc LogCallback);



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
		internal extern static bool JmpEndsUpInRange(IntPtr SrcAddr, IntPtr RangeStart, uint Size);



		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct Arguments
		{
			public Guid IID;
			public IntPtr PPV;
			public ulong VTableIndex;
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

		public class NativeArrayAccess<T>
			where T : struct
		{
			public NativeArrayAccess(IntPtr Address, int ElementCount)
			{
				this.Address = Address;
				this.ElementCount = ElementCount;
			}
			protected readonly IntPtr Address;
			protected readonly int ElementCount;
			public T this[int index]
			{
				get
				{
					if (Address == IntPtr.Zero) throw new NullReferenceException();
					else if (index < 0 || index >= ElementCount) throw new IndexOutOfRangeException();
					else return Marshal.PtrToStructure<T>(IntPtr.Add(Address, index * Marshal.SizeOf<T>()));
				}
				set
				{
					if (Address == IntPtr.Zero) throw new NullReferenceException();
					else if (index < 0 || index >= ElementCount) throw new IndexOutOfRangeException();
					else Marshal.StructureToPtr(value, IntPtr.Add(Address, index * Marshal.SizeOf<T>()), false);
				}
			}
			public IntPtr FixedAddress => Address;
			public int Length => ElementCount;
		}
		public class AllocatedArrayAccess<T> : NativeArrayAccess<T>, IDisposable
			where T : struct
		{
			public AllocatedArrayAccess(int ElementCount) :
				base(Marshal.AllocHGlobal(Marshal.SizeOf<T>() * ElementCount), ElementCount)
			{ }

			private bool disposedValue;
			protected virtual void Dispose(bool disposing)
			{
				if (!disposedValue)
				{
					if (disposing)
					{
						Marshal.FreeHGlobal(Address);
					}
					disposedValue = true;
				}
			}
			~AllocatedArrayAccess()
			{
				Dispose(disposing: true);
			}
			public void Dispose()
			{
				Dispose(disposing: true);
				GC.SuppressFinalize(this);
			}
		}

		public class HookCallbackEventArgs : EventArgs
		{
			public Arguments Args;
		}
#pragma warning disable IDE1006

		public class CreateSwapChainEventArgs : HookCallbackEventArgs
		{
			public IntPtr pDevice
			{
				get => Args.Get<IntPtr>(0);
				set => Args.Set(0, value);
			}
			public DXGI_SWAP_CHAIN_DESC Desc
			{
				get => Args.Get<DXGI_SWAP_CHAIN_DESC>(1);
				set => Args.Set(1, value);
			}
			public IntPtr ppSwapChain
			{
				get => Args.Get<IntPtr>(2);
				set => Args.Set(2, value);
			}
		}
		public class CreateSwapChainForHwndEventArgs : HookCallbackEventArgs
		{
			public IntPtr pDevice
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
#pragma warning restore IDE1006

		#region Overlay

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
		public extern static void SetDpi(this IntPtr pInstance, float DpiX, float DpiY);

		#endregion

		#region DXGI
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
				readonly get => Right - Left;
				set => Right = Left + value;
			}
			public int Height
			{
				readonly get => Bottom - Top;
				set => Bottom = Top + value;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X, Y;
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
				readonly get
				{
					if (pDirtyRects != IntPtr.Zero)
					{
						return new NativeArrayAccess<RECT>(pDirtyRects, (int)DirtyRectsCount);
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
						DirtyRectsCount = 0;
						pDirtyRects = IntPtr.Zero;
					}
					else
					{
						DirtyRectsCount = (uint)value.Length;
						pDirtyRects = value.FixedAddress;
					}
				}
			}
			public RECT? ScrollRect
			{
				readonly get
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
					if (value == null) pScrollRect = IntPtr.Zero;
					else if (pScrollRect == IntPtr.Zero) throw new NullReferenceException();
					else Marshal.StructureToPtr(value.Value, pScrollRect, false);
				}
			}
			public POINT? ScrollOffset
			{
				readonly get
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
					if (value == null) pScrollOffset = IntPtr.Zero;
					else if (pScrollOffset == IntPtr.Zero) throw new NullReferenceException();
					else Marshal.StructureToPtr(value.Value, pScrollOffset, false);
				}
			}
		}

		#endregion

		#region Direct2D

		[StructLayout(LayoutKind.Sequential)]
		public struct D2D1_COLOR_F
		{
			public float R, G, B, A;
			public static implicit operator D2D1_COLOR_F(Color UnityColor) => new D2D1_COLOR_F
			{
				R = UnityColor.r,
				G = UnityColor.g,
				B = UnityColor.b,
				A = UnityColor.a,
			};
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct D2D1_POINT_2F
		{
			public float X, Y;
			public static implicit operator D2D1_POINT_2F(Vector2 V2) => new D2D1_POINT_2F
			{
				X = V2.x,
				Y = V2.y,
			};
		}
		
		[StructLayout(LayoutKind.Sequential)]
		public struct D2D1_RECT_F
		{
			public float Left, Top, Right, Bottom;
			public static implicit operator D2D1_RECT_F(Rect UnityRect) => new D2D1_RECT_F
			{
				Left = UnityRect.xMin,
				Top = UnityRect.yMin,
				Right = UnityRect.xMax,
				Bottom = UnityRect.yMax,
			};
		}
		
		[StructLayout(LayoutKind.Sequential)]
		public struct Matrix3x2F
		{
			public float
				M11, M12,
				M21, M22,
				M31, M32;
		}

		/// <summary>
		/// The font weight enumeration describes common values for degree of blackness or thickness of strokes of characters in a font.
		/// Font weight values less than 1 or greater than 999 are considered to be invalid, and they are rejected by font API functions.
		/// </summary>
		public enum DWRITE_FONT_WEIGHT
		{
			/// <summary>
			/// Predefined font weight : Thin (100).
			/// </summary>
			DWRITE_FONT_WEIGHT_THIN = 100,

			/// <summary>
			/// Predefined font weight : Extra-light (200).
			/// </summary>
			DWRITE_FONT_WEIGHT_EXTRA_LIGHT = 200,

			/// <summary>
			/// Predefined font weight : Ultra-light (200).
			/// </summary>
			DWRITE_FONT_WEIGHT_ULTRA_LIGHT = 200,

			/// <summary>
			/// Predefined font weight : Light (300).
			/// </summary>
			DWRITE_FONT_WEIGHT_LIGHT = 300,

			/// <summary>
			/// Predefined font weight : Semi-light (350).
			/// </summary>
			DWRITE_FONT_WEIGHT_SEMI_LIGHT = 350,

			/// <summary>
			/// Predefined font weight : Normal (400).
			/// </summary>
			DWRITE_FONT_WEIGHT_NORMAL = 400,

			/// <summary>
			/// Predefined font weight : Regular (400).
			/// </summary>
			DWRITE_FONT_WEIGHT_REGULAR = 400,

			/// <summary>
			/// Predefined font weight : Medium (500).
			/// </summary>
			DWRITE_FONT_WEIGHT_MEDIUM = 500,

			/// <summary>
			/// Predefined font weight : Demi-bold (600).
			/// </summary>
			DWRITE_FONT_WEIGHT_DEMI_BOLD = 600,

			/// <summary>
			/// Predefined font weight : Semi-bold (600).
			/// </summary>
			DWRITE_FONT_WEIGHT_SEMI_BOLD = 600,

			/// <summary>
			/// Predefined font weight : Bold (700).
			/// </summary>
			DWRITE_FONT_WEIGHT_BOLD = 700,

			/// <summary>
			/// Predefined font weight : Extra-bold (800).
			/// </summary>
			DWRITE_FONT_WEIGHT_EXTRA_BOLD = 800,

			/// <summary>
			/// Predefined font weight : Ultra-bold (800).
			/// </summary>
			DWRITE_FONT_WEIGHT_ULTRA_BOLD = 800,

			/// <summary>
			/// Predefined font weight : Black (900).
			/// </summary>
			DWRITE_FONT_WEIGHT_BLACK = 900,

			/// <summary>
			/// Predefined font weight : Heavy (900).
			/// </summary>
			DWRITE_FONT_WEIGHT_HEAVY = 900,

			/// <summary>
			/// Predefined font weight : Extra-black (950).
			/// </summary>
			DWRITE_FONT_WEIGHT_EXTRA_BLACK = 950,

			/// <summary>
			/// Predefined font weight : Ultra-black (950).
			/// </summary>
			DWRITE_FONT_WEIGHT_ULTRA_BLACK = 950,

			NULL = -1,
		};

		/// <summary>
		/// The font stretch enumeration describes relative change from the normal aspect ratio
		/// as specified by a font designer for the glyphs in a font.
		/// Values less than 1 or greater than 9 are considered to be invalid, and they are rejected by font API functions.
		/// </summary>
		public enum DWRITE_FONT_STRETCH
		{
			/// <summary>
			/// Predefined font stretch : Not known (0).
			/// </summary>
			DWRITE_FONT_STRETCH_UNDEFINED = 0,

			/// <summary>
			/// Predefined font stretch : Ultra-condensed (1).
			/// </summary>
			DWRITE_FONT_STRETCH_ULTRA_CONDENSED = 1,

			/// <summary>
			/// Predefined font stretch : Extra-condensed (2).
			/// </summary>
			DWRITE_FONT_STRETCH_EXTRA_CONDENSED = 2,

			/// <summary>
			/// Predefined font stretch : Condensed (3).
			/// </summary>
			DWRITE_FONT_STRETCH_CONDENSED = 3,

			/// <summary>
			/// Predefined font stretch : Semi-condensed (4).
			/// </summary>
			DWRITE_FONT_STRETCH_SEMI_CONDENSED = 4,

			/// <summary>
			/// Predefined font stretch : Normal (5).
			/// </summary>
			DWRITE_FONT_STRETCH_NORMAL = 5,

			/// <summary>
			/// Predefined font stretch : Medium (5).
			/// </summary>
			DWRITE_FONT_STRETCH_MEDIUM = 5,

			/// <summary>
			/// Predefined font stretch : Semi-expanded (6).
			/// </summary>
			DWRITE_FONT_STRETCH_SEMI_EXPANDED = 6,

			/// <summary>
			/// Predefined font stretch : Expanded (7).
			/// </summary>
			DWRITE_FONT_STRETCH_EXPANDED = 7,

			/// <summary>
			/// Predefined font stretch : Extra-expanded (8).
			/// </summary>
			DWRITE_FONT_STRETCH_EXTRA_EXPANDED = 8,

			/// <summary>
			/// Predefined font stretch : Ultra-expanded (9).
			/// </summary>
			DWRITE_FONT_STRETCH_ULTRA_EXPANDED = 9,

			NULL = -1,
		};

		/// <summary>
		/// The font style enumeration describes the slope style of a font face, such as Normal, Italic or Oblique.
		/// Values other than the ones defined in the enumeration are considered to be invalid, and they are rejected by font API functions.
		/// </summary>
		public enum DWRITE_FONT_STYLE
		{
			/// <summary>
			/// Font slope style : Normal.
			/// </summary>
			DWRITE_FONT_STYLE_NORMAL,

			/// <summary>
			/// Font slope style : Oblique.
			/// </summary>
			DWRITE_FONT_STYLE_OBLIQUE,

			/// <summary>
			/// Font slope style : Italic.
			/// </summary>
			DWRITE_FONT_STYLE_ITALIC,

			NULL = -1,

		};

		/// <summary>
		/// Alignment of paragraph text along the reading direction axis relative to 
		/// the leading and trailing edge of the layout box.
		/// </summary>
		public enum DWRITE_TEXT_ALIGNMENT
		{
			/// <summary>
			/// The leading edge of the paragraph text is aligned to the layout box's leading edge.
			/// </summary>
			DWRITE_TEXT_ALIGNMENT_LEADING,

			/// <summary>
			/// The trailing edge of the paragraph text is aligned to the layout box's trailing edge.
			/// </summary>
			DWRITE_TEXT_ALIGNMENT_TRAILING,

			/// <summary>
			/// The center of the paragraph text is aligned to the center of the layout box.
			/// </summary>
			DWRITE_TEXT_ALIGNMENT_CENTER,

			/// <summary>
			/// Align text to the leading side, and also justify text to fill the lines.
			/// </summary>
			DWRITE_TEXT_ALIGNMENT_JUSTIFIED,

			NULL = -1,
		};

		/// <summary>
		/// Alignment of paragraph text along the flow direction axis relative to the
		/// flow's beginning and ending edge of the layout box.
		/// </summary>
		public enum DWRITE_PARAGRAPH_ALIGNMENT
		{
			/// <summary>
			/// The first line of paragraph is aligned to the flow's beginning edge of the layout box.
			/// </summary>
			DWRITE_PARAGRAPH_ALIGNMENT_NEAR,

			/// <summary>
			/// The last line of paragraph is aligned to the flow's ending edge of the layout box.
			/// </summary>
			DWRITE_PARAGRAPH_ALIGNMENT_FAR,

			/// <summary>
			/// The center of the paragraph is aligned to the center of the flow of the layout box.
			/// </summary>
			DWRITE_PARAGRAPH_ALIGNMENT_CENTER,

			NULL = -1,
		};

		/// <summary>
		/// Word wrapping in multiline paragraph.
		/// </summary>
		public enum DWRITE_WORD_WRAPPING
		{
			/// <summary>
			/// Words are broken across lines to avoid text overflowing the layout box.
			/// </summary>
			DWRITE_WORD_WRAPPING_WRAP = 0,

			/// <summary>
			/// Words are kept within the same line even when it overflows the layout box.
			/// This option is often used with scrolling to reveal overflow text. 
			/// </summary>
			DWRITE_WORD_WRAPPING_NO_WRAP = 1,

			/// <summary>
			/// Words are broken across lines to avoid text overflowing the layout box.
			/// Emergency wrapping occurs if the word is larger than the maximum width.
			/// </summary>
			DWRITE_WORD_WRAPPING_EMERGENCY_BREAK = 2,

			/// <summary>
			/// Only wrap whole words, never breaking words (emergency wrapping) when the
			/// layout width is too small for even a single word.
			/// </summary>
			DWRITE_WORD_WRAPPING_WHOLE_WORD = 3,

			/// <summary>
			/// Wrap between any valid characters clusters.
			/// </summary>
			DWRITE_WORD_WRAPPING_CHARACTER = 4,

			NULL = -1,
		};

		/// <summary>
		/// Direction for how reading progresses.
		/// </summary>
		public enum DWRITE_READING_DIRECTION
		{
			/// <summary>
			/// Reading progresses from left to right.
			/// </summary>
			DWRITE_READING_DIRECTION_LEFT_TO_RIGHT = 0,

			/// <summary>
			/// Reading progresses from right to left.
			/// </summary>
			DWRITE_READING_DIRECTION_RIGHT_TO_LEFT = 1,

			/// <summary>
			/// Reading progresses from top to bottom.
			/// </summary>
			DWRITE_READING_DIRECTION_TOP_TO_BOTTOM = 2,

			/// <summary>
			/// Reading progresses from bottom to top.
			/// </summary>
			DWRITE_READING_DIRECTION_BOTTOM_TO_TOP = 3,

			NULL = -1,
		};

		/// <summary>
		/// Direction for how lines of text are placed relative to one another.
		/// </summary>
		public enum DWRITE_FLOW_DIRECTION
		{
			/// <summary>
			/// Text lines are placed from top to bottom.
			/// </summary>
			DWRITE_FLOW_DIRECTION_TOP_TO_BOTTOM = 0,

			/// <summary>
			/// Text lines are placed from bottom to top.
			/// </summary>
			DWRITE_FLOW_DIRECTION_BOTTOM_TO_TOP = 1,

			/// <summary>
			/// Text lines are placed from left to right.
			/// </summary>
			DWRITE_FLOW_DIRECTION_LEFT_TO_RIGHT = 2,

			/// <summary>
			/// Text lines are placed from right to left.
			/// </summary>
			DWRITE_FLOW_DIRECTION_RIGHT_TO_LEFT = 3,

			NULL = -1,
		};

		/// <summary>
		/// The method used for line spacing in layout.
		/// </summary>
		public enum DWRITE_LINE_SPACING_METHOD
		{
			/// <summary>
			/// Line spacing depends solely on the content, growing to accommodate the size of fonts and inline objects.
			/// </summary>
			DWRITE_LINE_SPACING_METHOD_DEFAULT,

			/// <summary>
			/// Lines are explicitly set to uniform spacing, regardless of contained font sizes.
			/// This can be useful to avoid the uneven appearance that can occur from font fallback.
			/// </summary>
			DWRITE_LINE_SPACING_METHOD_UNIFORM,

			/// <summary>
			/// Line spacing and baseline distances are proportional to the computed values based on the content, the size of the fonts and inline objects.
			/// </summary>
			DWRITE_LINE_SPACING_METHOD_PROPORTIONAL,

			NULL = -1,
		};

		#endregion
	}
}
