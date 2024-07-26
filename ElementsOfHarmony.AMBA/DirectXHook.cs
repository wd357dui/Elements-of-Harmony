using UnityEngine.Rendering.Universal;

namespace ElementsOfHarmony.AMBA
{
	public class DirectXHook
	{
		public static void Init()
		{
			Log.Message("filling in default UberPost parameters (Tonemapping, ColorAdjustments, Bloom, Vignette)");

			Settings.DirectXHook.URP.Tonemapping.Active ??= true;
			Settings.DirectXHook.URP.Tonemapping.Mode.Value ??= TonemappingMode.ACES;
			Settings.DirectXHook.URP.Tonemapping.Mode.Override ??= true;

			Settings.DirectXHook.URP.ColorAdjustments.Active ??= true;
			Settings.DirectXHook.URP.ColorAdjustments.PostExposure.Value ??= 0.5f;
			Settings.DirectXHook.URP.ColorAdjustments.PostExposure.Override ??= true;
			Settings.DirectXHook.URP.ColorAdjustments.Contrast.Value ??= 15f;
			Settings.DirectXHook.URP.ColorAdjustments.Contrast.Override ??= true;
			Settings.DirectXHook.URP.ColorAdjustments.ColorFilter.Value ??= "1, 0.9777778f, 0.9f, 1";
			Settings.DirectXHook.URP.ColorAdjustments.ColorFilter.Override ??= true;
			Settings.DirectXHook.URP.ColorAdjustments.HueShift.Value ??= 0f;
			Settings.DirectXHook.URP.ColorAdjustments.HueShift.Override ??= false;
			Settings.DirectXHook.URP.ColorAdjustments.Saturation.Value ??= -15f;
			Settings.DirectXHook.URP.ColorAdjustments.Saturation.Override ??= true;

			Settings.DirectXHook.URP.Bloom.Active ??= true;
			Settings.DirectXHook.URP.Bloom.Threshold.Value ??= 1.15f;
			Settings.DirectXHook.URP.Bloom.Threshold.Override ??= true;
			Settings.DirectXHook.URP.Bloom.Intensity.Value ??= 0.5f;
			Settings.DirectXHook.URP.Bloom.Intensity.Override ??= true;
			Settings.DirectXHook.URP.Bloom.Scatter.Value ??= 0.7f;
			Settings.DirectXHook.URP.Bloom.Scatter.Override ??= false;
			Settings.DirectXHook.URP.Bloom.Clamp.Value ??= 96f;
			Settings.DirectXHook.URP.Bloom.Clamp.Override ??= true;
			Settings.DirectXHook.URP.Bloom.Tint.Value ??= "1, 0.866838f, 0.596f, 1";
			Settings.DirectXHook.URP.Bloom.Tint.Override ??= true;
			Settings.DirectXHook.URP.Bloom.HighQualityFiltering.Value ??= false;
			Settings.DirectXHook.URP.Bloom.HighQualityFiltering.Override ??= false;
			Settings.DirectXHook.URP.Bloom.SkipIterations.Value ??= 1;
			Settings.DirectXHook.URP.Bloom.SkipIterations.Override ??= false;

			Settings.DirectXHook.URP.Vignette.Active ??= true;
			Settings.DirectXHook.URP.Vignette.Color.Value ??= "0.826415, 0.6792453, 0.9056604, 1";
			Settings.DirectXHook.URP.Vignette.Color.Override ??= true;
			Settings.DirectXHook.URP.Vignette.Center.Value ??= "0.5, 0.5";
			Settings.DirectXHook.URP.Vignette.Center.Override ??= false;
			Settings.DirectXHook.URP.Vignette.Intensity.Value ??= 0.2f;
			Settings.DirectXHook.URP.Vignette.Intensity.Override ??= true;
			Settings.DirectXHook.URP.Vignette.Smoothness.Value ??= 1f;
			Settings.DirectXHook.URP.Vignette.Smoothness.Override ??= true;
			Settings.DirectXHook.URP.Vignette.Rounded.Value ??= false;
			Settings.DirectXHook.URP.Vignette.Rounded.Override ??= false;
		}
	}
}
