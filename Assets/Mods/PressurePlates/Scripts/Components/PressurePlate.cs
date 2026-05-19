using System.Linq;
using GerkinDev.PressurePlates.Components.LogicModes;
using GerkinDev.PressurePlates.Extensions;
using GerkinDev.PressurePlates.Services;
using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Illumination;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace GerkinDev.PressurePlates.Components
{
	internal class PressurePlate : BaseComponent, IAwakableComponent, IFinishedStateListener, IPersistentEntity,
		ITransmitter
	{
		private static readonly ComponentKey _persistenceKey = new(nameof(PressurePlate));
		private readonly PropertyKey<string> _logicModeKey = new(nameof(LogicMode));
		private readonly LogicModeSerializer _logicModeSerializer;
		private readonly OccupantDetectorService _occupantDetectorService;

		private bool _hasOccupant;

		public PressurePlate(OccupantDetectorService occupantDetectorService, LogicModeSerializer logicModeSerializer)
		{
			_occupantDetectorService = occupantDetectorService;
			_logicModeSerializer = logicModeSerializer;
		}

		public IPressurePlateLogicMode LogicMode { get; private set; } = new CountLatch();

		private void _SetupLogicMode(IPressurePlateLogicMode mode)
		{
			LogicMode.ActiveChanged -= _OnActiveChanged;
			mode.ActiveChanged += _OnActiveChanged;
			LogicMode = mode;
		}

		private void _OnEnter(object sender, OccupantDetectorService.OccupancyEvent evt)
		{
			this.Log("Entered");
			LogicMode.OnEnter(evt);
			_OnChangeOccupancy(evt);
		}

		private void _OnExit(object sender, OccupantDetectorService.OccupancyEvent evt)
		{
			this.Log("Exited");
			LogicMode.OnExit(evt);
			_OnChangeOccupancy(evt);
		}

		private void _OnChangeOccupancy(OccupantDetectorService.OccupancyEvent evt)
		{
			_hasOccupant = evt.Within.Any();
			_UpdateIlluminator();
		}

		private void _OnActiveChanged(object sender, bool active)
		{
			this.Log("Active changed: {0}", active);
			_automator.SetState(active);
			_UpdateIlluminator();
		}

		private void _UpdateIlluminator()
		{
			var active = _automator.State == AutomatorState.On;
			if (!active && !_hasOccupant)
			{
				_illuminator.Toggle(false);
				return;
			}

			var strength = Mathf.Clamp((active ? 0.67f : 0) + (_hasOccupant ? 0.33f : 0), 0, 1);
			_illuminator.SetStrength(strength);
			_illuminator.Toggle(true);
		}

		#region IAwakableComponent

		private BlockObject _blockObject = null!;
		private Illuminator _illuminator = null!;
		private Automator _automator = null!;

		public void Awake()
		{
			_blockObject = GetComponent<BlockObject>();
			_illuminator = GetComponent<Illuminator>();
			_automator = GetComponent<Automator>();
		}

		#endregion

		#region IFinishedStateListener

		private OccupantDetectorService.Subscriber? _subscriber;

		public void OnEnterFinishedState()
		{
			_subscriber = _occupantDetectorService.Subscribe(this, _blockObject);
			_subscriber.OnEnter += _OnEnter;
			_subscriber.OnExit += _OnExit;
			_occupantDetectorService.ScanImmediate(this);
		}

		public void OnExitFinishedState()
		{
			if (_subscriber is null)
			{
				return;
			}

			_occupantDetectorService.Unsubscribe(this);

			_subscriber.OnEnter -= _OnEnter;
			_subscriber.OnExit -= _OnExit;
			_subscriber = null;
		}

		#endregion

		#region IPersistentEntity

		public void Save(IEntitySaver entitySaver)
		{
			var serializedLogicMode = _logicModeSerializer.Serialize(LogicMode);
			var objectSaver = entitySaver.GetComponent(_persistenceKey);
			objectSaver.Set(_logicModeKey, serializedLogicMode);
		}

		public void Load(IEntityLoader entityLoader)
		{
			var serializedLogicMode = entityLoader.GetAsString(_persistenceKey, _logicModeKey);
			var logicMode = serializedLogicMode is null
				? LogicMode
				: _logicModeSerializer.Deserialize(serializedLogicMode) ?? LogicMode;
			_SetupLogicMode(logicMode);
		}

		#endregion
	}
}