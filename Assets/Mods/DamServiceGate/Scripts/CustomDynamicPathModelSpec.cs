using Timberborn.BlueprintSystem;
using UnityEngine;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts
{
	public record CustomDynamicPathModelSpec : ComponentSpec
	{
		[Serialize]
		public string GroundModelPrefix { get; init; }

		[Serialize]
		public string RoofModelPrefix { get; init; }

		[Serialize]
		public Vector3Int Position { get; init; }
	}
}
