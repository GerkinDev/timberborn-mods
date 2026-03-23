using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.Specs;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Extensions;
using System;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components
{
	internal class GateAutoOpener : BaseComponent, IAwakableComponent, IDeletableEntity, IFinishedStateListener, IDisposable
	{
		private readonly OccupantDetectorService _occupantDetectorService;

		public GateAutoOpener(OccupantDetectorService occupantDetectorService)
		{
			this.Log("Init GateAutoOpener");
			_occupantDetectorService = occupantDetectorService;
		}

		#region IAwakableComponent
		private WatertightGateSpec _spec;
		private BlockObject _blockObject;

		public void Awake()
		{
			_spec = GetComponent<WatertightGateSpec>();
			_blockObject = GetComponent<BlockObject>();
		}
		#endregion

		#region IDeletableEntity
		public void DeleteEntity()
		{
			_DebugDispose(nameof(DeleteEntity));
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
			_DebugDispose(nameof(OnExitFinishedState));
		}
		#endregion

		#region IDisposable
		private bool _disposed;
		public void Dispose()
		{
			_DebugDispose(nameof(Dispose));
		}
		private void _DebugDispose(string from)
		{
			this.Log("Disposing from {0}, subscriber: {1}, disposed: {2}", from, _subscriber, _disposed);
			_subscriber.OnEnter -= _OnOccupantEnter;
			_subscriber.OnExit -= _OnOccupantExit;
			_occupantDetectorService.Unsubscribe(this);
			_subscriber = null;
			_disposed = true;
		}
		#endregion

		private bool _isOpen = false;
		private void _OnOccupantEnter(object sender, OccupantDetectorService.OccypancyEvent evt)
		{
			this.Log("Enter");
			if (!_isOpen)
			{
				_isOpen = true;
				this.Log("Open");
			}
		}

		private void _OnOccupantExit(object sender, OccupantDetectorService.OccypancyEvent evt)
		{
			this.Log("Exit");
			if (_isOpen && evt.Within.Length == 0)
			{
				_isOpen = false;
				this.Log("Close");
			}
		}

		private OccupantDetectorService.Subscriber? _subscriber;
		private void _SubscribeToOccupants()
		{
			this.Log("Subscribing");
			var blocks = _blockObject.PositionedBlocks.GetOccupiedBlocks().Select(b => b.Coordinates)
				.Append(_blockObject.TransformCoordinates(_spec.PathStart))
				.Append(_blockObject.TransformCoordinates(_spec.PathEnd))
				.ToArray();
			_subscriber = _occupantDetectorService.Subscribe(this, blocks);
			_subscriber.OnEnter += _OnOccupantEnter;
			_subscriber.OnExit += _OnOccupantExit;
		}
	}
}
