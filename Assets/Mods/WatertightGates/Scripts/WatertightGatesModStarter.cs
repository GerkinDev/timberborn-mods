using Castle.Core.Internal;
using GerkinDev.WatertightGates.Services;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Timberborn.ModManagerScene;
using Timberborn.Versioning;

namespace GerkinDev.WatertightGates
{
	internal class ModInfo
	{
		public string Name { get; init; } = null!;
		public string Version { get; init; } = null!;
		public string Id { get; init; } = null!;
		public string MinimumGameVersion { get; init; } = null!;
		public string Description { get; init; } = null!;
	}

	// ReSharper disable once ClassNeverInstantiated.Global -- Injected
	public class WatertightGatesModStarter : IModStarter
	{
		public void StartMod(IModEnvironment modEnvironment)
		{
			string json = File.ReadAllText(Path.Combine(modEnvironment.ModPath, "manifest.json"));
			ModInfo modInfo = JsonSerializer.Deserialize<ModInfo>(json) ??
				throw new ApplicationException("Could not load mod info");
			string? modInteropVersion = Path.GetFileName(modEnvironment.ModPath).Split('-')[1];
			WatertightGates.Log(
				format: "Mod version: {0}, loading build for game version {1}, actual {2}",
				modInfo.Version,
				modInteropVersion,
				GameVersions.CurrentVersion.Full
			);

			WatertightGates.Log("Checking patches conflicts");
			HarmonyPatch? patch = typeof(GateUpdaterPatch).GetAttribute<HarmonyPatch>();
			// get the MethodBase of the original
			MethodInfo? original = patch.info.declaringType.GetMethod(patch.info.methodName);
			if (original is null)
			{
				throw new ApplicationException("Unable to patch, missing method");
			}

			// retrieve all patches
			Patches? patches = Harmony.GetPatchInfo(original);
			if (patches is not null)
			{
				string patchers = string.Join(", ", patches.Owners);
				throw new ApplicationException(
					$"Another mod patched {original.DeclaringType!.Name}#{original.Name}: {patchers}. To avoid issues, it is considered as a conflict. Please contact the devs for a resolution."
				);
			}

			WatertightGates.Log("Patching");
			new Harmony(WatertightGates.MOD_ID).PatchAll();
		}
	}
}