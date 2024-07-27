using MelonLoader;
using System.Reflection;
using System;
using System.IO;

[assembly: MelonInfo(typeof(ElementsOfHarmony.MelonLoaderReference.ElementsOfHarmony),
	name: "Elements of Harmony",
	version: "0.3.3.0",
	author: "wd357dui")]
[assembly: MelonGame("Melbot Studios", "MLP")]
[assembly: MelonGame("DrakharStudio", "MyLittlePonyZephyrHeights")]
[assembly: MelonPlatform(MelonPlatformAttribute.CompatiblePlatforms.WINDOWS_X64)]

namespace ElementsOfHarmony.MelonLoaderReference
{
    public class ElementsOfHarmony : MelonMod
    {
		public override void OnLateInitializeMelon()
		{
			Assembly Program = Assembly.LoadFile(
				Path.Combine(Environment.CurrentDirectory, "Elements of Harmony/Managed/ElementsOfHarmony.dll"));
			Program.GetType("ElementsOfHarmony.ElementsOfHarmony")
				.GetMethod("Exist")
				.Invoke(null, Array.Empty<object>());
		}
	}
}
