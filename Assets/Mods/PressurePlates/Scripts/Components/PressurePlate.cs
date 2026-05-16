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

		public PressurePlate(OccupantDetectorService occupantDetectorService)
		{
			_occupantDetectorService = occupantDetectorService;
		}

		private void _OnEnter(object sender, OccupantDetectorService.OccupancyEvent evt) => _OnChangeOccupancy(evt);
		private void _OnExit(object sender, OccupantDetectorService.OccupancyEvent evt) => _OnChangeOccupancy(evt);

		private void _OnChangeOccupancy(OccupantDetectorService.OccupancyEvent evt) =>
			_illuminator.Toggle(evt.Within.Any());

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
	}
}