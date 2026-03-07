using Timberborn.WaterSystem;
using UnityEngine;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts
{
	/// <summary>
	/// Extracted from <see cref="Timberborn.WaterObjects.WaterObstacle"/>
	/// </summary>
	internal partial class DamServiceGate
	{
		private readonly IWaterService _waterObstacle_waterService;
		private bool _waterObstacle_wasAdded;
		private float _waterObstacle_height;


		private void _WaterObstacle_AddToWaterService(float height)
		{
			if (_waterObstacle_wasAdded || !_blockObject.AddedToService)
			{
				return;
			}
			if (height is > 1f or < 0f)
			{
				Debug.LogFormat("Height {0} should be within [0, 1]");
				// Clamp in range
				height = Mathf.Clamp(height, 0f, 1f);
			}

			Vector3Int coordinates;
			foreach (var blocking in _spec.WaterBlockingPositions)
			{
				coordinates = _blockObject.TransformCoordinates(blocking);
				_waterObstacle_waterService.AddFullObstacle(coordinates);
			}

			_waterObstacle_height = height;
			coordinates = _blockObject.TransformCoordinates(_spec.WaterDynamicPosition);
			if (height < 1f)
			{
				_waterObstacle_waterService.UpdateInflowLimiter(coordinates, height);
			}
			else
			{
				_waterObstacle_waterService.AddFullObstacle(coordinates);
			}

			_waterObstacle_wasAdded = true;
		}

		private void _WaterObstacle_RemoveFromWaterService()
		{
			if (!_waterObstacle_wasAdded)
			{
				return;
			}

			Vector3Int coordinates;
			foreach (var blocking in _spec.WaterBlockingPositions)
			{
				coordinates = _blockObject.TransformCoordinates(blocking);
				_waterObstacle_waterService.RemoveFullObstacle(coordinates);
			}

			coordinates = _blockObject.TransformCoordinates(_spec.WaterDynamicPosition);
			if (_waterObstacle_height < 1f)
			{
				_waterObstacle_waterService.RemoveInflowLimiter(coordinates);
			}
			else
			{
				_waterObstacle_waterService.RemoveFullObstacle(coordinates);
			}

			_waterObstacle_wasAdded = false;
		}
		private void _WaterObstacle_SetObstacleHeight(float effectiveHeight)
		{
			_WaterObstacle_RemoveFromWaterService();
			_WaterObstacle_AddToWaterService(effectiveHeight);
		}
	}
}
