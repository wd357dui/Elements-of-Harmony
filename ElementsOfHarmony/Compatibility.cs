using System;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace ElementsOfHarmony
{
	/// <summary>
	/// solves API difference problem between Unity 2020 and Unity 2022 and possibly beyond
	/// </summary>
	public static class Compatibility
	{
		/// <summary>
		/// bool UnityEngine.Rendering.Volume.isGlobal
		/// </summary>
		public static bool IsGlobal(this Volume volume, bool? setValue = null)
		{
			if (IsGlobalField != null) goto field_found;
			else if (volume.GetType().GetRuntimeField("isGlobal") is FieldInfo isGlobal)
			{
				IsGlobalField = isGlobal;
			}
			else if (volume.GetType().GetRuntimeField("m_IsGlobal") is FieldInfo m_IsGlobal)
			{
				IsGlobalField = m_IsGlobal;
			}
			else if (volume.GetType().GetRuntimeFields()
				.FirstOrDefault(F => F.GetCustomAttributes<FormerlySerializedAsAttribute>()
				.Any(A => A.oldName == "isGlobal" || A.oldName == "m_IsGlobal"))
				is FieldInfo isGlobal_new)
			{
				IsGlobalField = isGlobal_new;
			}
			else throw new MissingFieldException("cannot find field `bool isGlobal` or any of its equivalents");

			field_found:
			if (setValue is bool newValue)
			{
				IsGlobalField.SetValue(volume, setValue);
			}
			return (bool)IsGlobalField.GetValue(volume);
		}
		internal static FieldInfo? IsGlobalField;

		public static VolumeComponent NewTonemapping(this VolumeProfile profile)
		{
			Type BloomType = Type.GetType("UnityEngine.Rendering.Universal.Tonemapping, Unity.RenderPipelines.Universal.Runtime");
			return profile.Add(BloomType);
		}
		
		public static VolumeComponent NewBloom(this VolumeProfile profile)
		{
			Type BloomType = Type.GetType("UnityEngine.Rendering.Universal.Bloom, Unity.RenderPipelines.Universal.Runtime");
			return profile.Add(BloomType);
		}

#pragma warning disable IDE1006 // Naming convention
		public static TonemappingModeParameter? mode(this VolumeComponent TonemappingComponent)
		{
			return TonemappingComponent.GetType().GetRuntimeField("mode")?.GetValue(TonemappingComponent) as TonemappingModeParameter;
		}
		/// <summary>
		/// public enum NeutralRangeReductionMode
		/// {
		/// 	Reinhard = 1,
		/// 	BT2390
		/// }
		/// </summary>
		public static VolumeParameter? neutralHDRRangeReductionMode(this VolumeComponent TonemappingComponent)
		{
			return TonemappingComponent.GetType().GetRuntimeField("neutralHDRRangeReductionMode")?.GetValue(TonemappingComponent) as VolumeParameter;
		}
		/// <summary>
		/// public enum HDRACESPreset
		/// {
		/// 	ACES1000Nits = 3,
		/// 	ACES2000Nits,
		/// 	ACES4000Nits
		/// }
		/// </summary>
		public static VolumeParameter? acesPreset(this VolumeComponent TonemappingComponent)
		{
			return TonemappingComponent.GetType().GetRuntimeField("acesPreset")?.GetValue(TonemappingComponent) as VolumeParameter;
		}
		public static ClampedFloatParameter? hueShiftAmount(this VolumeComponent TonemappingComponent)
		{
			return TonemappingComponent.GetType().GetRuntimeField("hueShiftAmount")?.GetValue(TonemappingComponent) as ClampedFloatParameter;
		}
		public static BoolParameter? detectPaperWhite(this VolumeComponent TonemappingComponent)
		{
			return TonemappingComponent.GetType().GetRuntimeField("detectPaperWhite")?.GetValue(TonemappingComponent) as BoolParameter;
		}
		public static ClampedFloatParameter? paperWhite(this VolumeComponent TonemappingComponent)
		{
			return TonemappingComponent.GetType().GetRuntimeField("paperWhite")?.GetValue(TonemappingComponent) as ClampedFloatParameter;
		}
		public static BoolParameter? detectBrightnessLimits(this VolumeComponent TonemappingComponent)
		{
			return TonemappingComponent.GetType().GetRuntimeField("detectBrightnessLimits")?.GetValue(TonemappingComponent) as BoolParameter;
		}
		public static ClampedFloatParameter? minNits(this VolumeComponent TonemappingComponent)
		{
			return TonemappingComponent.GetType().GetRuntimeField("minNits")?.GetValue(TonemappingComponent) as ClampedFloatParameter;
		}
		public static ClampedFloatParameter? maxNits(this VolumeComponent TonemappingComponent)
		{
			return TonemappingComponent.GetType().GetRuntimeField("maxNits")?.GetValue(TonemappingComponent) as ClampedFloatParameter;
		}


		public static ClampedIntParameter? skipIterations(this VolumeComponent BloomComponent)
		{
			return BloomComponent.GetType().GetRuntimeField("skipIterations")?.GetValue(BloomComponent) as ClampedIntParameter;
		}
		public static MinFloatParameter? threshold(this VolumeComponent BloomComponent)
		{
			return BloomComponent.GetType().GetRuntimeField("threshold")?.GetValue(BloomComponent) as MinFloatParameter;
		}
		public static MinFloatParameter? intensity(this VolumeComponent BloomComponent)
		{
			return BloomComponent.GetType().GetRuntimeField("intensity")?.GetValue(BloomComponent) as MinFloatParameter;
		}
		public static ClampedFloatParameter? scatter(this VolumeComponent BloomComponent)
		{
			return BloomComponent.GetType().GetRuntimeField("scatter")?.GetValue(BloomComponent) as ClampedFloatParameter;
		}
		public static MinFloatParameter? clamp(this VolumeComponent BloomComponent)
		{
			return BloomComponent.GetType().GetRuntimeField("clamp")?.GetValue(BloomComponent) as MinFloatParameter;
		}
		public static ColorParameter? tint(this VolumeComponent BloomComponent)
		{
			return BloomComponent.GetType().GetRuntimeField("tint")?.GetValue(BloomComponent) as ColorParameter;
		}
		public static BoolParameter? highQualityFiltering(this VolumeComponent BloomComponent)
		{
			return BloomComponent.GetType().GetRuntimeField("highQualityFiltering")?.GetValue(BloomComponent) as BoolParameter;
		}
		/// <summary>
		/// public enum BloomDownscaleMode
		/// {
		///		Half,
		///		Quarter
		/// }
		/// </summary>
		public static VolumeParameter? downscale(this VolumeComponent BloomComponent)
		{
			return BloomComponent.GetType().GetRuntimeField("downscale")?.GetValue(BloomComponent) as VolumeParameter;
		}
		public static ClampedIntParameter? maxIterations(this VolumeComponent BloomComponent)
		{
			return BloomComponent.GetType().GetRuntimeField("maxIterations")?.GetValue(BloomComponent) as ClampedIntParameter;
		}
		public static TextureParameter? dirtTexture(this VolumeComponent BloomComponent)
		{
			return BloomComponent.GetType().GetRuntimeField("dirtTexture")?.GetValue(BloomComponent) as TextureParameter;
		}
		public static MinFloatParameter? dirtIntensity(this VolumeComponent BloomComponent)
		{
			return BloomComponent.GetType().GetRuntimeField("dirtIntensity")?.GetValue(BloomComponent) as MinFloatParameter;
		}
#pragma warning restore IDE1006 // 命名样式
	}
}
