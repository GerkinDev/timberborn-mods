using System;
using Timberborn.BaseComponentSystem;
using Timberborn.EntitySystem;
using Timberborn.Localization;
using Timberborn.StatusSystem;

namespace GerkinDev.WatertightGates.Components.UI
{
	internal class WatertightGateCheckState : BaseComponent, IAwakableComponent, IPostLoadableEntity
	{
		private readonly ILoc _loc;
		private StatusToggle? _statusToggle;

		public WatertightGateCheckState(ILoc loc)
		{
			_loc = loc;
		}


		#region IPostLoadableEntity

		public void PostLoadEntity()
		{
			if (_gate.BadStateReason == null)
			{
				return;
			}

			_statusToggle = StatusToggle.CreateNormalStatusWithAlertAndFloatingIcon("GateConflict",
				_loc.T(_CheckStateLocKey, _gate.BadStateReason), _loc.T(_CheckStateShortLocKey));
			_gate.MainModeChanged += _OnStateChanged;
			_statusSubject.RegisterStatus(_statusToggle);
			_statusToggle.Activate();
		}

		#endregion

		private void _OnStateChanged(object sender, EventArgs e)
		{
			if (_gate.BadStateReason != null)
			{
				return;
			}

			_gate.MainModeChanged -= _OnStateChanged;
			_statusToggle?.Deactivate();
		}

		#region IAwakableComponent

		private static string _CheckStateLocKey => "GerkinDev.WatertightGates.Status.Buildings.CheckState{0}";
		private static string _CheckStateShortLocKey => "GerkinDev.WatertightGates.Status.Buildings.CheckStateShort";

		private WatertightGate _gate = null!;
		private StatusSubject _statusSubject = null!;

		public void Awake()
		{
			_gate = GetComponent<WatertightGate>();
			_statusSubject = GetComponent<StatusSubject>();
		}

		#endregion
	}
}