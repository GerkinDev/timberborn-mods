using System.Linq;
using GerkinDev.PressurePlates.Components.LogicModes;
using GerkinDev.PressurePlates.Extensions;
using GerkinDev.PressurePlates.Services;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Illumination;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;

namespace GerkinDev.PressurePlates.Components
{
	internal class PressurePlate : BaseComponent, IAwakableComponent, IFinishedStateListener, IPersistentEntity
	{
		private static readonly ComponentKey _persistenceKey = new(nameof(PressurePlate));
		private readonly LogicModeSerializer _logicModeSerializer;
		private readonly OccupantDetectorService _occupantDetectorService;
		public IPressurePlateLogicMode LogicMode { get; private set; } = new CountLatch();
		private readonly PropertyKey<string> _logicModeKey = new(nameof(LogicMode));

		public PressurePlate(OccupantDetectorService occupantDetectorService, LogicModeSerializer logicModeSerializer)
		{
			_occupantDetectorService = occupantDetectorService;
			_logicModeSerializer = logicModeSerializer;
		}

		public bool HasOccupant { get; private set; }

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
			HasOccupant = evt.Within.Any();
			_illuminator.Toggle(HasOccupant);
		}

		private void _OnActiveChanged(object sender, bool active)
		{
			this.Log("Active changed");
			_illuminator.Toggle(active);
		}

		#region IAwakableComponent

		private BlockObject _blockObject = null!;
		private Illuminator _illuminator = null!;

		public void Awake()
		{
			_blockObject = GetComponent<BlockObject>();
			_illuminator = GetComponent<Illuminator>();
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