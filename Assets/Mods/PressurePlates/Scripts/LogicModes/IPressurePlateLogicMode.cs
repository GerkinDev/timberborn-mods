using System;
using System.Text.Json.Nodes;
using GerkinDev.PressurePlates.Services;
using Timberborn.Automation;
using UnityEngine.UIElements;
using Version = Timberborn.Versioning.Version;

namespace GerkinDev.PressurePlates.LogicModes
{
	public interface IPressurePlateLogicModeUI
	{
		VisualElement Element { get; }
		IPressurePlateLogicModeUI ConnectToLogicMode(IPressurePlateLogicMode logicMode);
		void Reset();
	}

	public interface IPressurePlateLogicModeUI<TLogicMode> : IPressurePlateLogicModeUI
		where TLogicMode : IPressurePlateLogicMode
	{
		IPressurePlateLogicModeUI<TLogicMode> ConnectToLogicMode(TLogicMode logicMode);
	}

	public interface IPressurePlateLogicMode : ICombinationalTransmitter
	{
		bool Active { get; }
		void OnEnter(OccupantDetectorService.OccupancyEvent evt);

		void OnExit(OccupantDetectorService.OccupancyEvent evt);

		void Update();

		event EventHandler<bool>? ActiveChanged;

		#region Persistence

		JsonObject SerializeState();

		static IPressurePlateLogicMode LoadState(JsonObject state, Version? previousVersion) =>
			throw new NotImplementedException();


		void PostLoad();

		#endregion
	}
}