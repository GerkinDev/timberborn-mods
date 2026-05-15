using Castle.Core.Internal;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Services;
using HarmonyLib;
using System;
using System.IO;
using System.Text.Json;
using Timberborn.ModManagerScene;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts
{
	internal class ModInfo {
		public string Name { get; init; } 
		public string Version { get; init; } 
		public string Id { get; init; } 
		public string MinimumGameVersion { get; init; } 
		public string Description { get; init; } 
	}
	// ReSharper disable once ClassNeverInstantiated.Global -- Injected
	public class WatertightGatesModStarter : IModStarter
	{
		public void StartMod(IModEnvironment modEnvironment)
		{
			var json = File.ReadAllText(Path.Combine(modEnvironment.ModPath, "manifest.json"));
			var modInfo = JsonSerializer.Deserialize<ModInfo>(json);
			var modInteropVersion = Path.GetFileName(modEnvironment.ModPath).Split('-')[1];
			WatertightGates.Log(
				format: "Mod version: {0}, loading build for game version {1}, actual {2}",
				modInfo.Version,
				modInteropVersion,
				Timberborn.Versioning.GameVersions.CurrentVersion.Full
			);
			
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
