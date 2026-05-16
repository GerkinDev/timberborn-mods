using GerkinDev.WatertightGates.Components.Specs;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.EntitySystem;
using UnityEngine;

namespace GerkinDev.WatertightGates.Components
{
	internal class WatertightGateTransformController : BaseComponent, IAwakableComponent, IFinishedStateListener,
		IInitializableEntity
	{
		private bool _isOpen;

		public bool IsOpen
		{
			get => _isOpen;
			set
			{
				if (_isOpen != value)
				{
					_isOpen = value;
					_UpdateState();
				}
			}
		}

		#region IInitializableEntity

		public void InitializeEntity() => _UpdateState();

		#endregion

		private void _UpdateState()
		{
			if (_blockObject.IsFinished)
			{
				_waterBlocker.Height = _isOpen ? 0 : 1;
			}

			GateTransformSpec transform = _isOpen ? _spec.OpenTransform : _spec.CloseTransform;
			_anchor.transform.SetLocalPositionAndRotation(transform.Position + _anchorInitialPosition,
				Quaternion.Euler(transform.Rotation + _anchorInitialRotation));
		}

		#region IAwakableComponent

		private WatertightGateSpec _spec = null!;
		private BlockObject _blockObject = null!;
		private Transform _anchor = null!;
		private Vector3 _anchorInitialRotation;
		private Vector3 _anchorInitialPosition;
		private WaterBlocker _waterBlocker = null!;

		public void Awake()
		{
			_spec = GetComponent<WatertightGateSpec>();
			_blockObject = GetComponent<BlockObject>();
			_anchor = GameObject.FindChildTransform(_spec.Anchor);
			_anchorInitialRotation = _anchor.transform.rotation.eulerAngles;
			_anchorInitialPosition = _anchor.transform.position;
			_waterBlocker = GetComponent<WaterBlocker>();
		}

		#endregion

		#region IFinishedStateListener

		public void OnEnterFinishedState() => _UpdateState();

		public void OnExitFinishedState() => IsOpen = true;

		#endregion
	}
}