using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.BuildingsNavigation;
using Timberborn.Coordinates;
using Timberborn.EntitySystem;
using Timberborn.Navigation;
using Timberborn.PathSystem;
using Timberborn.WalkingSystem;
using UnityEngine;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts
{
	/// <summary>
	/// Extracted from <see cref="Timberborn.AutomationBuildings.GateNavMeshBlocker"/>
	/// </summary>
	internal class NavMeshBlocker : BaseComponent, IAwakableComponent, IDeletableEntity, IPathConnectionEnforcer
	{
		private readonly INavMeshService _navMeshService;
		private readonly NavMeshGroupService _navMeshGroupService;
		private readonly IPathService _pathService;
		private bool _navMeshBlocked;
		public bool NavMeshBlocked
		{
			get => _navMeshBlocked; set
			{
				if (value == _navMeshBlocked)
				{
					return;
				}

				_SetPathBlockage(isBlocked: value);
				_SetTraverseCost(isExpensive: value);
				_navMeshBlocked = value;
			}
		}
		private bool _expensiveTraverseCostSet;
		public NavMeshBlocker(INavMeshService navMeshService, NavMeshGroupService navMeshGroupService, IPathService pathService)
		{
			_navMeshService = navMeshService;
			_navMeshGroupService = navMeshGroupService;
			_pathService = pathService;
		}

		#region IAwakableComponent
		private BuildingNavMesh _buildingNavMesh;
		private BlockObject _blockObject;
		private DamServiceGateSpec _spec;

		public void Awake()
		{
			_spec = GetComponent<DamServiceGateSpec>();
			_buildingNavMesh = GetComponent<BuildingNavMesh>();
			_blockObject = GetComponent<BlockObject>();
		}
		#endregion

		#region IDeletableEntity
		public void DeleteEntity()
		{
			NavMeshBlocked = false;
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
				return _pathService.IsPath(target);
			}
			if (_blockObject.IsIntersecting(Block.FullFrom(target)))
			{
				return _pathService.IsPath(origin);
			}
			return false;
		}
		#endregion

		private void _SetPathBlockage(bool isBlocked)
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

		private void _SetTraverseCost(bool isExpensive)
		{
			if (isExpensive != _expensiveTraverseCostSet)
			{
				var start = _blockObject.TransformCoordinates(_spec.PathStart);
				var end = _blockObject.TransformCoordinates(_spec.PathEnd);
				var center = _blockObject.TransformCoordinates(_spec.PathCenter);
				if (isExpensive)
				{
					_navMeshService.RemoveEdge(_GetNormalEdge(center, start));
					_navMeshService.RemoveEdge(_GetNormalEdge(center, end));
					_navMeshService.AddEdge(_GetExpensiveEdge(center, start));
					_navMeshService.AddEdge(_GetExpensiveEdge(center, end));
				}
				else
				{
					_navMeshService.AddEdge(_GetNormalEdge(center, start));
					_navMeshService.AddEdge(_GetNormalEdge(center, end));
					_navMeshService.RemoveEdge(_GetExpensiveEdge(center, start));
					_navMeshService.RemoveEdge(_GetExpensiveEdge(center, end));
				}

				_expensiveTraverseCostSet = isExpensive;
			}
		}

		private NavMeshEdge _GetNormalEdge(Vector3Int start, Vector3Int end)
		{
			return _GetEdge(start, end, 1f);
		}

		private NavMeshEdge _GetExpensiveEdge(Vector3Int start, Vector3Int end)
		{
			return _GetEdge(start, end, WalkerLimits.BlockingEdgeCost);
		}

		private NavMeshEdge _GetEdge(Vector3Int start, Vector3Int end, float cost)
		{
			return NavMeshEdge.CreateGrouped(start, end, _navMeshGroupService.GetDefaultGroupId(), isRoad: false, cost);
		}
	}
}
