using GerkinDev.WatertightGates.Components.Specs;
using GerkinDev.WatertightGates.Extensions;
using GerkinDev.WatertightGates.Services;
using System;
using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.Illumination;
using Timberborn.Localization;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace GerkinDev.WatertightGates.Components
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
		public event EventHandler MainModeChanged;
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
				BadStateReason = null;
				_ScheduleStateUpdate();
				MainModeChanged?.Invoke(this, EventArgs.Empty);
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
		private readonly OptionalDependencies _optionalDependencies;
		private readonly ILoc _loc;

		public WatertightGate(GateLikeUpdater gateLikeUpdater, OptionalDependencies optionalDependencies, ILoc loc)
		{
			_gateLikeUpdater = gateLikeUpdater;
			_optionalDependencies = optionalDependencies;
			_loc = loc;
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
		private static readonly PropertyKey<EGateMainMode> _mainModeKey = new(nameof(_mainMode));
		private static readonly PropertyKey<EGateMode> _activeGateModeKey = new(nameof(_activeGateMode));
		private static readonly PropertyKey<EGateMode> _inactiveGateModeKey = new(nameof(_inactiveGateMode));
		public string? BadStateReason { get; private set; } = null;

		public void Load(IEntityLoader entityLoader)
		{
			ActiveGateMode = entityLoader.GetOrDefault(_persistenceKey, _activeGateModeKey, EGateMode.Open);
			InactiveGateMode = entityLoader.GetOrDefault(_persistenceKey, _inactiveGateModeKey, EGateMode.Close);
			_mainMode = _LoadMainModeWithBackwardCompatibility(entityLoader);
		}

		private EGateMainMode _LoadMainModeWithBackwardCompatibility(IEntityLoader entityLoader)
		{
			EGateMainMode mode;
			try
			{
				mode = entityLoader.GetRequired(_persistenceKey, _mainModeKey);
			}
			catch (IEntityLoaderExtensions.PersistenceException ex)
			{
				this.Log("Failed to load value: {0}", ex);
				var modeStr = entityLoader.GetOrDefaultAsString(
					_persistenceKey,
					_mainModeKey,
					() => entityLoader.GetOrDefaultAsString(_persistenceKey, new PropertyKey<string>("_activationMode"), "Pass")
				).ToLower();
				switch (modeStr)
				{
					case "open":
						mode = EGateMainMode.Open;
						break;
					case "close":
						mode = EGateMainMode.Close;
						break;
					case "active":
						mode = _GateModeToMainMode(ActiveGateMode);
						break;
					case "inactive":
						mode = _GateModeToMainMode(InactiveGateMode);
						break;
					case "pass":
						mode = EGateMainMode.Pass;
						break;
					default:
						this.Log("Invalid loaded value {0}, falling back to open", modeStr);
						mode = EGateMainMode.Open;
						BadStateReason = _loc.T("GerkinDev.WatertightGates.Status.Buildings.CheckState.Reason.BadValue{0}", modeStr);
						break;
				}
				this.Log("Resolved to {0}", mode);
			}

			// Check for optional dependency missing but needed by state
			if (!_optionalDependencies.PressurePlates)
			{
				if (mode == EGateMainMode.Pass)
				{
					this.Log("Was {0}, but missing dependency", mode);
					mode = EGateMainMode.Close;
					BadStateReason = _loc.T("GerkinDev.WatertightGates.Status.Buildings.CheckState.Reason.PassMissDependency");
				}
				if (ActiveGateMode == EGateMode.Pass)
				{
					this.Log("Was {0}, but missing dependency for active mode", mode);
					ActiveGateMode = EGateMode.Close;
					if (mode == EGateMainMode.Automated)
					{
						BadStateReason = _loc.T("GerkinDev.WatertightGates.Status.Buildings.CheckState.Reason.PassMissDependency");
					}
				}
				if (InactiveGateMode == EGateMode.Pass)
				{
					this.Log("Was {0}, but missing dependency for inactive mode", mode);
					InactiveGateMode = EGateMode.Close;
					if (mode == EGateMainMode.Automated)
					{
						BadStateReason = _loc.T("GerkinDev.WatertightGates.Status.Buildings.CheckState.Reason.PassMissDependency");
					}
				}
			}
			return mode;
		}

		public void Save(IEntitySaver entitySaver)
		{
			var objectSaver = entitySaver.GetComponent(_persistenceKey);
			objectSaver.Set(_mainModeKey, _mainMode);
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
