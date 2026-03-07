using Timberborn.BlueprintSystem;
using UnityEngine;

namespace GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts
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
