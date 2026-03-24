using Castle.Core.Internal;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Services;
using HarmonyLib;
using System;
using Timberborn.ModManagerScene;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts
{
	public class WatertightGatesModStarter : IModStarter
	{
		public void StartMod(IModEnvironment modEnvironment)
		{
			WatertightGates.Log("Checking patches conflicts");
			var patch = typeof(GateUpdaterPatch).GetAttribute<HarmonyPatch>();
			// get the MethodBase of the original
			var original = patch.info.declaringType.GetMethod(patch.info.methodName);
			// retrieve all patches
			var patches = Harmony.GetPatchInfo(original);
			if (patches is not null)
			{
				var patchers = string.Join(", ", patches.Owners);
				throw new Exception($"Another mod patched {original.DeclaringType.Name}#{original.Name}: {patchers}. To avoid issues, it is considered as a conflict. Please contact the devs for a resolution.");
			}

			WatertightGates.Log("Patching");
			new Harmony(WatertightGates.MOD_ID).PatchAll();
		}

	}
}
