using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.WaterSystem;
using UnityEngine;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts
{
	/// <summary>
	/// Extracted from <see cref="Timberborn.WaterObjects.WaterObstacle"/>
	/// </summary>
	internal class WaterBlocker : BaseComponent, IAwakableComponent, IDeletableEntity, IFinishedStateListener
	{
		private readonly IWaterService _waterService;
		private float _height;
		public float Height
		{
			get => _height;
			set
			{
				_SetInFlow(value);
				_height = value;
			}
		}

		public WaterBlocker(IWaterService waterService)
		{
			_waterService = waterService;
		}

		#region IAwakableComponent
		private DamServiceGateSpec _spec;
		private BlockObject _blockObject;

		public void Awake()
		{
			_spec = GetComponent<DamServiceGateSpec>();
			_blockObject = GetComponent<BlockObject>();
		}
		#endregion

		#region IDeletableEntity
		public void DeleteEntity()
		{
			foreach (var blocking in _spec.WaterBlockingPositions)
			{
				var coordinates = _blockObject.TransformCoordinates(blocking);
				_waterService.RemoveFullObstacle(coordinates);
			}
			Height = 0;
		}
		#endregion

		#region IFinishedStateListener
		public void OnEnterFinishedState()
		{
			foreach (var blocking in _spec.WaterBlockingPositions)
			{
				var coordinates = _blockObject.TransformCoordinates(blocking);
				_waterService.AddFullObstacle(coordinates);
			}
			Height = 0;
		}

		public void OnExitFinishedState()
		{
		}
		#endregion

		private void _SetInFlow(float height)
		{
			if (height is > 1f or < 0f)
			{
				Debug.LogFormat("Height {0} should be within [0, 1]");
				// Clamp in range
				height = Mathf.Clamp(height, 0f, 1f);
			}
			if (height == _height)
			{
				return;
			}
			var coordinates = _blockObject.TransformCoordinates(_spec.WaterDynamicPosition);
			switch (_height)
			{
				case 1f:
					_waterService.RemoveFullObstacle(coordinates);
					Debug.LogFormat("Remove old obstacle");
					break;
				case > 0f when height == 0:
					_waterService.RemoveInflowLimiter(coordinates);
					Debug.LogFormat("Remove old limiter");
					break;
				default:
					break;
			}
			switch (height)
			{
				case 1f:
					_waterService.AddFullObstacle(coordinates);
					Debug.LogFormat("Add obstacle");
					break;
				case > 0f:
					_waterService.UpdateInflowLimiter(coordinates, height);
					Debug.LogFormat("Set limiter");
					break;
				default:
					break;
			}
		}
	}
}
