using System;
using Timberborn.BaseComponentSystem;
using Timberborn.EntitySystem;
using Timberborn.Localization;
using Timberborn.StatusSystem;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.UI
{
	internal class WatertightGateCheckState : BaseComponent, IAwakableComponent, IPostLoadableEntity
	{
		private static string _CheckStateLocKey => "GerkinDev.WatertightGates.Status.Buildings.CheckState{0}";
		private static string _CheckStateShortLocKey => "GerkinDev.WatertightGates.Status.Buildings.CheckStateShort";

		private readonly ILoc _loc;

		private WatertightGate _gate;

		private StatusToggle _statusToggle;

		public WatertightGateCheckState(ILoc loc)
		{
			_loc = loc;
		}

		public void Awake()
		{
			_gate = GetComponent<WatertightGate>();
		}

		public void OnStateChanged(object sender, EventArgs e)
		{
			if (_gate.BadStateReason == null)
			{
				_gate.MainModeChanged -= OnStateChanged;
				_statusToggle.Deactivate();
			}
		}

		public void PostLoadEntity()
		{
			if (_gate.BadStateReason != null)
			{
				_statusToggle = StatusToggle.CreateNormalStatusWithAlertAndFloatingIcon("GateConflict", _loc.T(_CheckStateLocKey, _gate.BadStateReason), _loc.T(_CheckStateShortLocKey));
				_statusToggle.Activate();
				_gate.MainModeChanged += OnStateChanged;
				GetComponent<StatusSubject>().RegisterStatus(_statusToggle);
			}
		}
	}
}
