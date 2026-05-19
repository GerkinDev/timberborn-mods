using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using GerkinDev.PressurePlates.Services;
using Timberborn.Automation;
using Version = Timberborn.Versioning.Version;

namespace GerkinDev.PressurePlates.LogicModes
{
	public partial class CountLatch : IPressurePlateLogicMode, IDisposable, ICombinationalTransmitter
	{
		private int _activationThreshold = 1;
		private int _count;
		private bool _prevResetTriggerState;

		public CountLatch(Automator owner, AutomatorRegistry registry)
		{
			ResetTrigger = new(owner, registry);
			ResetTrigger.Changed += automator => _prevResetTriggerState = ResetTrigger.BooleanState;
		}

		public int Count
		{
			get => _count;
			private set
			{
				_count = value;
				Update();
			}
		}

		public int ActivationThreshold
		{
			get => _activationThreshold;
			set
			{
				_activationThreshold = value;
				Update();
			}
		}

		public AutomatorInputRef ResetTrigger { get; }

		public void Dispose()
		{
			PressurePlates.Log("Disposed CountLatch");
			ResetTrigger.Dispose();
		}

		/// <summary>
		///     Reset the count only if the reset trigger just passed to active.
		/// </summary>
		public void Evaluate()
		{
			if (_prevResetTriggerState == ResetTrigger.BooleanState)
			{
				return;
			}

			_prevResetTriggerState = ResetTrigger.BooleanState;
			if (_prevResetTriggerState)
			{
				ResetCount();
			}
		}

		public void ResetCount() => Count = 0;

		#region IPressurePlateEventHandler

		public void OnEnter(OccupantDetectorService.OccupancyEvent evt)
		{
			Count++;
			Update();
		}

		public void OnExit(OccupantDetectorService.OccupancyEvent evt)
		{
		}

		public void Update() => Active = Count >= ActivationThreshold;

		public event EventHandler<bool>? ActiveChanged;

		private bool _active;

		public bool Active
		{
			get => _active;
			private set
			{
				if (value == _active)
				{
					return;
				}

				_active = value;
				ActiveChanged?.Invoke(this, _active);
			}
		}

		public JsonObject SerializeState()
		{
			var obj = new JsonObject
			{
				[nameof(Count)] = Count,
				[nameof(ActivationThreshold)] = ActivationThreshold,
				[nameof(ResetTrigger)] = ResetTrigger.AutomatorId,
				[nameof(_prevResetTriggerState)] = _prevResetTriggerState
			};
			return obj;
		}

		public static CountLatch Load(Automator owner, JsonObject state, Version? previousVersion,
			AutomatorRegistry registry)
		{
			var latch = new CountLatch(owner, registry)
			{
				ActivationThreshold = state[nameof(ActivationThreshold)]?.GetValue<int>() ?? 0,
				Count = state[nameof(Count)]?.GetValue<int>() ?? 0,
				_prevResetTriggerState = state[nameof(_prevResetTriggerState)]?.GetValue<bool>() ?? false
			};
			latch.ResetTrigger.AutomatorId = state.TryGetPropertyValue(nameof(ResetTrigger), out var node)
				? node is JsonValue nodeValue && nodeValue.GetValueKind() != JsonValueKind.Null
					? nodeValue.GetValue<Guid>()
					: null
				: null;
			return latch;
		}

		public void PostLoad() => ResetTrigger.Load();

		#endregion
	}
}