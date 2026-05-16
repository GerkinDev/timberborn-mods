using HarmonyLib;
using System.Collections.Generic;
using Timberborn.AutomationBuildings;
using Timberborn.Navigation;
using Timberborn.SingletonSystem;
using UnityEngine;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Services
{
	public enum EGateState
	{
		Open,
		Closed,
		OpenConflict
	}

	public interface IGateLike
	{
		EGateState CurrentGateState { get; set; }
		Vector3Int PathStart { get; }
		Vector3Int PathEnd { get; }
		Vector3Int PathCenter { get; }
	}


	[HarmonyPatch(typeof(GateUpdater), nameof(GateUpdater.LateUpdateSingleton))]
	public static class GateUpdaterPatch
	{
		/// <summary>
		///     Full copy of <see cref="GateUpdater.LateUpdateSingleton" /> except it does not flush
		///     <see cref="GateUpdater._openGateCrossings" />: we'll flush it in <see cref="GateLikeUpdater.LateUpdateSingleton" />
		///     .
		/// </summary>
		// ReSharper disable once InconsistentNaming
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
	///     Reimplementation/extension of <see cref="GateUpdater" />
	/// </summary>
	public class GateLikeUpdater : IUpdatableSingleton, ILateUpdatableSingleton, ISingletonNavMeshListener
	{
		private readonly GateUpdater _baseGameGateUpdater;
		private readonly Dictionary<IGateLike, GateLikeUpdate> _gateChangeOperations = new();
		private readonly GateConflictDetector _gateConflictDetector;
		private readonly Dictionary<IGateLike, GateLikeUpdate> _gatesWithConflict = new();
		private readonly Dictionary<IGateLike, GateLikeUpdate> _gatesWithConflictCache = new();
		private bool _hasScheduledGates;
		private bool _hasScheduledUnblocking;

		public GateLikeUpdater(GateConflictDetector gateConflictDetector, GateUpdater baseGameGateUpdater)
		{
			_gateConflictDetector = gateConflictDetector;
			_baseGameGateUpdater = baseGameGateUpdater;
		}

		private Dictionary<Vector3Int, Vector3Int> _OpenGateCrossings => _baseGameGateUpdater._openGateCrossings;

		#region ILateUpdatableSingleton

		public void LateUpdateSingleton()
		{
			if (_OpenGateCrossings.Count > 0)
			{
				Debug.LogFormat("[LateUpdateSingleton] Base updater has {0} members", _OpenGateCrossings.Count);
				foreach (KeyValuePair<Vector3Int, Vector3Int> kvp in _OpenGateCrossings)
				{
					Debug.LogFormat("[LateUpdateSingleton] {0} => {1}", kvp.Key, kvp.Value);
				}
			}

			if (_hasScheduledGates)
			{
				foreach (KeyValuePair<IGateLike, GateLikeUpdate> kvp in _gateChangeOperations)
				{
					_TryUpdateGate(kvp.Key, kvp.Value);
				}

				_gateChangeOperations.Clear();
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
			Debug.LogFormat("[OnNavMeshUpdated] NavMesh updated");
			_hasScheduledUnblocking = true;
		}

		#endregion

		#region IUpdatableSingleton

		public void UpdateSingleton()
		{
			if (_baseGameGateUpdater._openGateCrossings.Count > 0)
			{
				Debug.LogFormat("[UpdateSingleton] Base updater has {0} members",
					_baseGameGateUpdater._openGateCrossings.Count);
			}
		}

		#endregion


		public void ScheduleGateUpdate(IGateLike gate, EGateState desired, bool force = false)
		{
			_gateChangeOperations[gate] =
				new() { CurrentState = gate.CurrentGateState, DesiredState = desired, Force = force };
			_gatesWithConflict.Remove(gate);
			_hasScheduledGates = true;
		}

		public void ScheduleToOpen(IGateLike gate) => ScheduleGateUpdate(gate, EGateState.Open);

		public void ScheduleToClose(IGateLike gate) => ScheduleGateUpdate(gate, EGateState.Closed);

		public void Remove(IGateLike gate)
		{
			_gateChangeOperations.Remove(gate);
			_gatesWithConflict.Remove(gate);
		}

		private EGateState _TryUpdateGate(IGateLike gate, GateLikeUpdate update) =>
			update.DesiredState == EGateState.Closed ? _TryCloseGate(gate, update) : _TryOpenGate(gate, update);

		private EGateState _TryCloseGate(IGateLike gate, GateLikeUpdate update)
		{
			if (update.CurrentState != EGateState.Closed || update.Force)
			{
				gate.CurrentGateState = EGateState.Closed;
				_gatesWithConflict.Remove(gate);
				return EGateState.Closed;
			}

			return EGateState.Open;
		}

		private EGateState _TryOpenGate(IGateLike gate, GateLikeUpdate update)
		{
			if ((update.CurrentState == EGateState.Open && update.Force) ||
				_gateConflictDetector.CanOpenGateWithoutConflict(gate.PathStart, gate.PathEnd, gate.PathCenter,
					_OpenGateCrossings))
			{
				gate.CurrentGateState = EGateState.Open;
				_gatesWithConflict.Remove(gate);
				_AddToOpenGateCrossings(gate);
				return EGateState.Open;
			}

			gate.CurrentGateState = EGateState.OpenConflict;
			_gatesWithConflict[gate] = update;
			return EGateState.OpenConflict;
		}

		private void _TryOpenConflictedGates()
		{
			if (_gatesWithConflict.Count <= 0)
			{
				return;
			}

			foreach ((IGateLike? gate, GateLikeUpdate? update) in _gatesWithConflict)
			{
				_gatesWithConflictCache.Add(gate, update);
			}

			_gatesWithConflict.Clear();
			foreach ((IGateLike? gate, GateLikeUpdate? update) in _gatesWithConflictCache)
			{
				EGateState result = _TryUpdateGate(gate, update);
			}

			_gatesWithConflictCache.Clear();
		}

		private void _AddToOpenGateCrossings(GatePlacement gatePlacement) =>
			_AddToOpenGateCrossings(gatePlacement.Start, gatePlacement.End);

		private void _AddToOpenGateCrossings(IGateLike gate) => _AddToOpenGateCrossings(gate.PathStart, gate.PathEnd);

		private void _AddToOpenGateCrossings(Vector3Int start, Vector3Int end)
		{
			_OpenGateCrossings[start] = end;
			_OpenGateCrossings[end] = start;
		}

		private record GateLikeUpdate
		{
			public EGateState CurrentState { get; init; }
			public EGateState DesiredState { get; init; }
			public bool Force { get; init; }
		}
	}
}