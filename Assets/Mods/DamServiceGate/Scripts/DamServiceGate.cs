using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.EntitySystem;
using Timberborn.Fields;
using Timberborn.Hauling;
using Timberborn.Navigation;
using Timberborn.PathSystem;
using Timberborn.Persistence;
using Timberborn.WaterSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts
{
	internal partial class DamServiceGate : BaseComponent, IAwakableComponent, IPersistentEntity, IDeletableEntity, IFinishedStateListener, IAutomatableNeeder, ITerminal, IInitializableEntity
	{
		public DamServiceGate(INavMeshService navMeshService, NavMeshGroupService navMeshGroupService, IWaterService waterService, IPathService pathService)
		{
			_gateNavMeshBlocker_navMeshService = navMeshService;
			_gateNavMeshBlocker_navMeshGroupService = navMeshGroupService;
			_waterObstacle_waterService = waterService;
			_gateNavMeshBlocker_pathService = pathService;
		}

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
					_UpdateState();
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

		#region IAwakableComponent
		private DamServiceGateSpec _spec;
		private Automatable _automatable;
		private BlockObject _blockObject;
		private Transform _anchor;

		public void Awake()
		{
			_ = typeof(Timberborn.WaterBuildings.Floodgate);
			_ = typeof(Timberborn.AutomationBuildings.Gate);
			_ = typeof(BlockObject);
			_ = typeof(FarmHouse);
			_ = typeof(HaulCandidate);
			//_ = typeof(Suspension);
			//_ = typeof(TimbermeshPreviewFactory);
			_spec = GetComponent<DamServiceGateSpec>();
			_automatable = GetComponent<Automatable>();
			_blockObject = GetComponent<BlockObject>();
			_anchor = GameObject.FindChildTransform(_spec.Anchor);
			_GateNavMeshBlocker_Awake();
		}
		#endregion

		#region IPersistentEntity
		private static readonly ComponentKey _persistenceKey = new("DamServiceGate");
		private static readonly PropertyKey<EMode> _modeKey = new(nameof(_mode));

		public void Load(IEntityLoader entityLoader)
		{
			if (entityLoader.TryGetComponent(_persistenceKey, out var objectLoader))
			{
				_mode = objectLoader.Get(_modeKey);
			}
		}

		public void Save(IEntitySaver entitySaver)
		{
			entitySaver.GetComponent(_persistenceKey).Set(_modeKey, _mode);
		}
		#endregion

		#region IDeletableEntity
		public void DeleteEntity()
		{
			_GateNavMeshBlocker_DeleteEntity();
		}
		#endregion

		#region IFinishedStateListener
		public void OnEnterFinishedState()
		{
			_UpdateState();
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
				_UpdateState();
			}
		}
		#endregion

		#region IInitializableEntity
		public void InitializeEntity()
		{
			_UpdateState();
		}
		#endregion

		private bool _IsOpen => _mode == EMode.Open || _IsOpenByAutomation;
		private void _UpdateState()
		{
			if (_blockObject.IsFinished)
			{
				if (_IsOpen)
				{
					Debug.Log("Schedule to open");
					_GateNavMeshBlocker_Unblock();
					_WaterObstacle_SetObstacleHeight(0);
					//_gateUpdater.ScheduleToOpen(this);
				}
				else
				{
					Debug.Log("Schedule to close");
					_GateNavMeshBlocker_Block();
					_WaterObstacle_SetObstacleHeight(1);
					//_gateUpdater.ScheduleToClose(this);
				}
			}
			if (_IsOpen)
			{
				_SetAnchorTransform(_spec.OpenTransform);
			}
			else
			{
				_SetAnchorTransform(_spec.CloseTransform);
			}
		}
		private void _SetAnchorTransform(GateTransformSpec transform)
		{
			_anchor.transform.SetLocalPositionAndRotation(transform.Position, Quaternion.Euler(transform.Rotation));
		}
	}
}
