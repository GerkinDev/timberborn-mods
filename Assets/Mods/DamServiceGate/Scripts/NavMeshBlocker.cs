using GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts.Extensions;
using System;
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
		private DamServiceGate.EGateMode _gateMode;
		public DamServiceGate.EGateMode GateMode
		{
			get => _gateMode; set
			{
				if (value == _gateMode)
				{
					return;
				}
				_SetPathBlockage(previousValue: _gateMode, value: value);
				_SetTraverseCost(previousValue: _gateMode, value: value);
				_gateMode = value;
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
			GateMode = DamServiceGate.EGateMode.Close;
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

		private void _SetPathBlockage(DamServiceGate.EGateMode previousValue, DamServiceGate.EGateMode value)
		{
			switch ((previousValue, value))
			{
				case (DamServiceGate.EGateMode.Close, DamServiceGate.EGateMode.Open or DamServiceGate.EGateMode.Pass):
					_buildingNavMesh.UnblockAndAddToNavMesh();
					break;
				case (DamServiceGate.EGateMode.Open or DamServiceGate.EGateMode.Pass, DamServiceGate.EGateMode.Close):
					_buildingNavMesh.BlockAndRemoveFromNavMesh();
					break;
			}
		}

		private void _SetTraverseCost(DamServiceGate.EGateMode previousValue, DamServiceGate.EGateMode value)
		{
			var start = _blockObject.TransformCoordinates(_spec.PathStart);
			var end = _blockObject.TransformCoordinates(_spec.PathEnd);
			var center = _blockObject.TransformCoordinates(_spec.PathCenter);
			var prevCost = _GetCost(previousValue);
			var cost = _GetCost(value);
			this.Log("Updating cost from {0} to {1}", prevCost, cost);
			_navMeshService.RemoveEdge(_GetEdge(center, start, prevCost));
			_navMeshService.RemoveEdge(_GetEdge(center, end, prevCost));
			_navMeshService.AddEdge(_GetEdge(center, start, cost));
			_navMeshService.AddEdge(_GetEdge(center, end, cost));
		}

		private float _GetCost(DamServiceGate.EGateMode gateMode) => gateMode switch
		{
			DamServiceGate.EGateMode.Open => 1,
			DamServiceGate.EGateMode.Close => WalkerLimits.BlockingEdgeCost,
			DamServiceGate.EGateMode.Pass => WalkerLimits.BlockingEdgeCost,
			_ => throw new Exception($"Invalid mode {gateMode}"),
		};

		private NavMeshEdge _GetEdge(Vector3Int start, Vector3Int end, float cost)
		{
			return NavMeshEdge.CreateGrouped(start, end, _navMeshGroupService.GetDefaultGroupId(), isRoad: false, cost);
		}
	}
}
