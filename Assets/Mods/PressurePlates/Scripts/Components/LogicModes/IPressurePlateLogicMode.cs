using System;
using System.Text.Json.Nodes;
using GerkinDev.PressurePlates.Services;
using Timberborn.EntityPanelSystem;
using Timberborn.SingletonSystem;
using UnityEngine.UIElements;
using Version = Timberborn.Versioning.Version;

namespace GerkinDev.PressurePlates.Components.LogicModes
{
	public interface IPressurePlateLogicModeUI: ILoadableSingleton
	{
		VisualElement Element { get; }
		IPressurePlateLogicModeUI ConnectToLogicMode(IPressurePlateLogicMode logicMode);
		void Reset();
		void UpdateFragment(){}
		void InitializeFragment(){}
	}
	public interface IPressurePlateLogicModeUI<TLogicMode>: IPressurePlateLogicModeUI where TLogicMode : IPressurePlateLogicMode
	{
		IPressurePlateLogicModeUI<TLogicMode> ConnectToLogicMode(TLogicMode logicMode);
	}
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