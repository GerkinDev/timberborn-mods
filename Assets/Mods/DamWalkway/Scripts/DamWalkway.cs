using Timberborn.AutomationBuildings;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.WaterBuildings;

namespace GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts
{
	internal class DamWalkway : BaseComponent, IAwakableComponent//, IPersistentEntity
	{
		internal enum EState
		{
			Open = 0b01,
			Close = 0b10,
			Pass = Open | Close,
			Automated = 0b00,
		}
		private DamWalkwaySpec _spec;
		public EState state = EState.Open;

		//public void Load(IEntityLoader entityLoader)
		//{
		//    throw new System.NotImplementedException();
		//}

		//public void Save(IEntitySaver entitySaver)
		//{
		//    throw new System.NotImplementedException();
		//}
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
