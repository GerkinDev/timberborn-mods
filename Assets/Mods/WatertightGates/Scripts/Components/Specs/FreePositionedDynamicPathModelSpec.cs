using Timberborn.BlueprintSystem;
using UnityEngine;

namespace GerkinDev.WatertightGates.Components.Specs
{
	public record FreePositionedDynamicPathModelSpec : ComponentSpec
	{
		[Serialize] public string GroundModelPrefix { get; init; } = null!;
		[Serialize] public string RoofModelPrefix { get; init; } = null!;
		[Serialize] public Vector3Int Position { get; init; }
	}
}