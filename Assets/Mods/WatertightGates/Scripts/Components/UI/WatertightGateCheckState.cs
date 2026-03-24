using System;
using Timberborn.BaseComponentSystem;
using Timberborn.EntitySystem;
using Timberborn.Localization;
using Timberborn.StatusSystem;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.UI
{
	internal class WatertightGateCheckState : BaseComponent, IAwakableComponent, IStartableComponent, IPostLoadableEntity
	{
		private static string _CheckStateLocKey => "GerkinDev.WatertightGates.Status.Buildings.CheckState";
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
			_statusToggle = StatusToggle.CreateNormalStatusWithAlertAndFloatingIcon("GateConflict", _loc.T(_CheckStateLocKey), _loc.T(_CheckStateShortLocKey));
			_gate.MainModeChanged += OnStateChanged;
		}

		public void Start()
		{
			GetComponent<StatusSubject>().RegisterStatus(_statusToggle);
		}

		public void OnStateChanged(object sender, EventArgs e)
		{
			_statusToggle.Toggle(_gate.StateNeedCheck);
		}

		public void PostLoadEntity()
		{
			_statusToggle.Toggle(_gate.StateNeedCheck);
		}
	}
}
