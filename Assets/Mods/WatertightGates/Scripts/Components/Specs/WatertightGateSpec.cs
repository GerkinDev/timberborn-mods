using System.Collections.Immutable;
using Timberborn.BlueprintSystem;
using UnityEngine;

namespace GerkinDev.WatertightGates.Components.Specs
{
	/// <seealso href="https://github.com/mechanistry/timberborn-modding/wiki/Timberborn-architecture#specs"/>
	internal record WatertightGateSpec : ComponentSpec
	{
		[Serialize] public string Anchor { get; set; }
		[Serialize] public GateTransformSpec OpenTransform { get; init; }
		[Serialize] public GateTransformSpec CloseTransform { get; init; }
		[Serialize] public float OpenTime { get; init; }
		[Serialize] public float CloseTime { get; init; }
		[Serialize] public Vector3Int PathStart { get; init; }
		[Serialize] public Vector3Int PathCenter { get; init; }
		[Serialize] public Vector3Int PathEnd { get; init; }
		[Serialize] public ImmutableArray<Vector3Int> WaterBlockingPositions { get; init; }
		[Serialize] public Vector3Int WaterDynamicPosition { get; init; }
	}

	public record GateTransformSpec
	{
		[Serialize] public Vector3 Position { get; init; }
		[Serialize] public Vector3 Rotation { get; init; }
	}
}
