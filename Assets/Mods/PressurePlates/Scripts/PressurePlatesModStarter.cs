using System;
using System.IO;
using System.Text.Json;
using Timberborn.ModManagerScene;
using Timberborn.Versioning;

namespace GerkinDev.PressurePlates
{
	public class ModInfo
	{
		public string Name { get; init; } = null!;
		public string Version { get; init; } = null!;
		public string Id { get; init; } = null!;
		public string MinimumGameVersion { get; init; } = null!;
		public string Description { get; init; } = null!;
	}

	// ReSharper disable once ClassNeverInstantiated.Global -- Injected
	public class PressurePlatesModStarter : IModStarter
	{
		private static ModInfo _modInfo;

		public static ModInfo ModInfo => _modInfo ?? throw new NullReferenceException(nameof(_modInfo));

		public void StartMod(IModEnvironment modEnvironment)
		{
			var json = File.ReadAllText(Path.Combine(modEnvironment.ModPath, "manifest.json"));
			var modInfo = JsonSerializer.Deserialize<ModInfo>(json) ??
				throw new ApplicationException("Could not load mod info");
			var modInteropVersion = Path.GetFileName(modEnvironment.ModPath).Split('-')[1];
			PressurePlates.Log(
				format: "Mod version: {0}, loading build for game version {1}, actual {2}",
				modInfo.Version,
				modInteropVersion,
				GameVersions.CurrentVersion.Full
			);
			_modInfo = modInfo;
		}
	}
}