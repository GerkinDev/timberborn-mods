using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts.UI
{
	internal class DamWalkwayEntityPanelModuleProvider : IProvider<EntityPanelModule>
	{

		private readonly DamWalkwayFragment _fragment;

		public DamWalkwayEntityPanelModuleProvider(DamWalkwayFragment fragment)
		{
			_fragment = fragment;
		}

		public EntityPanelModule Get()
		{
			var builder = new EntityPanelModule.Builder();
			builder.AddTopFragment(_fragment, -100);
			return builder.Build();
		}

	}
}
