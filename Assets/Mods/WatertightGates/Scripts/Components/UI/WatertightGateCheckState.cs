using System;
using Timberborn.BaseComponentSystem;
using Timberborn.EntitySystem;
using Timberborn.Localization;
using Timberborn.StatusSystem;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.UI
{
	internal class WatertightGateCheckState : BaseComponent, IAwakableComponent, IStartableComponent,
		IPostLoadableEntity
	{
		private readonly ILoc _loc;

		public WatertightGateCheckState(ILoc loc)
		{
			_loc = loc;
		}

		public void PostLoadEntity() => _statusToggle.Toggle(_gate.StateNeedCheck);

		public void Start() => GetComponent<StatusSubject>().RegisterStatus(_statusToggle);

		private void OnStateChanged(object sender, EventArgs e) => _statusToggle.Toggle(_gate.StateNeedCheck);

		#region IAwakableComponent

		private static string _CheckStateLocKey => "GerkinDev.WatertightGates.Status.Buildings.CheckState";
		private static string _CheckStateShortLocKey => "GerkinDev.WatertightGates.Status.Buildings.CheckStateShort";

		private WatertightGate _gate = null!;
		private StatusToggle _statusToggle = null!;

		public void Awake()
		{
			_gate = GetComponent<WatertightGate>();
			_statusToggle = StatusToggle.CreateNormalStatusWithAlertAndFloatingIcon("GateConflict",
				_loc.T(_CheckStateLocKey), _loc.T(_CheckStateShortLocKey));
			_gate.MainModeChanged += OnStateChanged;
		}

		#endregion
	}
}