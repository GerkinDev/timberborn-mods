using System;
using GerkinDev.WatertightGates.Components.Specs;
using GerkinDev.WatertightGates.Extensions;
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
				if (_isOpen == value)
				{
					return;
				}

				_isOpen = value;
				_UpdateState();
			}
		}

		public void SetImmediateOpen(bool open)
		{
			var prevAnimate = _tickerComponent.Animate;
			IsOpen = open;
			_tickerComponent.Animate = false;
			_tickerComponent.DoUpdate(true);
			_tickerComponent.Animate = prevAnimate;
		}

		#region IInitializableEntity

		public void InitializeEntity() => _UpdateState();

		#endregion

		private void _UpdateState()
		{
			if (_blockObject.IsFinished)
			{
				_waterBlocker.Height = _tickerComponent.ActuallyOpen ?? true ? 0 : 1;
			}

			_tickerComponent.Animate = _blockObject.IsFinished;
			_tickerComponent.TargetOpen = _isOpen;
		}

		#region IAwakableComponent

		private WatertightGateSpec _spec = null!;
		private BlockObject _blockObject = null!;
		private Transform _anchor = null!;
		private WaterBlocker _waterBlocker = null!;
		private TickerComponent _tickerComponent = null!;

		public void Awake()
		{
			_spec = GetComponent<WatertightGateSpec>();
			_blockObject = GetComponent<BlockObject>();
			_anchor = GameObject.FindChildTransform(_spec.Anchor);
			_tickerComponent = GameObject.AddComponent<TickerComponent>().Initialize(this, _spec, _anchor);
			_waterBlocker = GetComponent<WaterBlocker>();
		}

		#endregion

		#region IFinishedStateListener

		public void OnEnterFinishedState()
		{
			_tickerComponent.Animate = true;
			_UpdateState();
		}

		public void OnExitFinishedState()
		{
			_tickerComponent.Animate = false;
			IsOpen = true;
		}

		#endregion

		private class TickerComponent : MonoBehaviour
		{
			public WatertightGateSpec Spec { get; private set; } = null!;
			public WatertightGateTransformController Owner { get; private set; } = null!;
			public Transform TargetTransform { get; private set; } = null!;
			private float _openFrac = 0f;
			private bool _targetOpen;
			public bool Animate { get; set; }
			public bool? ActuallyOpen { get; private set; }

			public bool TargetOpen
			{
				get => _targetOpen;
				set
				{
					enabled = true;
					_targetOpen = value;
				}
			}

			private Vector3 _openPosition;
			private Quaternion _openRotation;
			private Vector3 _closePosition;
			private Quaternion _closeRotation;

			public TickerComponent Initialize(WatertightGateTransformController owner,
				WatertightGateSpec spec, Transform target)
			{
				enabled = false;
				Owner = owner;
				Spec = spec;
				TargetTransform = target;
				_openPosition = target.transform.position + spec.OpenTransform.Position;
				_openRotation = Quaternion.Euler(target.transform.rotation.eulerAngles + spec.OpenTransform.Rotation);
				_closePosition = target.transform.position + spec.CloseTransform.Position;
				_closeRotation = Quaternion.Euler(target.transform.rotation.eulerAngles + spec.CloseTransform.Rotation);
				return this;
			}

			public void Update() => DoUpdate(false);

			public void DoUpdate(bool force = false)
			{
				if (!force && Animate && Time.deltaTime == 0)
				{
					return;
				}
				var finalOpen = TargetOpen ? 1f : 0f;
				if (Animate)
				{
					var deltaTimeFrac = Time.deltaTime / (TargetOpen ? Spec.OpenTime : Spec.CloseTime);
					_openFrac = Mathf.Clamp(_openFrac + deltaTimeFrac * (TargetOpen ? 1 : -1), 0f, 1f);
				}
				else
				{
					_openFrac = finalOpen;
				}

				if (Mathf.Approximately(_openFrac, finalOpen))
				{
					enabled = false;
					_openFrac = finalOpen;
					ActuallyOpen = TargetOpen;
					Owner._UpdateState();
				}
				else
				{
					ActuallyOpen = null;
				}

				var currentPosition = Vector3.Lerp(_closePosition, _openPosition, _openFrac);
				var currentRotation = Quaternion.Lerp(_closeRotation, _openRotation, _openFrac);
				TargetTransform.SetLocalPositionAndRotation(currentPosition, currentRotation);
			}
		}
	}
}