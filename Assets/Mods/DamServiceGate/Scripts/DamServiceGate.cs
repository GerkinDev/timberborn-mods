using System;
using Timberborn.Automation;
using Timberborn.AutomationBuildings;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.EntitySystem;
using Timberborn.Fields;
using Timberborn.Hauling;
using Timberborn.Navigation;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts
{
	internal class DamServiceGate : BaseComponent, IAwakableComponent, IPersistentEntity, IFinishedStateListener, IAutomatableNeeder, ITerminal, IInitializableEntity
	{
		internal enum EMode
		{
			Open = 0b01,
			Close = 0b10,
			Pass = Open | Close,
			Automated = 0b00,
		}
		private EMode _mode = EMode.Open;
		public EMode Mode
		{
			get => _mode; set
			{
				if (_mode != value)
				{
					_mode = value;
					_UpdateState();
				}
			}
		}
		private bool _IsOpenByAutomation
		{
			get
			{
				if (Mode == EMode.Automated)
				{
					return _automatable.State != ConnectionState.Off;
				}

				return false;
			}
		}
		public bool IsConflict { get; private set; }
		public event EventHandler StateChanged;

		private readonly GateConflictDetector _gateConflictDetector;
		private readonly GateUpdater _gateUpdater;

		public DamServiceGate(GateConflictDetector gateConflictDetector, GateUpdater gateUpdater)
		{
			_gateConflictDetector = gateConflictDetector;
			_gateUpdater = gateUpdater;
		}

		#region IAwakableComponent
		private DamServiceGateSpec _spec;
		private Automatable _automatable;
		private BlockObject _blockObject;
		private Transform _anchor;
		private NavMeshBlocker _navMeshBlocker;
		private WaterBlocker _waterBlocker;

		public void Awake()
		{
			_ = typeof(Timberborn.WaterBuildings.Floodgate);
			_ = typeof(Timberborn.AutomationBuildings.Gate);
			_ = typeof(BlockObject);
			_ = typeof(FarmHouse);
			_ = typeof(HaulCandidate);
			//_ = typeof(Suspension);
			//_ = typeof(TimbermeshPreviewFactory);
			_spec = GetComponent<DamServiceGateSpec>();
			_automatable = GetComponent<Automatable>();
			_blockObject = GetComponent<BlockObject>();
			_anchor = GameObject.FindChildTransform(_spec.Anchor);
			_navMeshBlocker = GetComponent<NavMeshBlocker>();
			_waterBlocker = GetComponent<WaterBlocker>();
		}
		#endregion

		#region IPersistentEntity
		private static readonly ComponentKey _persistenceKey = new("DamServiceGate");
		private static readonly PropertyKey<EMode> _modeKey = new(nameof(_mode));

		public void Load(IEntityLoader entityLoader)
		{
			if (entityLoader.TryGetComponent(_persistenceKey, out var objectLoader))
			{
				_mode = objectLoader.Get(_modeKey);
			}
		}

		public void Save(IEntitySaver entitySaver)
		{
			entitySaver.GetComponent(_persistenceKey).Set(_modeKey, _mode);
		}
		#endregion

		#region IFinishedStateListener
		public void OnEnterFinishedState()
		{
			_UpdateState();
		}

		public void OnExitFinishedState()
		{
		}
		#endregion

		#region IAutomatableNeeder
		public bool NeedsAutomatable => Mode == EMode.Automated;
		#endregion

		#region ITerminal
		public void Evaluate()
		{
			if (NeedsAutomatable)
			{
				_UpdateState();
			}
		}
		#endregion

		#region IInitializableEntity
		public void InitializeEntity()
		{
			_UpdateState();
		}
		#endregion

		private bool _WantOpen => _mode == EMode.Open || _IsOpenByAutomation;
		private bool _isActuallyOpen;
		private void _UpdateState()
		{
			if (!_WantOpen || _gateConflictDetector.CanOpenGateWithoutConflict(
				_blockObject.TransformCoordinates(_spec.PathStart),
				_blockObject.TransformCoordinates(_spec.PathEnd),
				_blockObject.TransformCoordinates(_spec.PathCenter),
				_gateUpdater._openGateCrossings
			))
			{
				IsConflict = false;
				_isActuallyOpen = _WantOpen;
				_NotifyStateChanged();
				_AddToOpenGateCrossings();
			}
			else
			{
				IsConflict = true;
				_isActuallyOpen = false;
				_NotifyStateChanged();
			}

			if (_blockObject.IsFinished)
			{
				_navMeshBlocker.NavMeshBlocked = !_isActuallyOpen;
				_waterBlocker.Height = _isActuallyOpen ? 0 : 1;
			}
			_SetAnchorTransform(_isActuallyOpen ? _spec.OpenTransform : _spec.CloseTransform);
		}
		private void _SetAnchorTransform(GateTransformSpec transform)
		{
			_anchor.transform.SetLocalPositionAndRotation(transform.Position, Quaternion.Euler(transform.Rotation));
		}

		private void _NotifyStateChanged()
		{
			StateChanged?.Invoke(this, EventArgs.Empty);
		}
		private void _AddToOpenGateCrossings()
		{
			_gateUpdater._openGateCrossings[_blockObject.TransformCoordinates(_spec.PathStart)] = _blockObject.TransformCoordinates(_spec.PathEnd);
			_gateUpdater._openGateCrossings[_blockObject.TransformCoordinates(_spec.PathEnd)] = _blockObject.TransformCoordinates(_spec.PathStart);
		}
	}
}
