using static ElementsOfHarmony.Settings.DirectXHook;

namespace ElementsOfHarmony.AZHM
{
	public class DirectXHook
	{
		public static void Init()
		{
			URP.ColorAdjustments.PostExposure ??= 0.5f;
			Settings.WriteOurSettings();
		}
	}
}
