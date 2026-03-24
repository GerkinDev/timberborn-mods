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
		internal enum EGateMainMode
		{
			Open,
			Close,
			Pass,
			Automated,
		}
		private static EGateMainMode _GateModeToMainMode(EGateMode mode) => mode switch
		{
			EGateMode.Open => EGateMainMode.Open,
			EGateMode.Close => EGateMainMode.Close,
			EGateMode.Pass => EGateMainMode.Pass,
			_ => throw new ArgumentException($"Unexpected gate mode {mode}"),
		};
		private static EGateMode _MainModeToGateMode(EGateMainMode mode) => mode switch
		{
			EGateMainMode.Open => EGateMode.Open,
			EGateMainMode.Close => EGateMode.Close,
			EGateMainMode.Pass => EGateMode.Pass,
			_ => throw new ArgumentException($"Unexpected gate main mode {mode}"),
		};
		private EGateMainMode _mainMode = EGateMainMode.Open;
		public EGateMainMode MainMode
		{
			get => _mainMode;
			set
			{
				if (_mainMode == value)
				{
					return;
				}
				_mainMode = value;
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
		private EGateMode _CurrentGateMode => _mainMode switch
		{
			EGateMainMode.Automated => _automatable.State != ConnectionState.Off ? _activeGateMode : _inactiveGateMode,
			EGateMainMode.Open or EGateMainMode.Close or EGateMainMode.Pass => _MainModeToGateMode(_mainMode),
			_ => throw new ArgumentException($"Unexpected gate main mode {_mainMode}"),
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
		private GateAutoOpener? _autoOpener;

		public void Awake()
		{
			_spec = GetComponent<WatertightGateSpec>();
			_automatable = GetComponent<Automatable>();
			_blockObject = GetComponent<BlockObject>(); /// Position is not initialized yet. See <see cref="PreInitializeEntity"/> for transforms
			_navMeshBlocker = GetComponent<NavMeshBlocker>();
			_illuminator = GetComponent<Illuminator>();
			_gateTransformController = GetComponent<WatertightGateTransformController>();
			TryGetComponent(out _autoOpener);
		}
		#endregion

		#region IPersistentEntity
		private static readonly ComponentKey _persistenceKey = new("WatertightGate");
		private static readonly PropertyKey<EGateMainMode> __mainModeKey = new(nameof(_mainMode));
		private static readonly PropertyKey<EGateMode> _activeGateModeKey = new(nameof(_activeGateMode));
		private static readonly PropertyKey<EGateMode> _inactiveGateModeKey = new(nameof(_inactiveGateMode));

		public void Load(IEntityLoader entityLoader)
		{
			ActiveGateMode = entityLoader.GetOrDefault(_persistenceKey, _activeGateModeKey, EGateMode.Open);
			InactiveGateMode = entityLoader.GetOrDefault(_persistenceKey, _inactiveGateModeKey, EGateMode.Close);
			try
			{
				_mainMode = entityLoader.GetOrDefault(_persistenceKey, __mainModeKey, EGateMainMode.Open);
			}
			catch (ArgumentException ex)
			{
				this.Log("Failed to parse value: {0}", ex);
				var mode = entityLoader.GetOrDefaultAsString(_persistenceKey, __mainModeKey, "Active").ToLower();
				switch (mode)
				{
					case "active":
						_mainMode = _GateModeToMainMode(ActiveGateMode);
						_quickNotificationService.SendNotification("Gate mode updated.");
						break;
					case "inactive":
						_mainMode = _GateModeToMainMode(InactiveGateMode);
						_quickNotificationService.SendNotification("Gate mode updated.");
						break;
					default:
						this.Log("Invalid loaded value {0}, falling back to open");
						_quickNotificationService.SendWarningNotification("Failed to load previous watertight gate mode. It has been opened as a default. Please verify your watertight gates to avoid leakage.");
						_mainMode = EGateMainMode.Open;
						break;
				}
				;
			}
		}

		public void Save(IEntitySaver entitySaver)
		{
			var objectSaver = entitySaver.GetComponent(_persistenceKey);
			objectSaver.Set(__mainModeKey, _mainMode);
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
		public bool NeedsAutomatable => _mainMode == EGateMainMode.Automated;
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
			if (_autoOpener != null)
			{
				_autoOpener.IsWatching = actualGateMode == EGateMode.Pass;
			}
			_gateTransformController.IsOpen = actualGateMode == EGateMode.Open;
		}

		private void _NotifyConflictStateChanged()
		{
			ConflictStateChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
