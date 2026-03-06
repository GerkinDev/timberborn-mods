using Timberborn.AutomationBuildings;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Persistence;
using Timberborn.WaterBuildings;
using Timberborn.WorldPersistence;

namespace GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts
{
	internal class DamWalkway : BaseComponent, IAwakableComponent, IPersistentEntity
	{
		private static readonly ComponentKey _persistenceKey = new("DamWalkway");
		private static readonly PropertyKey<EState> _stateKey = new(nameof(state));
		internal enum EState
		{
			Open = 0b01,
			Close = 0b10,
			Pass = Open | Close,
			Automated = 0b00,
		}
		private DamWalkwaySpec _spec;
		public EState state = EState.Open;

		public void Load(IEntityLoader entityLoader)
		{
			if (entityLoader.TryGetComponent(_persistenceKey, out var objectLoader))
			{
				state = objectLoader.Get(_stateKey);
			}
		}

		public void Save(IEntitySaver entitySaver)
		{
			entitySaver.GetComponent(_persistenceKey).Set(_stateKey, state);
		}

		public void Awake()
		{
			_ = typeof(Floodgate);
			_ = typeof(Gate);
			_ = typeof(BlockObject);
			//_ = typeof(TimbermeshPreviewFactory);
			_spec = GetComponent<DamWalkwaySpec>();
		}
	}
}
