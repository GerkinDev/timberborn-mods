using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.Specs;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Extensions;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Utils;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.WaterSystem;
using UnityEngine;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components
{
	/// <summary>
	/// Extracted from <see cref="Timberborn.WaterObjects.WaterObstacle"/>
	/// </summary>
	internal class WaterBlocker : BaseComponent, IAwakableComponent, IFinishedStateListener
	{
		private readonly IWaterService _waterService;
		private CommitableState<float> _height;
		public float Height
		{
			get => _height.Value;
			set
			{
				_height.DesiredValue = value;
				_UpdateState();
			}
		}


		public WaterBlocker(IWaterService waterService)
		{
			_waterService = waterService;
		}

		#region IAwakableComponent
		private WatertightGateSpec _spec;
		private BlockObject _blockObject;

		public void Awake()
		{
			_spec = GetComponent<WatertightGateSpec>();
			_blockObject = GetComponent<BlockObject>();
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
			_UpdateState();
		}

		public void OnExitFinishedState()
		{
			foreach (var blocking in _spec.WaterBlockingPositions)
			{
				var coordinates = _blockObject.TransformCoordinates(blocking);
				_waterService.RemoveFullObstacle(coordinates);
			}
			Height = 0;
		}
		#endregion
		private void _UpdateState()
		{
			if (_height.DesiredValue is > 1f or < 0f)
			{
				this.Warn("Height {0} should be within [0, 1]");
				// Clamp in range
				_height.DesiredValue = Mathf.Clamp(_height.DesiredValue, 0f, 1f);
			}
			if (!_blockObject.IsFinished || !_height.HasChange)
			{
				return;
			}
			var coordinates = _blockObject.TransformCoordinates(_spec.WaterDynamicPosition);
			switch (_height.Value)
			{
				case 1f:
					_waterService.RemoveFullObstacle(coordinates);
					break;
				case > 0f when _height.DesiredValue == 0:
					_waterService.RemoveInflowLimiter(coordinates);
					break;
				default:
					break;
			}
			switch (_height.DesiredValue)
			{
				case 1f:
					_waterService.AddFullObstacle(coordinates);
					break;
				case > 0f:
					_waterService.UpdateInflowLimiter(coordinates, _height.DesiredValue);
					break;
				default:
					break;
			}
			_height.Commit();
		}
	}
}
