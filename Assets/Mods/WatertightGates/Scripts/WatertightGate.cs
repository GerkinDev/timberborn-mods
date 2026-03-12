using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Extensions;
using System;
using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts
{
	internal class WatertightGate : BaseComponent, IAwakableComponent, IPersistentEntity, IFinishedStateListener, IAutomatableNeeder, ITerminal, IInitializableEntity, IGateLike, IPreInitializableEntity
	{
		internal enum EActivationMode
		{
			Active,
			Inactive,
			Automated,
		}
		internal enum EGateMode
		{
			Open,
			Close,
			Pass,
		}
		private EActivationMode _activationMode = EActivationMode.Active;
		public EActivationMode ActivationMode
		{
			get => _activationMode;
			set
			{
				if (_activationMode == value)
				{
					return;
				}
				_activationMode = value;
				_ScheduleStateUpdate();
			}
		}
		private EGateMode _activeGateMode = EGateMode.Open;
		public EGateMode ActiveGateMode
		{
			get => _activeGateMode; set
			{
				if (_activeGateMode == value)
				{
					return;
				}
				_activeGateMode = value;
				_ScheduleStateUpdate();
			}
		}
		private EGateMode _inactiveGateMode = EGateMode.Close;
		public EGateMode InactiveGateMode
		{
			get => _inactiveGateMode; set
			{
				if (_inactiveGateMode == value)
				{
					return;
				}
				_inactiveGateMode = value;
				_ScheduleStateUpdate();
			}
		}
		private EGateMode _CurrentGateMode
		{
			get
			{
				bool isActive = _activationMode switch
				{
					EActivationMode.Automated => _automatable.State != ConnectionState.Off,
					EActivationMode.Active => true,
					EActivationMode.Inactive => false,
					_ => throw new Exception($"Unexpected activation mode {_activationMode}"),
				};
				return isActive ? _activeGateMode : _inactiveGateMode;
			}
		}

		public bool IsConflict { get; private set; }
		public event EventHandler StateChanged;

		private readonly GateLikeUpdater _gateLikeUpdater;

		public WatertightGate(GateLikeUpdater gateLikeUpdater)
		{
			_gateLikeUpdater = gateLikeUpdater;
		}

		#region IAwakableComponent
		private WatertightGateSpec _spec;
		private Automatable _automatable;
		private BlockObject _blockObject;
		private NavMeshBlocker _navMeshBlocker;
		private WaterBlocker _waterBlocker;
		private Transform _anchor;
		private Vector3 _anchorInitialRotation;
		private Vector3 _anchorInitialPosition;

		public void Awake()
		{
			_spec = GetComponent<WatertightGateSpec>();
			_automatable = GetComponent<Automatable>();
			_blockObject = GetComponent<BlockObject>(); /// Position is not initialized yet. See <see cref="PreInitializeEntity"/> for transforms
			_navMeshBlocker = GetComponent<NavMeshBlocker>();
			_waterBlocker = GetComponent<WaterBlocker>();
			_anchor = GameObject.FindChildTransform(_spec.Anchor);
			_anchorInitialRotation = _anchor.transform.rotation.eulerAngles;
			_anchorInitialPosition = _anchor.transform.position;
		}
		#endregion

		#region IPersistentEntity
		private static readonly ComponentKey _persistenceKey = new("WatertightGate");
		private static readonly PropertyKey<EActivationMode> _activationModeKey = new(nameof(_activationMode));
		private static readonly PropertyKey<EGateMode> _activeGateModeKey = new(nameof(_activeGateMode));
		private static readonly PropertyKey<EGateMode> _inactiveGateModeKey = new(nameof(_inactiveGateMode));

		public void Load(IEntityLoader entityLoader)
		{
			_activationMode = entityLoader.GetOrDefault(_persistenceKey, _activationModeKey, EActivationMode.Active);
			ActiveGateMode = entityLoader.GetOrDefault(_persistenceKey, _activeGateModeKey, EGateMode.Open);
			InactiveGateMode = entityLoader.GetOrDefault(_persistenceKey, _inactiveGateModeKey, EGateMode.Close);
		}

		public void Save(IEntitySaver entitySaver)
		{
			var objectSaver = entitySaver.GetComponent(_persistenceKey);
			objectSaver.Set(_activationModeKey, _activationMode);
			objectSaver.Set(_activeGateModeKey, ActiveGateMode);
			objectSaver.Set(_inactiveGateModeKey, InactiveGateMode);
		}
		#endregion

		#region IFinishedStateListener
		public void OnEnterFinishedState()
		{
			_ScheduleStateUpdate();
		}

		public void OnExitFinishedState()
		{
		}
		#endregion

		#region IAutomatableNeeder
		public bool NeedsAutomatable => _activationMode == EActivationMode.Automated;
		#endregion

		#region ITerminal
		public void Evaluate()
		{
			if (NeedsAutomatable)
			{
				_ScheduleStateUpdate();
			}
		}
		#endregion

		#region IInitializableEntity
		public void InitializeEntity()
		{
			Close();
			_ScheduleStateUpdate();
		}
		#endregion

		#region IGateLike
		public bool IsClosed { get; private set; }
		public Vector3Int PathStart { get; private set; }
		public Vector3Int PathEnd { get; private set; }
		public Vector3Int PathCenter { get; private set; }

		public void Close()
		{
			IsClosed = true;
			_UpdateState();
		}

		public void Open()
		{
			IsClosed = false;
			_UpdateState();
		}

		public void EnableConflict()
		{
			IsConflict = true;
			_NotifyStateChanged();
		}

		public void DisableConflict()
		{
			IsConflict = false;
			_NotifyStateChanged();
		}
		#endregion
		#region IPreInitializableEntity
		public void PreInitializeEntity()
		{
			PathStart = _blockObject.TransformCoordinates(_spec.PathStart);
			PathEnd = _blockObject.TransformCoordinates(_spec.PathEnd);
			PathCenter = _blockObject.TransformCoordinates(_spec.PathCenter);
		}
		#endregion

		private void _ScheduleStateUpdate()
		{
			if (_blockObject.IsFinished)
			{
				this.Log("Scheduling gate for desired {0}", _CurrentGateMode);
				if (_CurrentGateMode != EGateMode.Close)
				{
					_gateLikeUpdater.ScheduleToOpen(this);
				}
				else
				{
					_gateLikeUpdater.ScheduleToClose(this);
				}
			}
			else
			{
				_SetAnchorTransform(_CurrentGateMode);
			}
		}
		private void _UpdateState()
		{
			var actualGateMode = IsClosed ? EGateMode.Close : _CurrentGateMode;
			this.Log("Set gate closed {0}, desired {1}, actual {2}", IsClosed, _CurrentGateMode, actualGateMode);
			_navMeshBlocker.GateMode = actualGateMode;
			if (_blockObject.IsFinished)
			{
				_waterBlocker.Height = actualGateMode == EGateMode.Open ? 0 : 1;
			}
			_SetAnchorTransform(actualGateMode);
		}

		private void _SetAnchorTransform(EGateMode gateMode)
		{
			var transform = gateMode == EGateMode.Open ? _spec.OpenTransform : _spec.CloseTransform;
			_anchor.transform.SetLocalPositionAndRotation(transform.Position + _anchorInitialPosition, Quaternion.Euler(transform.Rotation + _anchorInitialRotation));
		}

		private void _NotifyStateChanged()
		{
			StateChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
