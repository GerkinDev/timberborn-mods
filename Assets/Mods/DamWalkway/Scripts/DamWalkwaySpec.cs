using Timberborn.BlueprintSystem;
using UnityEngine;

namespace GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts
{
	/// <seealso href="https://github.com/mechanistry/timberborn-modding/wiki/Timberborn-architecture#specs"/>
	internal record DamWalkwaySpec : ComponentSpec
	{
		[Serialize] public string Anchor { get; set; }
		[Serialize] public GateTransformSpec OpenTransform { get; init; }
		[Serialize] public GateTransformSpec CloseTransform { get; init; }
	}

	public record GateTransformSpec
	{
		[Serialize] public Vector3 Position { get; init; }
		[Serialize] public Vector3 Rotation { get; init; }
	}
}
