using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.Specs;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Utils;
using System;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.BuildingsNavigation;
using Timberborn.Coordinates;
using Timberborn.Navigation;
using Timberborn.PathSystem;
using Timberborn.WalkingSystem;
using UnityEngine;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components
{
	/// <summary>
	/// Extracted from <see cref="Timberborn.AutomationBuildings.GateNavMeshBlocker"/>
	/// </summary>
	internal class NavMeshBlocker : BaseComponent, IAwakableComponent, IFinishedStateListener, IPathConnectionEnforcer
	{
		private readonly INavMeshService _navMeshService;
		private readonly NavMeshGroupService _navMeshGroupService;
		private readonly IPathService _pathService;
		private CommitableState<bool> _pathBlocked;
		private CommitableState<WatertightGate.EGateMode> _traverseCost;
		private WatertightGate.EGateMode _gateMode;
		public WatertightGate.EGateMode GateMode
		{
			get => _gateMode; set
			{
				if (value == _gateMode)
				{
					return;
				}
				_pathBlocked.DesiredValue = value == WatertightGate.EGateMode.Close;
				_traverseCost.DesiredValue = value;
				_gateMode = value;
				_UpdateState();
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

		#region IFinishedStateListener
		public void OnEnterFinishedState()
		{
			_UpdateState();
		}
		public void OnExitFinishedState()
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

		private void _UpdateState()
		{
			_UpdateTraverseCost();
			_UpdatePathBlockage();
		}

		private void _UpdatePathBlockage()
		{
			if (!_blockObject.IsFinished || !_pathBlocked.HasChange)
			{
				return;
			}
			if (_pathBlocked.DesiredValue)
			{
				_buildingNavMesh.BlockAndRemoveFromNavMesh();
			}
			else
			{
				_buildingNavMesh.UnblockAndAddToNavMesh();
			}
			_pathBlocked.Commit();
		}

		private void _UpdateTraverseCost()
		{
			if (!_blockObject.IsFinished || !_traverseCost.HasChange)
			{
				return;
			}
			var start = _blockObject.TransformCoordinates(_spec.PathStart);
			var end = _blockObject.TransformCoordinates(_spec.PathEnd);
			var center = _blockObject.TransformCoordinates(_spec.PathCenter);
			var prevCost = _GetCost(_traverseCost.Value);
			var cost = _GetCost(_traverseCost.DesiredValue);
			_navMeshService.RemoveEdge(_GetEdge(center, start, prevCost));
			_navMeshService.RemoveEdge(_GetEdge(center, end, prevCost));
			_navMeshService.AddEdge(_GetEdge(center, start, cost));
			_navMeshService.AddEdge(_GetEdge(center, end, cost));
			_traverseCost.Commit();
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
