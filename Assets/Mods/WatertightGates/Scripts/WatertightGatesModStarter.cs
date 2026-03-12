using HarmonyLib;
using Timberborn.ModManagerScene;
using UnityEngine;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts
{
	public class WatertightGatesModStarter : IModStarter
	{
		public void StartMod(IModEnvironment modEnvironment)
		{
			Debug.Log("Patching");
			new Harmony(nameof(WatertightGatesModStarter)).PatchAll();
		}
	}
}
