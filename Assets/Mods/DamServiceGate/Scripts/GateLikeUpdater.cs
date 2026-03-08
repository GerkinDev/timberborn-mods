using HarmonyLib;
using System.Collections.Generic;
using Timberborn.AutomationBuildings;
using Timberborn.Navigation;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts
{
	public interface IGateLike
	{
		public bool IsClosed { get; }
		public Vector3Int PathStart { get; }
		public Vector3Int PathEnd { get; }
		public Vector3Int PathCenter { get; }

		public void Close();
		public void Open();
		public void EnableConflict();
		public void DisableConflict();
	}


	[HarmonyPatch(typeof(GateUpdater), nameof(GateUpdater.LateUpdateSingleton))]
	public static class ConstructionSiteBuildTimeCalculatorPatch
	{
		/// <summary>
		/// Full copy of <see cref="GateUpdater.LateUpdateSingleton"/> except it does not flush <see cref="GateUpdater._openGateCrossings"/>: we'll flush it in <see cref="GateLikeUpdater.LateUpdateSingleton"/>.
		/// </summary>
		public static bool Prefix(GateUpdater __instance)
		{
			if (__instance._hasScheduledGates)
			{
				__instance.CloseScheduledGates();
				__instance.OpenScheduledGates();
				__instance._hasScheduledUnblocking = true;
				__instance._hasScheduledGates = false;
			}

			if (__instance._hasScheduledUnblocking)
			{
				__instance.TryOpenConflictedGates();
				__instance._hasScheduledUnblocking = false;
			}
			return false;
		}
	}

	/// <summary>
	/// Reimplementation/extension of <see cref="GateUpdater"/>
	/// </summary>
	public class GateLikeUpdater : IUpdatableSingleton, ILateUpdatableSingleton, ISingletonNavMeshListener
	{
		private readonly GateConflictDetector _gateConflictDetector;
		private readonly GateUpdater _baseGameGateUpdater;
		private readonly HashSet<IGateLike> _gatesScheduledToOpen = new();
		private readonly HashSet<IGateLike> _gatesScheduledToClose = new();
		private readonly HashSet<IGateLike> _gatesWithConflict = new();
		private readonly List<IGateLike> _gatesWithConflictCache = new();
		private Dictionary<Vector3Int, Vector3Int> _OpenGateCrossings => _baseGameGateUpdater._openGateCrossings;
		private bool _hasScheduledGates;
		private bool _hasScheduledUnblocking;

		public GateLikeUpdater(GateConflictDetector gateConflictDetector, GateUpdater baseGameGateUpdater)
		{
			_gateConflictDetector = gateConflictDetector;
			_baseGameGateUpdater = baseGameGateUpdater;
		}

		#region IUpdatableSingleton
		public void UpdateSingleton()
		{
			if (_baseGameGateUpdater._openGateCrossings.Count > 0)
				Debug.LogFormat("[UpdateSingleton] Base updater has {0} members", _baseGameGateUpdater._openGateCrossings.Count);
		}
		#endregion

		#region ILateUpdatableSingleton
		public void LateUpdateSingleton()
		{
			if (_OpenGateCrossings.Count > 0)
			{
				Debug.LogFormat("[LateUpdateSingleton] Base updater has {0} members", _OpenGateCrossings.Count);
				foreach (var kvp in _OpenGateCrossings)
				{
					Debug.LogFormat("[LateUpdateSingleton] {0} => {1}", kvp.Key, kvp.Value);
				}
			}
			if (_hasScheduledGates)
			{
				_CloseScheduledGates();
				_OpenScheduledGates();
				_hasScheduledUnblocking = true;
				_hasScheduledGates = false;
			}

			if (_hasScheduledUnblocking)
			{
				_TryOpenConflictedGates();
				_hasScheduledUnblocking = false;
			}

			_OpenGateCrossings.Clear();
		}
		#endregion

		#region ISingletonNavMeshListener
		public void OnNavMeshUpdated(NavMeshUpdate navMeshUpdate)
		{
			_hasScheduledUnblocking = true;
		}
		#endregion

		public void ScheduleToOpen(IGateLike gate)
		{
			_gatesScheduledToOpen.Add(gate);
			_gatesScheduledToClose.Remove(gate);
			_hasScheduledGates = true;
		}

		public void ScheduleToClose(IGateLike gate)
		{
			_gatesScheduledToClose.Add(gate);
			_gatesScheduledToOpen.Remove(gate);
			_hasScheduledGates = true;
		}

		public void Remove(IGateLike gate)
		{
			_gatesScheduledToClose.Remove(gate);
			_gatesScheduledToOpen.Remove(gate);
			_RemoveGateFromConflicted(gate);
		}

		private void _CloseScheduledGates()
		{
			foreach (var item in _gatesScheduledToClose)
			{
				_TryCloseGate(item);
			}

			_gatesScheduledToClose.Clear();
		}

		private void _OpenScheduledGates()
		{
			foreach (var item in _gatesScheduledToOpen)
			{
				_TryOpenGate(item);
			}

			_gatesScheduledToOpen.Clear();
		}

		private void _TryCloseGate(IGateLike gate)
		{
			if (!gate.IsClosed)
			{
				gate.Close();
			}

			_RemoveGateFromConflicted(gate);
		}

		private void _TryOpenGate(IGateLike gate)
		{
			if (!gate.IsClosed)
			{
				return;
			}

			Debug.LogFormat("[LateUpdateSingleton] Check if can open {0} => {1} => {2}", gate.PathStart, gate.PathCenter, gate.PathEnd);
			if (_gateConflictDetector.CanOpenGateWithoutConflict(gate.PathStart, gate.PathEnd, gate.PathCenter, _OpenGateCrossings))
			{
				Debug.LogFormat("[LateUpdateSingleton] Yes");
				gate.Open();
				_RemoveGateFromConflicted(gate);
				_AddToOpenGateCrossings(gate);
				return;
			}
			Debug.LogFormat("[LateUpdateSingleton] No");

			if (!gate.IsClosed)
			{
				gate.Close();
			}

			_AddGateToConflicted(gate);
		}

		private void _AddGateToConflicted(IGateLike gate)
		{
			gate.EnableConflict();
			_gatesWithConflict.Add(gate);
		}

		private void _RemoveGateFromConflicted(IGateLike gate)
		{
			gate.DisableConflict();
			_gatesWithConflict.Remove(gate);
		}

		private void _TryOpenConflictedGates()
		{
			if (_gatesWithConflict.Count <= 0)
			{
				return;
			}

			_gatesWithConflictCache.AddRange(_gatesWithConflict);
			_gatesWithConflict.Clear();
			foreach (var item in _gatesWithConflictCache)
			{
				_TryOpenGate(item);
			}

			_gatesWithConflictCache.Clear();
		}

		private void _AddToOpenGateCrossings(GatePlacement gatePlacement)
		{
			_AddToOpenGateCrossings(gatePlacement.Start, gatePlacement.End);
		}
		private void _AddToOpenGateCrossings(IGateLike gate)
		{
			_AddToOpenGateCrossings(gate.PathStart, gate.PathEnd);
		}
		private void _AddToOpenGateCrossings(Vector3Int start, Vector3Int end)
		{
			_OpenGateCrossings[start] = end;
			_OpenGateCrossings[end] = start;
		}
	}
}
