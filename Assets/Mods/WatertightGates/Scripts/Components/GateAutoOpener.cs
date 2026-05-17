#region Optional dependencies

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GerkinDev.WatertightGates.Components.Specs;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.TimeSystem;
using OccupantDetectorService = GerkinDev.PressurePlates.Services.OccupantDetectorService;
using Subscriber = GerkinDev.PressurePlates.Services.OccupantDetectorService.Subscriber;
using OccupancyEvent = GerkinDev.PressurePlates.Services.OccupantDetectorService.OccupancyEvent;

#endregion

namespace GerkinDev.WatertightGates.Components
{
	/// <summary>
	///     Depends on optional <see cref="OccupantDetectorService" />
	/// </summary>
	internal class GateAutoOpener : BaseComponent, IAwakableComponent, IFinishedStateListener
	{
		private const float _ONE_HOUR = 1f / 24;
		private const float _CLOSE_DURATION = _ONE_HOUR / 4;
		private readonly ITimeTrigger _closeTrigger;
		private readonly object _occupantDetectorService;

		private bool _isOpen;
		private bool _isWatching;


		private object? _subscriber;

		public GateAutoOpener(OccupantDetectorService occupantDetectorService, ITimeTriggerFactory timeTriggerFactory)
		{
			_occupantDetectorService = occupantDetectorService;
			_closeTrigger = timeTriggerFactory.Create(_DoClose, _CLOSE_DURATION);
		}

		private OccupantDetectorService _OccupantDetectorService => (OccupantDetectorService)_occupantDetectorService;

		[NotNullIfNotNull(nameof(_subscriber))]
		private Subscriber? _Subscriber => (Subscriber?)_subscriber;

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

		private void _OnOccupantEnter(object sender, OccupancyEvent evt)
		{
			_closeTrigger.Reset();
			if (_isOpen)
			{
				return;
			}

			_isOpen = true;
			_gateTransformController.IsOpen = _isOpen;
		}

		private void _OnOccupantExit(object sender, OccupancyEvent evt)
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
			_subscriber = _OccupantDetectorService.Subscribe(this, blocks);
			_Subscriber.OnEnter += _OnOccupantEnter;
			_Subscriber.OnExit += _OnOccupantExit;
		}

		private void _UnsubscribeToOccupants()
		{
			_isOpen = false;
			_closeTrigger.Reset();
			if (_subscriber == null)
			{
				return;
			}

			_Subscriber.OnEnter -= _OnOccupantEnter;
			_Subscriber.OnExit -= _OnOccupantExit;
			_OccupantDetectorService.Unsubscribe(this);
			_subscriber = null;
		}

		#region IAwakableComponent

		private WatertightGateSpec _spec = null!;
		private BlockObject _blockObject = null!;
		private WatertightGateTransformController _gateTransformController = null!;

		public void Awake()
		{
			_spec = GetComponent<WatertightGateSpec>();
			_blockObject = GetComponent<BlockObject>();
			_gateTransformController = GetComponent<WatertightGateTransformController>();
		}

		#endregion

		#region IFinishedStateListener

		public void OnEnterFinishedState() => _SubscribeToOccupants();

		/// <summary>
		///     Emitted when destroying a finished building
		/// </summary>
		public void OnExitFinishedState() => _UnsubscribeToOccupants();

		#endregion
	}
}