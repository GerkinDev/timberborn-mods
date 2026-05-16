using GerkinDev.WatertightGates.Services;
using System;
using Timberborn.AutomationBuildingsUI;
using Timberborn.BaseComponentSystem;
using Timberborn.Localization;
using Timberborn.StatusSystem;

namespace GerkinDev.WatertightGates.Components.UI
{
	internal class WatertightGateConflictStatus : BaseComponent, IAwakableComponent, IStartableComponent
	{
		private readonly ILoc _loc;

		private WatertightGate _gate;

		private StatusToggle _statusToggle;

		public WatertightGateConflictStatus(ILoc loc)
		{
			_loc = loc;
		}

		private static string _ConflictLocKey => GateConflictStatus.ConflictLocKey;
		private static string _ConflictShortLocKey => GateConflictStatus.ConflictShortLocKey;

		public void Awake()
		{
			_gate = GetComponent<WatertightGate>();
			_statusToggle = StatusToggle.CreateNormalStatusWithAlertAndFloatingIcon("GateConflict",
				_loc.T(_ConflictLocKey), _loc.T(_ConflictShortLocKey));
			_gate.ConflictStateChanged += OnStateChanged;
		}

		public void Start() => GetComponent<StatusSubject>().RegisterStatus(_statusToggle);

		public void OnStateChanged(object sender, EventArgs e) =>
			_statusToggle.Toggle(_gate.CurrentGateState == EGateState.OpenConflict);
	}
}