using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.Specs;
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

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components
{
	/// <summary>
	/// Extracted from <see cref="Timberborn.AutomationBuildings.GateNavMeshBlocker"/>
	/// </summary>
	internal class NavMeshBlocker : BaseComponent, IAwakableComponent, IDeletableEntity, IPathConnectionEnforcer
	{
		private readonly INavMeshService _navMeshService;
		private readonly NavMeshGroupService _navMeshGroupService;
		private readonly IPathService _pathService;
		private WatertightGate.EGateMode _gateMode;
		public WatertightGate.EGateMode GateMode
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
		public NavMeshBlocker(INavMeshService navMeshService, NavMeshGroupService navMeshGroupService, IPathService pathService)
		{
			_navMeshService = navMeshService;
			_navMeshGroupService = navMeshGroupService;
			_pathService = pathService;
		}

		#region IAwakableComponent
		private BuildingNavMesh _buildingNavMesh;
		private BlockObject _blockObject;
		private WatertightGateSpec _spec;

		public void Awake()
		{
			_spec = GetComponent<WatertightGateSpec>();
			_buildingNavMesh = GetComponent<BuildingNavMesh>();
			_blockObject = GetComponent<BlockObject>();
		}
		#endregion

		#region IDeletableEntity
		public void DeleteEntity()
		{
			GateMode = WatertightGate.EGateMode.Close;
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
			var pathStart = _blockObject.TransformCoordinates(_spec.PathStart);
			var pathCenter = _blockObject.TransformCoordinates(_spec.PathCenter);
			var pathEnd = _blockObject.TransformCoordinates(_spec.PathEnd);
			foreach (var (a, b) in new[] { (origin, target), (target, origin) })
			{
				if (_blockObject.IsIntersecting(Block.FullFrom(a)))
				{
					if (b != pathStart && b != pathEnd)
					{
						return false;
					}
					return (b == pathStart || b == pathEnd) && _pathService.IsPath(b);
				}
			}
			return false;
		}
		#endregion

		private void _SetPathBlockage(WatertightGate.EGateMode previousValue, WatertightGate.EGateMode value)
		{
			switch ((previousValue, value))
			{
				case (WatertightGate.EGateMode.Close, WatertightGate.EGateMode.Open or WatertightGate.EGateMode.Pass):
					_buildingNavMesh.UnblockAndAddToNavMesh();
					break;
				case (WatertightGate.EGateMode.Open or WatertightGate.EGateMode.Pass, WatertightGate.EGateMode.Close):
					_buildingNavMesh.BlockAndRemoveFromNavMesh();
					break;
			}
		}

		private void _SetTraverseCost(WatertightGate.EGateMode previousValue, WatertightGate.EGateMode value)
		{
			var start = _blockObject.TransformCoordinates(_spec.PathStart);
			var end = _blockObject.TransformCoordinates(_spec.PathEnd);
			var center = _blockObject.TransformCoordinates(_spec.PathCenter);
			var prevCost = _GetCost(previousValue);
			var cost = _GetCost(value);
			_navMeshService.RemoveEdge(_GetEdge(center, start, prevCost));
			_navMeshService.RemoveEdge(_GetEdge(center, end, prevCost));
			_navMeshService.AddEdge(_GetEdge(center, start, cost));
			_navMeshService.AddEdge(_GetEdge(center, end, cost));
		}

		private float _GetCost(WatertightGate.EGateMode gateMode) => gateMode switch
		{
			WatertightGate.EGateMode.Open => 1,
			WatertightGate.EGateMode.Close => WalkerLimits.BlockingEdgeCost,
			WatertightGate.EGateMode.Pass => WalkerLimits.BlockingEdgeCost,
			_ => throw new Exception($"Invalid mode {gateMode}"),
		};

		private NavMeshEdge _GetEdge(Vector3Int start, Vector3Int end, float cost)
		{
			return NavMeshEdge.CreateGrouped(start, end, _navMeshGroupService.GetDefaultGroupId(), isRoad: false, cost);
		}
	}
}
