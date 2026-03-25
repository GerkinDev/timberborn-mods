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
		}
		public void OnExitFinishedState()
		{
			_occupantDetectorService.Unsubscribe(this);
			_subscriber.OnEnter -= _OnEnter;
			_subscriber.OnExit -= _OnExit;
		}
		#endregion

		private void _OnEnter(object sender, OccupantDetectorService.OccypancyEvent evt) => _OnChangeOccupancy(evt);
		private void _OnExit(object sender, OccupantDetectorService.OccypancyEvent evt) => _OnChangeOccupancy(evt);
		private void _OnChangeOccupancy(OccupantDetectorService.OccypancyEvent evt)
		{
			_illuminator.Toggle(evt.Within.Any());
		}
	}
}
