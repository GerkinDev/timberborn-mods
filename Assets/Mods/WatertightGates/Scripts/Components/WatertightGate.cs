using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.Specs;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Extensions;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Services;
using System;
using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.EntitySystem;
using Timberborn.Illumination;
using Timberborn.Persistence;
using Timberborn.QuickNotificationSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components
{
	internal class WatertightGate : BaseComponent, IAwakableComponent, IPersistentEntity, IFinishedStateListener, IAutomatableNeeder, ITerminal, IInitializableEntity, IGateLike, IPreInitializableEntity
	{
		internal enum EGateMode
		{
			Open,
			Close,
			Pass,
		}
		internal enum EGateControlMode
		{
			Open,
			Close,
			Pass,
			Automated,
		}
		private static EGateControlMode _GateModeToControl(EGateMode mode) => mode switch
		{
			EGateMode.Open => EGateControlMode.Open,
			EGateMode.Close => EGateControlMode.Close,
			EGateMode.Pass => EGateControlMode.Pass,
			_ => throw new Exception($"Unexpected gate mode {mode}"),
		};
		private static EGateMode _GateControlToMode(EGateControlMode mode) => mode switch
		{
			EGateControlMode.Open => EGateMode.Open,
			EGateControlMode.Close => EGateMode.Close,
			EGateControlMode.Pass => EGateMode.Pass,
			_ => throw new Exception($"Unexpected gate control {mode}"),
		};
		private EGateControlMode _activationMode = EGateControlMode.Open;
		public EGateControlMode ActivationMode
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
		private EGateMode _CurrentGateMode => _activationMode switch
		{
			EGateControlMode.Automated => _automatable.State != ConnectionState.Off ? _activeGateMode : _inactiveGateMode,
			EGateControlMode.Open or EGateControlMode.Close or EGateControlMode.Pass => _GateControlToMode(_activationMode),
			_ => throw new Exception($"Unexpected activation mode {_activationMode}"),
		};

		public event EventHandler ConflictStateChanged;

		private readonly GateLikeUpdater _gateLikeUpdater;
		private readonly QuickNotificationService _quickNotificationService;

		public WatertightGate(GateLikeUpdater gateLikeUpdater, QuickNotificationService quickNotificationService)
		{
			_gateLikeUpdater = gateLikeUpdater;
			_quickNotificationService = quickNotificationService;
		}

		#region IAwakableComponent
		private WatertightGateSpec _spec;
		private Automatable _automatable;
		private BlockObject _blockObject;
		private NavMeshBlocker _navMeshBlocker;
		private Illuminator _illuminator;
		private WatertightGateTransformController _gateTransformController;
		private GateAutoOpener _autoOpener;

		public void Awake()
		{
			_spec = GetComponent<WatertightGateSpec>();
			_automatable = GetComponent<Automatable>();
			_blockObject = GetComponent<BlockObject>(); /// Position is not initialized yet. See <see cref="PreInitializeEntity"/> for transforms
			_navMeshBlocker = GetComponent<NavMeshBlocker>();
			_illuminator = GetComponent<Illuminator>();
			_gateTransformController = GetComponent<WatertightGateTransformController>();
			_autoOpener = GetComponent<GateAutoOpener>();
		}
		#endregion

		#region IPersistentEntity
		private static readonly ComponentKey _persistenceKey = new("WatertightGate");
		private static readonly PropertyKey<EGateControlMode> _activationModeKey = new(nameof(_activationMode));
		private static readonly PropertyKey<EGateMode> _activeGateModeKey = new(nameof(_activeGateMode));
		private static readonly PropertyKey<EGateMode> _inactiveGateModeKey = new(nameof(_inactiveGateMode));

		public void Load(IEntityLoader entityLoader)
		{
			ActiveGateMode = entityLoader.GetOrDefault(_persistenceKey, _activeGateModeKey, EGateMode.Open);
			InactiveGateMode = entityLoader.GetOrDefault(_persistenceKey, _inactiveGateModeKey, EGateMode.Close);
			try
			{
				_activationMode = entityLoader.GetOrDefault(_persistenceKey, _activationModeKey, EGateControlMode.Open);
			}
			catch (ArgumentException ex)
			{
				this.Log("Failed to parse value: {0}", ex);
				var mode = entityLoader.GetOrDefaultAsString(_persistenceKey, _activationModeKey, "Active").ToLower();
				switch (mode)
				{
					case "active":
						_activationMode = _GateModeToControl(ActiveGateMode);
						_quickNotificationService.SendNotification("Gate mode updated.");
						break;
					case "inactive":
						_activationMode = _GateModeToControl(InactiveGateMode);
						_quickNotificationService.SendNotification("Gate mode updated.");
						break;
					default:
						this.Log("Invalid loaded value {0}, falling back to open");
						_quickNotificationService.SendWarningNotification("Failed to load previous watertight gate mode. It has been opened as a default. Please verify your watertight gates to avoid leakage.");
						_activationMode = EGateControlMode.Open;
						break;
				}
				;
			}
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
		public bool NeedsAutomatable => _activationMode == EGateControlMode.Automated;
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
			CurrentGateState = EGateState.Closed;
			_ScheduleStateUpdate();
		}
		#endregion

		#region IGateLike
		private EGateState _currentGateState;
		public EGateState CurrentGateState
		{
			get => _currentGateState;
			set
			{
				var prevState = _currentGateState;
				_currentGateState = value;
				_UpdateState();
				if (prevState != _currentGateState && (prevState == EGateState.OpenConflict || _currentGateState == EGateState.OpenConflict))
				{
					_NotifyConflictStateChanged();
				}
			}
		}
		public Vector3Int PathStart { get; private set; }
		public Vector3Int PathEnd { get; private set; }
		public Vector3Int PathCenter { get; private set; }
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
				_gateTransformController.IsOpen = _CurrentGateMode == EGateMode.Open;
			}
		}

		private void _UpdateState()
		{
			var actualGateMode = _currentGateState != EGateState.Open ? EGateMode.Close : _CurrentGateMode;
			_navMeshBlocker.GateMode = actualGateMode;
			switch (actualGateMode)
			{
				case EGateMode.Close:
					_illuminator.Toggle(false);
					break;
				case EGateMode.Open:
					_illuminator.ClearColor(1);
					_illuminator.Toggle(true);
					break;
				case EGateMode.Pass:
					_illuminator.SetColor(1, Color.red);
					_illuminator.Toggle(true);
					break;
			}
			_autoOpener.IsWatching = actualGateMode == EGateMode.Pass;
			_gateTransformController.IsOpen = actualGateMode == EGateMode.Open;
		}

		private void _NotifyConflictStateChanged()
		{
			ConflictStateChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
