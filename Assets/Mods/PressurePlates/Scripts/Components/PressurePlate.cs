using GerkinDev.PressurePlates.Extensions;
using GerkinDev.PressurePlates.Services;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Illumination;

namespace GerkinDev.PressurePlates.Components
{
	internal class PressurePlate : BaseComponent, IAwakableComponent, IFinishedStateListener
	{
		private readonly OccupantDetectorService _occupantDetectorService;
		
		public bool HasOccupant { get; private set; }

		public PressurePlate(OccupantDetectorService occupantDetectorService)
		{
			_occupantDetectorService = occupantDetectorService;
		}
		#region IAwakableComponent
		private BlockObject _blockObject;
		private Illuminator _illuminator;

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
			_occupantDetectorService.Unsubscribe(this);
			if (_subscriber != null)
			{
				_subscriber.OnEnter -= _OnEnter;
				_subscriber.OnExit -= _OnExit;
				_subscriber = null;
			}
		}
		#endregion

		private void _OnEnter(object sender, OccupantDetectorService.OccupancyEvent evt)
		{
			this.Log("Entered");
			_OnChangeOccupancy(evt);
		}

		private void _OnExit(object sender, OccupantDetectorService.OccupancyEvent evt)
		{
			this.Log("Exited");
			_OnChangeOccupancy(evt);
		}

		private void _OnChangeOccupancy(OccupantDetectorService.OccupancyEvent evt)
		{
			HasOccupant = evt.Within.Any();
			_illuminator.Toggle(HasOccupant);
		}
	}
}
