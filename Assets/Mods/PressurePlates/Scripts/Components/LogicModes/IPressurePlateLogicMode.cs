using System;
using GerkinDev.PressurePlates.Services;

namespace GerkinDev.PressurePlates.Components.LogicModes
{
	public interface IPressurePlateLogicMode
	{
		void OnEnter(OccupantDetectorService.OccupancyEvent evt);

		void OnExit(OccupantDetectorService.OccupancyEvent evt);

		bool Active { get; }

		event EventHandler<bool>? ActiveChanged;
	}
}