using GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts.Extensions;
using System;
using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts
{
	internal class DamServiceGate : BaseComponent, IAwakableComponent, IPersistentEntity, IFinishedStateListener, IAutomatableNeeder, ITerminal, IInitializableEntity, IGateLike, IPreInitializableEntity
	{
		internal enum EMode
		{
			Open = 0b01,
			Close = 0b10,
			Pass = Open | Close,
			Automated = 0b00,
		}
		private EMode _mode = EMode.Open;
		public EMode Mode
		{
			get => _mode; set
			{
				if (_mode != value)
				{
					_mode = value;
					_ScheduleStateUpdate();
				}
			}
		}
		private bool _IsOpenByAutomation
		{
			get
			{
				if (Mode == EMode.Automated)
				{
					return _automatable.State != ConnectionState.Off;
				}

				return false;
			}
		}
		public bool IsConflict { get; private set; }
		public event EventHandler StateChanged;

		private readonly GateLikeUpdater _gateLikeUpdater;

		public DamServiceGate(GateLikeUpdater gateLikeUpdater)
		{
			_gateLikeUpdater = gateLikeUpdater;
		}

		#region IAwakableComponent
		private DamServiceGateSpec _spec;
		private Automatable _automatable;
		private BlockObject _blockObject;
		private NavMeshBlocker _navMeshBlocker;
		private WaterBlocker _waterBlocker;
		private Transform _anchor;

		public void Awake()
		{
			_spec = GetComponent<DamServiceGateSpec>();
			_automatable = GetComponent<Automatable>();
			_blockObject = GetComponent<BlockObject>(); /// Position is not initialized yet. See <see cref="PreInitializeEntity"/> for transforms
			_navMeshBlocker = GetComponent<NavMeshBlocker>();
			_waterBlocker = GetComponent<WaterBlocker>();
			_anchor = GameObject.FindChildTransform(_spec.Anchor);
		}
		#endregion

		#region IPersistentEntity
		private static readonly ComponentKey _persistenceKey = new("DamServiceGate");
		private static readonly PropertyKey<EMode> _modeKey = new(nameof(_mode));

		public void Load(IEntityLoader entityLoader)
		{
			Mode = entityLoader.GetOrDefault(_persistenceKey, _modeKey, EMode.Open);
		}

		public void Save(IEntitySaver entitySaver)
		{
			entitySaver.GetComponent(_persistenceKey).Set(_modeKey, _mode);
		}
		#endregion

		#region IFinishedStateListener
		public void OnEnterFinishedState()
		{
			_ScheduleStateUpdate();
		}

		public void OnExitFinishedState()
		{
		}
		#endregion

		#region IAutomatableNeeder
		public bool NeedsAutomatable => Mode == EMode.Automated;
		#endregion

		#region ITerminal
		public void Evaluate()
		{
			if (NeedsAutomatable)
			{
				_ScheduleStateUpdate();
			}
		}
		#endregion

		#region IInitializableEntity
		public void InitializeEntity()
		{
			_ScheduleStateUpdate();
			Close();
		}
		#endregion

		#region IGateLike
		public bool IsClosed { get; private set; }
		public Vector3Int PathStart { get; private set; }
		public Vector3Int PathEnd { get; private set; }
		public Vector3Int PathCenter { get; private set; }

		public void Close()
		{
			IsClosed = true;
			_UpdateState();
		}

		public void Open()
		{
			IsClosed = false;
			_UpdateState();
		}

		public void EnableConflict()
		{
			IsConflict = true;
			_NotifyStateChanged();
		}

		public void DisableConflict()
		{
			IsConflict = false;
			_NotifyStateChanged();
		}
		#endregion
		#region IPreInitializableEntity
		public void PreInitializeEntity()
		{
			PathStart = _blockObject.TransformCoordinates(_spec.PathStart);
			PathEnd = _blockObject.TransformCoordinates(_spec.PathEnd);
			PathCenter = _blockObject.TransformCoordinates(_spec.PathCenter);
		}
		#endregion

		private bool _WantOpen => _mode == EMode.Open || _IsOpenByAutomation;
		private void _ScheduleStateUpdate()
		{
			if (_blockObject.IsFinished)
			{
				this.Log("Scheduling gate for desired {0}", _WantOpen ? "open" : "close");
				if (_WantOpen)
				{
					_gateLikeUpdater.ScheduleToOpen(this);
				}
				else
				{
					_gateLikeUpdater.ScheduleToClose(this);
				}
			}
		}
		private void _UpdateState()
		{
			this.Log("Set gate {0}, desired {1}", IsClosed ? "close" : "open", _WantOpen ? "open" : "close");
			_navMeshBlocker.NavMeshBlocked = IsClosed;
			if (_blockObject.IsFinished)
			{
				_waterBlocker.Height = IsClosed ? 1 : 0;
			}
			_SetAnchorTransform(IsClosed ? _spec.CloseTransform : _spec.OpenTransform);
		}

		private void _SetAnchorTransform(GateTransformSpec transform)
		{
			_anchor.transform.SetLocalPositionAndRotation(transform.Position, Quaternion.Euler(transform.Rotation));
		}

		private void _NotifyStateChanged()
		{
			StateChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
