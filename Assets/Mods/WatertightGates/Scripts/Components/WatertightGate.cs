using GerkinDev.WatertightGates.Components.Specs;
using GerkinDev.WatertightGates.Extensions;
using GerkinDev.WatertightGates.Services;
using System;
using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.DuplicationSystem;
using Timberborn.EntitySystem;
using Timberborn.Illumination;
using Timberborn.Persistence;
using Timberborn.QuickNotificationSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace GerkinDev.WatertightGates.Components
{
	internal class WatertightGate : BaseComponent, IAwakableComponent, IPersistentEntity, IFinishedStateListener,
		IAutomatableNeeder, ITerminal, IInitializableEntity, IGateLike, IPreInitializableEntity,
		IDuplicable<WatertightGate>, IDuplicable
	{
		private readonly GateLikeUpdater _gateLikeUpdater;
		private readonly QuickNotificationService _quickNotificationService;

		private EGateMode _activeGateMode = EGateMode.OPEN;

		private EGateMode _inactiveGateMode = EGateMode.CLOSE;
		private EGateMainMode _mainMode = EGateMainMode.OPEN;

		public WatertightGate(GateLikeUpdater gateLikeUpdater, QuickNotificationService quickNotificationService)
		{
			_gateLikeUpdater = gateLikeUpdater;
			_quickNotificationService = quickNotificationService;
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
				StateNeedCheck = false;
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
		public bool StateNeedCheck { get; private set; }

		private EGateMode _LoadGateModeWithBackwardCompatibility(IEntityLoader entityLoader, PropertyKey<EGateMode> key,
			string label, EGateMode defaultMode)
		{
			try
			{
				return entityLoader.GetOrDefault(_persistenceKey, key, defaultMode);
			}
			catch (ArgumentException ex) when (ex.InnerException is ArgumentException)
			{
				string? stringValue = entityLoader.GetAsString(_persistenceKey, key);
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
				return defaultMode;
			}
		}

		public void Load(IEntityLoader entityLoader)
		{
			ActiveGateMode =
				_LoadGateModeWithBackwardCompatibility(entityLoader, _activeGateModeKey, "active", EGateMode.OPEN);
			InactiveGateMode =
				_LoadGateModeWithBackwardCompatibility(entityLoader, _inactiveGateModeKey, "inactive", EGateMode.CLOSE);

			try
			{
				_mainMode = entityLoader.GetRequired(_persistenceKey, _mainModeKey);
			}
			catch (IEntityLoaderExtensions.PersistenceException ex)
			{
				this.Log("Failed to load main mode value: {0}", ex);
				string mode = entityLoader.GetOrDefaultAsString(
					_persistenceKey,
					_mainModeKey,
					() => entityLoader.GetOrDefaultAsString(
						_persistenceKey,
						new PropertyKey<string>("_activationMode"),
						"Active"
					)
				).ToLower();
				switch (mode)
				{
					case "active":
						_mainMode = _GateModeToMainMode(ActiveGateMode);
						break;
					case "inactive":
						_mainMode = _GateModeToMainMode(InactiveGateMode);
						break;
					case "pass":
						_mainMode = EGateMainMode.PASS;
						break;
					case "automated":
						_mainMode = EGateMainMode.AUTOMATED;
						break;
					case "open":
						_mainMode = EGateMainMode.OPEN;
						break;
					case "close":
						_mainMode = EGateMainMode.CLOSE;
						break;
					default:
						this.Warn("Invalid loaded main mode value \"{0}\", falling back to open", mode);
						_mainMode = EGateMainMode.OPEN;
						StateNeedCheck = true;
						break;
				}
			}
		}

		public void Save(IEntitySaver entitySaver)
		{
			IObjectSaver? objectSaver = entitySaver.GetComponent(_persistenceKey);
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
				EGateState prevState = _currentGateState;
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