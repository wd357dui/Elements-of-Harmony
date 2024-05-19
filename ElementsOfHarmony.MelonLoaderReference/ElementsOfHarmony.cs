using MelonLoader;

[assembly: MelonInfo(typeof(ElementsOfHarmony.MelonLoaderReference.ElementsOfHarmony),
	name: "Elements of Harmony",
	version: "0.3.0",
	author: "wd357dui")]
[assembly: MelonGame("Melbot Studios", "MLP")]
[assembly: MelonGame("DrakharStudio", "MyLittlePonyZephyrHeights")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]
[assembly: MelonOptionalDependencies(
	"ElementsOfHarmony.AMBA",
	"ElementsOfHarmony.AZHM",
	"ElementsOfHarmony.KinectControl",
	"ElementsOfHarmony.KinectControl.AMBA",
	"ElementsOfHarmony.KinectControl.AZHM")]

namespace ElementsOfHarmony.MelonLoaderReference
{
    public class ElementsOfHarmony : MelonMod
    {
		public override void OnLateInitializeMelon()
		{
			global::ElementsOfHarmony.ElementsOfHarmony.Exist();
		}
	}
}
