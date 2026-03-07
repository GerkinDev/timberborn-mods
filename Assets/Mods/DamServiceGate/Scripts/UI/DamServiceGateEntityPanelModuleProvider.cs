using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts.UI
{
	internal class DamServiceGateEntityPanelModuleProvider : IProvider<EntityPanelModule>
	{

		private readonly DamServiceGateFragment _fragment;

		public DamServiceGateEntityPanelModuleProvider(DamServiceGateFragment fragment)
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
