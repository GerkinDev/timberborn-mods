using HarmonyLib;
using Timberborn.ModManagerScene;
using UnityEngine;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts
{
	public class DamServiceGateModStarter : IModStarter
	{
		public void StartMod(IModEnvironment modEnvironment)
		{
			Debug.Log("Patching");
			new Harmony(nameof(DamServiceGateModStarter)).PatchAll();
		}
	}
}
