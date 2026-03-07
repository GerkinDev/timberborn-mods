using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.Fields;
using Timberborn.Hauling;
using Timberborn.Navigation;
using Timberborn.Persistence;
using Timberborn.WaterSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts
{
	internal partial class DamWalkway : BaseComponent, IAwakableComponent, IPersistentEntity, IDeletableEntity
	{
		public DamWalkway(INavMeshService navMeshService, NavMeshGroupService navMeshGroupService, IWaterService waterService)
		{
			_gateNavMeshBlocker_navMeshService = navMeshService;
			_gateNavMeshBlocker_navMeshGroupService = navMeshGroupService;
			_waterObstacle_waterService = waterService;
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
					UpdateState();
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
		private DamWalkwaySpec _spec;
		private Automatable _automatable;
		private BlockObject _blockObject;

		public void Awake()
		{
			_ = typeof(Timberborn.WaterBuildings.Floodgate);
			_ = typeof(Timberborn.AutomationBuildings.Gate);
			_ = typeof(BlockObject);
			_ = typeof(FarmHouse);
			_ = typeof(HaulCandidate);
			//_ = typeof(TimbermeshPreviewFactory);
			_spec = GetComponent<DamWalkwaySpec>();
			_automatable = GetComponent<Automatable>();
			_blockObject = GetComponent<BlockObject>();
			_GateNavMeshBlocker_Awake();
		}
		#endregion

		#region IPersistentEntity
		private static readonly ComponentKey _persistenceKey = new("DamWalkway");
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

		public void UpdateState()
		{
			if (_blockObject.IsFinished)
			{
				if (_mode == EMode.Open || _IsOpenByAutomation)
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
		}
	}
}
