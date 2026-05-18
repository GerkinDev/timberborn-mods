using System.Linq;
using GerkinDev.PressurePlates.Components.LogicModes;
using GerkinDev.PressurePlates.Extensions;
using GerkinDev.PressurePlates.Services;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Illumination;
using Timberborn.WorldPersistence;

namespace GerkinDev.PressurePlates.Components
{
	internal class PressurePlate : BaseComponent, IAwakableComponent, IFinishedStateListener
	{
		private readonly OccupantDetectorService _occupantDetectorService;
		private IPressurePlateLogicMode _logicMode = new CountLatch();

		public PressurePlate(OccupantDetectorService occupantDetectorService)
		{
			_occupantDetectorService = occupantDetectorService;
			_SetupLogicMode(_logicMode);
		}

		private void _SetupLogicMode(IPressurePlateLogicMode mode)
		{
			_logicMode.ActiveChanged -= _OnActiveChanged;
			mode.ActiveChanged += _OnActiveChanged;
			_logicMode = mode;
		}

		public bool HasOccupant { get; private set; }

		private void _OnEnter(object sender, OccupantDetectorService.OccupancyEvent evt)
		{
			this.Log("Entered");
			_logicMode.OnEnter(evt);
			_OnChangeOccupancy(evt);
		}

		private void _OnExit(object sender, OccupantDetectorService.OccupancyEvent evt)
		{
			this.Log("Exited");
			_logicMode.OnExit(evt);
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
			if (_subscriber != null)
			{
				_subscriber.OnEnter -= _OnEnter;
				_subscriber.OnExit -= _OnExit;
				_subscriber = null;
			}
		}

		#endregion
	}
}