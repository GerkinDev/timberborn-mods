using System;
using GerkinDev.WatertightGates.Components.Specs;
using GerkinDev.WatertightGates.Extensions;
using GerkinDev.WatertightGates.Services;
using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.DuplicationSystem;
using Timberborn.EntitySystem;
using Timberborn.Illumination;
using Timberborn.Localization;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace GerkinDev.WatertightGates.Components
{
	internal class WatertightGate : BaseComponent, IAwakableComponent, IPersistentEntity, IFinishedStateListener,
		IAutomatableNeeder, ITerminal, IInitializableEntity, IGateLike, IPreInitializableEntity,
		IDuplicable<WatertightGate>, IDuplicable
	{
		private readonly GateLikeUpdater _gateLikeUpdater;
		private readonly ILoc _loc;
		private readonly OptionalDependencies _optionalDependencies;
		private EGateMode _activeGateMode = EGateMode.OPEN;

		private EGateMode _inactiveGateMode = EGateMode.CLOSE;
		private EGateMainMode _mainMode = EGateMainMode.OPEN;

		public WatertightGate(GateLikeUpdater gateLikeUpdater, OptionalDependencies optionalDependencies, ILoc loc)
		{
			_gateLikeUpdater = gateLikeUpdater;
			_optionalDependencies = optionalDependencies;
			_loc = loc;
		}

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

		public EGateMode ActiveGateMode
		{
			get => _activeGateMode;
			set
			{
				if (_activeGateMode == value)
				{
					return;
				}

				_activeGateMode = value;
				BadStateReason = null;
				_ScheduleStateUpdate();
			}
		}

		public EGateMode InactiveGateMode
		{
			get => _inactiveGateMode;
			set
			{
				if (_inactiveGateMode == value)
				{
					return;
				}

				_inactiveGateMode = value;
				BadStateReason = null;
				_ScheduleStateUpdate();
			}
		}

		private EGateMode _CurrentGateMode => _mainMode switch
		{
			EGateMainMode.AUTOMATED => _automatable.State != ConnectionState.Off ? _activeGateMode : _inactiveGateMode,
			EGateMainMode.OPEN or EGateMainMode.CLOSE or EGateMainMode.PASS => _MainModeToGateMode(_mainMode),
			_ => throw new ArgumentException($"Unexpected gate main mode {_mainMode}")
		};

		#region IAutomatableNeeder

		public bool NeedsAutomatable => _mainMode == EGateMainMode.AUTOMATED;

		#endregion

		#region IDuplicable<WatertightGate>

		public void DuplicateFrom(WatertightGate source)
		{
			MainMode = source.MainMode;
			ActiveGateMode = source.ActiveGateMode;
			InactiveGateMode = source.InactiveGateMode;
			_automatable = source._automatable;
			_ScheduleStateUpdate();
		}

		#endregion

		#region IInitializableEntity

		public void InitializeEntity()
		{
			CurrentGateState = EGateState.Closed;
			_ScheduleStateUpdate();
			_didInitialize = true;
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

		#region ITerminal

		public void Evaluate()
		{
			if (NeedsAutomatable)
			{
				_ScheduleStateUpdate();
			}
		}

		#endregion

		private static EGateMainMode _GateModeToMainMode(EGateMode mode) => mode switch
		{
			EGateMode.OPEN => EGateMainMode.OPEN,
			EGateMode.CLOSE => EGateMainMode.CLOSE,
			EGateMode.PASS => EGateMainMode.PASS,
			_ => throw new ArgumentException($"Unexpected gate mode {mode}")
		};

		private static EGateMode _MainModeToGateMode(EGateMainMode mode) => mode switch
		{
			EGateMainMode.OPEN => EGateMode.OPEN,
			EGateMainMode.CLOSE => EGateMode.CLOSE,
			EGateMainMode.PASS => EGateMode.PASS,
			_ => throw new ArgumentException($"Unexpected gate main mode {mode}")
		};

		public event EventHandler? MainModeChanged;

		public event EventHandler? ConflictStateChanged;

		private void _ScheduleStateUpdate()
		{
			if (_blockObject.IsFinished)
			{
				if (_CurrentGateMode != EGateMode.CLOSE)
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
				_gateTransformController.IsOpen = _CurrentGateMode == EGateMode.OPEN;
			}
		}

		private bool _didInitialize;
		private bool _didScheduledForCorrectState;
		private void _UpdateState()
		{
			var actualGateMode = _currentGateState != EGateState.Open ? EGateMode.CLOSE : _CurrentGateMode;
			_navMeshBlocker.GateMode = actualGateMode;
			switch (actualGateMode)
			{
				case EGateMode.CLOSE:
					_illuminator.Toggle(false);
					break;
				case EGateMode.OPEN:
					_illuminator.ClearColor(1);
					_illuminator.Toggle(true);
					break;
				case EGateMode.PASS:
					_illuminator.SetColor(1, Color.red);
					_illuminator.Toggle(true);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (_autoOpener != null)
			{
				_autoOpener.IsWatching = actualGateMode == EGateMode.PASS;
			}

			if (_didInitialize && _didScheduledForCorrectState)
			{
				_gateTransformController.IsOpen = actualGateMode == EGateMode.OPEN;
			}
			else
			{
				_gateTransformController.SetImmediateOpen(actualGateMode == EGateMode.OPEN);
			}
		}

		private void _NotifyConflictStateChanged() => ConflictStateChanged?.Invoke(this, EventArgs.Empty);

		internal enum EGateMode
		{
			OPEN,
			CLOSE,
			PASS
		}

		internal enum EGateMainMode
		{
			OPEN,
			CLOSE,
			PASS,
			AUTOMATED
		}

		#region IAwakableComponent

		private WatertightGateSpec _spec = null!;
		private Automatable _automatable = null!;
		private BlockObject _blockObject = null!;
		private NavMeshBlocker _navMeshBlocker = null!;
		private Illuminator _illuminator = null!;
		private WatertightGateTransformController _gateTransformController = null!;
		private GateAutoOpener? _autoOpener;

		public void Awake()
		{
			_spec = GetComponent<WatertightGateSpec>();
			_automatable = GetComponent<Automatable>();
			// Position is not initialized yet. See <see cref="PreInitializeEntity"/> for transforms
			_blockObject = GetComponent<BlockObject>();
			_navMeshBlocker = GetComponent<NavMeshBlocker>();
			_illuminator = GetComponent<Illuminator>();
			_gateTransformController = GetComponent<WatertightGateTransformController>();
			TryGetComponent(out _autoOpener);
		}

		#endregion

		#region IPersistentEntity

		internal static readonly ComponentKey _persistenceKey = new("WatertightGate");
		internal static readonly PropertyKey<EGateMainMode> _mainModeKey = new(nameof(_mainMode));
		internal static readonly PropertyKey<EGateMode> _activeGateModeKey = new(nameof(_activeGateMode));
		internal static readonly PropertyKey<EGateMode> _inactiveGateModeKey = new(nameof(_inactiveGateMode));
		public string? BadStateReason { get; private set; }


		private EGateMode _LoadGateModeWithBackwardCompatibility(IEntityLoader entityLoader, PropertyKey<EGateMode> key,
			string label, EGateMode defaultMode)
		{
			try
			{
				return entityLoader.GetOrDefault(_persistenceKey, key, defaultMode);
			}
			catch (ArgumentException ex) when (ex.InnerException is ArgumentException)
			{
				var stringValue = entityLoader.GetAsString(_persistenceKey, key);
				if (string.IsNullOrEmpty(stringValue))
				{
					this.Warn("No {0} gate mode found", label);
					return defaultMode;
				}

				this.Log("Has legacy {0} gate mode stored value \"{1}\"", label, stringValue);
				if (Enum.TryParse(stringValue, true, out EGateMode value))
				{
					return value;
				}

				this.Warn(
					"Legacy {0} gate mode stored value \"{1}\" is invalid, return default \"{2}\"",
					label,
					stringValue,
					defaultMode
				);
				BadStateReason =
					_loc.T("GerkinDev.WatertightGates.Status.Buildings.CheckState.Reason.BadValue{0}", stringValue);
				return defaultMode;
			}
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
				this.Log("Failed to load main mode value: {0}", ex);
				var modeStr = entityLoader.GetOrDefaultAsString(
					_persistenceKey,
					_mainModeKey,
					() => entityLoader.GetOrDefaultAsString(
						_persistenceKey,
						new PropertyKey<string>("_activationMode"),
						"Active"
					)
				).ToLower();
				switch (modeStr)
				{
					case "open":
						mode = EGateMainMode.OPEN;
						break;
					case "close":
						mode = EGateMainMode.CLOSE;
						break;
					case "active":
						mode = _GateModeToMainMode(ActiveGateMode);
						break;
					case "inactive":
						mode = _GateModeToMainMode(InactiveGateMode);
						break;
					case "pass":
						mode = EGateMainMode.PASS;
						break;
					case "automated":
						mode = EGateMainMode.AUTOMATED;
						break;
					default:
						this.Warn("Invalid loaded main mode value \"{0}\", falling back to open", modeStr);
						mode = EGateMainMode.OPEN;
						BadStateReason =
							_loc.T("GerkinDev.WatertightGates.Status.Buildings.CheckState.Reason.BadValue{0}", modeStr);
						break;
				}

				this.Log("Resolved to {0}", mode);
			}

			return mode;
		}

		public void Load(IEntityLoader entityLoader)
		{
			_activeGateMode =
				_LoadGateModeWithBackwardCompatibility(entityLoader, _activeGateModeKey, "active", EGateMode.OPEN);
			_inactiveGateMode =
				_LoadGateModeWithBackwardCompatibility(entityLoader, _inactiveGateModeKey, "inactive", EGateMode.CLOSE);
			_mainMode = _LoadMainModeWithBackwardCompatibility(entityLoader);


			// Check for optional dependency missing but needed by state
			if (!_optionalDependencies.PressurePlates)
			{
				if (_mainMode == EGateMainMode.PASS)
				{
					this.Log("Main mode was \"{0}\", but missing dependency", _mainMode);
					_mainMode = EGateMainMode.CLOSE;
					BadStateReason =
						_loc.T("GerkinDev.WatertightGates.Status.Buildings.CheckState.Reason.PassMissDependency");
				}

				if (ActiveGateMode == EGateMode.PASS)
				{
					this.Log("Active gate mode was \"{0}\", but missing dependency", ActiveGateMode);
					ActiveGateMode = EGateMode.CLOSE;
					// Active/inactive gates are effective only if main is automated
					if (_mainMode == EGateMainMode.AUTOMATED)
					{
						BadStateReason =
							_loc.T("GerkinDev.WatertightGates.Status.Buildings.CheckState.Reason.PassMissDependency");
					}
				}

				if (InactiveGateMode == EGateMode.PASS)
				{
					this.Log("Inactive gate mode was \"{0}\", but missing dependency", InactiveGateMode);
					InactiveGateMode = EGateMode.CLOSE;
					// Active/inactive gates are effective only if main is automated
					if (_mainMode == EGateMainMode.AUTOMATED)
					{
						BadStateReason =
							_loc.T("GerkinDev.WatertightGates.Status.Buildings.CheckState.Reason.PassMissDependency");
					}
				}
			}

			_ScheduleStateUpdate();
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

		public void OnEnterFinishedState() => _ScheduleStateUpdate();

		public void OnExitFinishedState()
		{
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
				if (_didInitialize)
				{
					_didScheduledForCorrectState = true;
				}
				if (
					prevState != _currentGateState &&
					(prevState == EGateState.OpenConflict || _currentGateState == EGateState.OpenConflict)
				)
				{
					_NotifyConflictStateChanged();
				}
			}
		}

		public Vector3Int PathStart { get; private set; }
		public Vector3Int PathEnd { get; private set; }
		public Vector3Int PathCenter { get; private set; }

		#endregion
	}
}