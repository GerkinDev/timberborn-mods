using System;
using GerkinDev.PressurePlates.Services;

namespace GerkinDev.PressurePlates.Components.LogicModes
{
	public class CountLatch : IPressurePlateLogicMode
	{
		private int _count;
		private int _activationThreshold = 2;
		private bool _active;

		#region IPressurePlateEventHandler

		public void OnEnter(OccupantDetectorService.OccupancyEvent evt)
		{
			_count++;
			Active = _count >= _activationThreshold;
		}

		public void OnExit(OccupantDetectorService.OccupancyEvent evt)
		{
		}

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

		#endregion
	}
}