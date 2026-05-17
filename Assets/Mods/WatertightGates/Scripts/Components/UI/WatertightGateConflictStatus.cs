using System;
using GerkinDev.WatertightGates.Services;
using Timberborn.AutomationBuildingsUI;
using Timberborn.BaseComponentSystem;
using Timberborn.Localization;
using Timberborn.StatusSystem;

namespace GerkinDev.WatertightGates.Components.UI
{
	internal class WatertightGateConflictStatus : BaseComponent, IAwakableComponent, IStartableComponent
	{
		private readonly ILoc _loc;

		public WatertightGateConflictStatus(ILoc loc)
		{
			_loc = loc;
		}

		private static string _ConflictLocKey => GateConflictStatus.ConflictLocKey;
		private static string _ConflictShortLocKey => GateConflictStatus.ConflictShortLocKey;

		#region IStartableComponent

		public void Start() => GetComponent<StatusSubject>().RegisterStatus(_statusToggle);

		#endregion

		private void _OnStateChanged(object sender, EventArgs e) =>
			_statusToggle.Toggle(_gate.CurrentGateState == EGateState.OpenConflict);

		#region IAwakableComponent

		private WatertightGate _gate = null!;
		private StatusToggle _statusToggle = null!;

		public void Awake()
		{
			_gate = GetComponent<WatertightGate>();
			_statusToggle = StatusToggle.CreateNormalStatusWithAlertAndFloatingIcon("GateConflict",
				_loc.T(_ConflictLocKey), _loc.T(_ConflictShortLocKey));
			_gate.ConflictStateChanged += _OnStateChanged;
		}

		#endregion
	}
}