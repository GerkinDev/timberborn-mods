using System;
using System.Text.Json.Nodes;
using GerkinDev.PressurePlates.Services;
using UnityEngine.UIElements;
using Version = Timberborn.Versioning.Version;

namespace GerkinDev.PressurePlates.Components.LogicModes
{
	public interface IPressurePlateLogicModeUI
	{
		VisualElement Element { get; }
		IPressurePlateLogicModeUI ConnectToLogicMode(IPressurePlateLogicMode logicMode);
		void Reset();

		void UpdateFragment()
		{
		}

		void InitializeFragment()
		{
		}
	}

	public interface IPressurePlateLogicModeUI<TLogicMode> : IPressurePlateLogicModeUI
		where TLogicMode : IPressurePlateLogicMode
	{
		IPressurePlateLogicModeUI<TLogicMode> ConnectToLogicMode(TLogicMode logicMode);
	}

	public interface IPressurePlateLogicMode
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

		#endregion
	}
}