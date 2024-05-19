using System;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering;
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
		/// <param name="volume"></param>
		/// <returns></returns>
		/// <exception cref="MissingFieldException"></exception>
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
	}
}
