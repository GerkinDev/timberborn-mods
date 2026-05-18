using System;
using System.Text.Json.Nodes;
using GerkinDev.PressurePlates.Services;
using Version = Timberborn.Versioning.Version;

namespace GerkinDev.PressurePlates.Components.LogicModes
{
	public interface IPressurePlateLogicMode
	{
		bool Active { get; }
		void OnEnter(OccupantDetectorService.OccupancyEvent evt);

		void OnExit(OccupantDetectorService.OccupancyEvent evt);

		event EventHandler<bool>? ActiveChanged;

		#region Persistence

		JsonObject SerializeState();

		static IPressurePlateLogicMode LoadState(JsonObject state, Version? previousVersion) =>
			throw new NotImplementedException();

		#endregion
	}
}