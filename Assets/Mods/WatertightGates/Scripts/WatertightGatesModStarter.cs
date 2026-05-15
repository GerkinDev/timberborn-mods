using Castle.Core.Internal;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Services;
using HarmonyLib;
using System;
using System.IO;
using System.Text.Json;
using Timberborn.ModManagerScene;
using UnityEngine;

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
		public const string MOD_ID = "GerkinDev.WatertightGates";
		public void StartMod(IModEnvironment modEnvironment)
		{
			Debug.Log($"[{MOD_ID}] Loading mod from {modEnvironment.ModPath} ({modEnvironment.OriginPath})");
			var json = File.ReadAllText(Path.Combine(modEnvironment.ModPath, "manifest.json"));
			var modInfo = JsonSerializer.Deserialize<ModInfo>(json);
			var modInteropVersion = Path.GetFileName(modEnvironment.ModPath).Split('-')[1];
			Debug.Log($"[{MOD_ID}] Mod version: {modInfo.Version}, loading build for game version {modInteropVersion}, actual {Timberborn.Versioning.GameVersions.CurrentVersion.Full}");
			
			Debug.Log($"[{MOD_ID}] Checking patches conflicts");
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

			Debug.Log($"[{MOD_ID}] Patching");
			new Harmony(MOD_ID).PatchAll();
		}

	}
}
