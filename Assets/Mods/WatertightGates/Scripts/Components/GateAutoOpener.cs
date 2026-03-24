using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.Specs;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.TimeSystem;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components
{
	internal class GateAutoOpener : BaseComponent, IAwakableComponent, IFinishedStateListener
	{
		private const float _ONE_HOUR = 1f / 24;
		private const float _CLOSE_DURATION = _ONE_HOUR / 4;
		private readonly OccupantDetectorService _occupantDetectorService;
		private readonly ITimeTriggerFactory _timeTriggerFactory;
		private readonly ITimeTrigger _closeTrigger;

		public GateAutoOpener(OccupantDetectorService occupantDetectorService, ITimeTriggerFactory timeTriggerFactory)
		{
			_occupantDetectorService = occupantDetectorService;
			_timeTriggerFactory = timeTriggerFactory;
			_closeTrigger = _timeTriggerFactory.Create(_DoClose, _CLOSE_DURATION);
		}

		#region IAwakableComponent
		private WatertightGateSpec _spec;
		private BlockObject _blockObject;
		private WatertightGateTransformController _gateTransformController;

		public void Awake()
		{
			_spec = GetComponent<WatertightGateSpec>();
			_blockObject = GetComponent<BlockObject>();
			_gateTransformController = GetComponent<WatertightGateTransformController>();
		}
		#endregion

		#region IFinishedStateListener
		public void OnEnterFinishedState()
		{
			_SubscribeToOccupants();
		}
		/// <summary>
		/// Emitted when destroying a finished building
		/// </summary>
		public void OnExitFinishedState()
		{
			_UnsubscribeToOccupants();
		}
		#endregion


		private OccupantDetectorService.Subscriber? _subscriber;
		private bool _isWatching;
		public bool IsWatching
		{
			get => _isWatching;
			set
			{
				_isWatching = value;
				if (value)
				{
					_SubscribeToOccupants();
				}
				else
				{
					_UnsubscribeToOccupants();
				}
			}
		}

		private bool _isOpen = false;
		private void _OnOccupantEnter(object sender, OccupantDetectorService.OccypancyEvent evt)
		{
			_closeTrigger.Reset();
			if (!_isOpen)
			{
				_isOpen = true;
				_gateTransformController.IsOpen = _isOpen;
			}
		}

		private void _OnOccupantExit(object sender, OccupantDetectorService.OccypancyEvent evt)
		{
			if (_isOpen && evt.Within.Length == 0)
			{
				_closeTrigger.Resume();
			}
		}

		private void _DoClose()
		{
			_closeTrigger.Reset();
			_isOpen = false;
			_gateTransformController.IsOpen = _isOpen;
		}

		private void _SubscribeToOccupants()
		{
			if (!_blockObject.IsFinished || !_isWatching)
			{
				return;
			}
			var blocks = _blockObject.PositionedBlocks.GetOccupiedBlocks().Select(b => b.Coordinates)
				.Append(_blockObject.TransformCoordinates(_spec.PathStart))
				.Append(_blockObject.TransformCoordinates(_spec.PathEnd))
				.ToArray();
			_subscriber = _occupantDetectorService.Subscribe(this, blocks);
			_subscriber.OnEnter += _OnOccupantEnter;
			_subscriber.OnExit += _OnOccupantExit;
		}

		private void _UnsubscribeToOccupants()
		{
			_isOpen = false;
			_closeTrigger.Reset();
			if (_subscriber == null)
			{
				return;
			}
			_subscriber.OnEnter -= _OnOccupantEnter;
			_subscriber.OnExit -= _OnOccupantExit;
			_occupantDetectorService.Unsubscribe(this);
			_subscriber = null;
		}
	}
}
