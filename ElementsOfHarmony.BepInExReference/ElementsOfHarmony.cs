using BepInEx;
using System;
using System.IO;
using System.Reflection;

namespace ElementsOfHarmony.BepInExReference
{
	[BepInPlugin(GUID: "wd357dui.ElementsOfHarmony", Name: "Elements of Harmony", Version: "0.3.3.0")]
	public class ElementsOfHarmony : BaseUnityPlugin
	{
		public void Awake()
		{
			Assembly Program = Assembly.LoadFile(
				Path.Combine(Environment.CurrentDirectory, "Elements of Harmony/Managed/ElementsOfHarmony.dll"));
			Program.GetType("ElementsOfHarmony.ElementsOfHarmony")
				.GetMethod("Exist")
				.Invoke(null, Array.Empty<object>());
		}
	}
}
