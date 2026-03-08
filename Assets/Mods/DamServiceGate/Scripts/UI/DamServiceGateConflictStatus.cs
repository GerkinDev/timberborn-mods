using System;
using Timberborn.AutomationBuildingsUI;
using Timberborn.BaseComponentSystem;
using Timberborn.Localization;
using Timberborn.StatusSystem;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts.UI
{
	internal class DamServiceGateConflictStatus : BaseComponent, IAwakableComponent, IStartableComponent
	{
		private static string _ConflictLocKey => GateConflictStatus.ConflictLocKey;
		private static string _ConflictShortLocKey => GateConflictStatus.ConflictShortLocKey;

		private readonly ILoc _loc;

		private DamServiceGate _gate;

		private StatusToggle _statusToggle;

		public DamServiceGateConflictStatus(ILoc loc)
		{
			_loc = loc;
		}

		public void Awake()
		{
			_gate = GetComponent<DamServiceGate>();
			_statusToggle = StatusToggle.CreateNormalStatusWithAlertAndFloatingIcon("GateConflict", _loc.T(_ConflictLocKey), _loc.T(_ConflictShortLocKey));
			_gate.StateChanged += OnStateChanged;
		}

		public void Start()
		{
			GetComponent<StatusSubject>().RegisterStatus(_statusToggle);
		}

		public void OnStateChanged(object sender, EventArgs e)
		{
			_statusToggle.Toggle(_gate.IsConflict);
		}
	}
}
