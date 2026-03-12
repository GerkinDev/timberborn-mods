using Timberborn.BlueprintSystem;
using UnityEngine;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts
{
	public record FreePositionedDynamicPathModelSpec : ComponentSpec
	{
		[Serialize]
		public string GroundModelPrefix { get; init; }

		[Serialize]
		public string RoofModelPrefix { get; init; }

		[Serialize]
		public Vector3Int Position { get; init; }
	}
}
