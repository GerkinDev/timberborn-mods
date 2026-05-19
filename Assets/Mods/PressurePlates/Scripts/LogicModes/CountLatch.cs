using System;
using System.Text.Json.Nodes;
using GerkinDev.PressurePlates.Services;
using Version = Timberborn.Versioning.Version;

namespace GerkinDev.PressurePlates.Components.LogicModes
{
	public partial class CountLatch : IPressurePlateLogicMode
	{
		private int _activationThreshold = 2;
		private bool _active;
		private int _count;

		#region IPressurePlateEventHandler

		public void OnEnter(OccupantDetectorService.OccupancyEvent evt)
		{
			_count++;
			Update();
		}

		public void OnExit(OccupantDetectorService.OccupancyEvent evt)
		{
		}

		public void Update() => Active = _count >= _activationThreshold;

		public event EventHandler<bool>? ActiveChanged;

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
				[nameof(_count)] = _count,
				[nameof(_activationThreshold)] = _activationThreshold,
				[nameof(_active)] = _active
			};
			return obj;
		}

		public static CountLatch Load(JsonObject state, Version? previousVersion)
		{
			var latch = new CountLatch
			{
				_active = state[nameof(_active)]?.GetValue<bool>() ?? false,
				_activationThreshold = state[nameof(_activationThreshold)]?.GetValue<int>() ?? 0,
				_count = state[nameof(_count)]?.GetValue<int>() ?? 0
			};
			return latch;
		}

		#endregion
	}
}