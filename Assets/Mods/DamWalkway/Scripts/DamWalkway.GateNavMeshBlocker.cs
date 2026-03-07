using Timberborn.BlockSystem;
using Timberborn.BuildingsNavigation;
using Timberborn.Coordinates;
using Timberborn.Navigation;
using Timberborn.PathSystem;
using Timberborn.WalkingSystem;
using UnityEngine;

namespace GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts
{
	/// <summary>
	/// Extracted from <see cref="Timberborn.AutomationBuildings.GateNavMeshBlocker"/>
	/// </summary>
	internal partial class DamWalkway : IPathConnectionEnforcer
	{
		private readonly INavMeshService _gateNavMeshBlocker_navMeshService;
		private readonly NavMeshGroupService _gateNavMeshBlocker_navMeshGroupService;
		private readonly IPathService _gateNavMeshBlocker_pathService;
		public bool Gate_NavMeshBlocked { get; private set; }
		private bool _gateNavMeshBlocker_expensiveTraverseCostSet;

		#region IAwakableComponent
		private BuildingNavMesh _buildingNavMesh;
		private void _GateNavMeshBlocker_Awake()
		{
			_buildingNavMesh = GetComponent<BuildingNavMesh>();
		}
		#endregion

		#region IDeletableEntity
		private void _GateNavMeshBlocker_DeleteEntity()
		{
			if (Gate_NavMeshBlocked)
			{
				_GateNavMeshBlocker_Unblock();
			}
		}
		#endregion

		#region IPathConnectionEnforcer
		public bool CanConnectPath(Vector3Int origin, Vector3Int target)
		{
			// Verify if the path is on the correct sides (aligned with passage)
			var direction = origin - target;
			if (direction != _blockObject.TransformDirection(Direction2D.Down).ToOffset() && direction != _blockObject.TransformDirection(Direction2D.Up).ToOffset())
			{
				return false;
			}
			// Check if there is some path at positions
			if (_blockObject.IsIntersecting(Block.FullFrom(origin)))
			{
				return _gateNavMeshBlocker_pathService.IsPath(target);
			}
			if (_blockObject.IsIntersecting(Block.FullFrom(target)))
			{
				return _gateNavMeshBlocker_pathService.IsPath(origin);
			}
			return false;
		}
		#endregion

		private void _GateNavMeshBlocker_Block()
		{
			_GateNavMeshBlocker_SetPathBlockage(isBlocked: true);
			_GateNavMeshBlocker_SetTraverseCost(isExpensive: true);
			Gate_NavMeshBlocked = true;
		}

		private void _GateNavMeshBlocker_Unblock()
		{
			_GateNavMeshBlocker_SetPathBlockage(isBlocked: false);
			_GateNavMeshBlocker_SetTraverseCost(isExpensive: false);
			Gate_NavMeshBlocked = false;
		}

		private void _GateNavMeshBlocker_SetPathBlockage(bool isBlocked)
		{
			if (isBlocked)
			{
				_buildingNavMesh.BlockAndRemoveFromNavMesh();
			}
			else
			{
				_buildingNavMesh.UnblockAndAddToNavMesh();
			}
		}

		private void _GateNavMeshBlocker_SetTraverseCost(bool isExpensive)
		{
			if (isExpensive != _gateNavMeshBlocker_expensiveTraverseCostSet)
			{
				var start = _blockObject.TransformCoordinates(_spec.PathStart);
				var end = _blockObject.TransformCoordinates(_spec.PathEnd);
				var center = _blockObject.TransformCoordinates(_spec.PathCenter);
				if (isExpensive)
				{
					_gateNavMeshBlocker_navMeshService.RemoveEdge(_GateNavMeshBlocker_GetNormalEdge(center, start));
					_gateNavMeshBlocker_navMeshService.RemoveEdge(_GateNavMeshBlocker_GetNormalEdge(center, end));
					_gateNavMeshBlocker_navMeshService.AddEdge(_GateNavMeshBlocker_GetExpensiveEdge(center, start));
					_gateNavMeshBlocker_navMeshService.AddEdge(_GateNavMeshBlocker_GetExpensiveEdge(center, end));
				}
				else
				{
					_gateNavMeshBlocker_navMeshService.AddEdge(_GateNavMeshBlocker_GetNormalEdge(center, start));
					_gateNavMeshBlocker_navMeshService.AddEdge(_GateNavMeshBlocker_GetNormalEdge(center, end));
					_gateNavMeshBlocker_navMeshService.RemoveEdge(_GateNavMeshBlocker_GetExpensiveEdge(center, start));
					_gateNavMeshBlocker_navMeshService.RemoveEdge(_GateNavMeshBlocker_GetExpensiveEdge(center, end));
				}

				_gateNavMeshBlocker_expensiveTraverseCostSet = isExpensive;
			}
		}

		private NavMeshEdge _GateNavMeshBlocker_GetNormalEdge(Vector3Int start, Vector3Int end)
		{
			return _GateNavMeshBlocker_GetEdge(start, end, 1f);
		}

		private NavMeshEdge _GateNavMeshBlocker_GetExpensiveEdge(Vector3Int start, Vector3Int end)
		{
			return _GateNavMeshBlocker_GetEdge(start, end, WalkerLimits.BlockingEdgeCost);
		}

		private NavMeshEdge _GateNavMeshBlocker_GetEdge(Vector3Int start, Vector3Int end, float cost)
		{
			return NavMeshEdge.CreateGrouped(start, end, _gateNavMeshBlocker_navMeshGroupService.GetDefaultGroupId(), isRoad: false, cost);
		}
	}
}
