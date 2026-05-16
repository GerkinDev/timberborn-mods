using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.UI
{
	internal class WatertightGateEntityPanelModuleProvider : IProvider<EntityPanelModule>
	{
		private readonly WatertightGateFragment _fragment;

		public WatertightGateEntityPanelModuleProvider(WatertightGateFragment fragment)
		{
			_fragment = fragment;
		}

		public EntityPanelModule Get()
		{
			EntityPanelModule.Builder builder = new();
			builder.AddTopFragment(_fragment, -100);
			return builder.Build();
		}
	}
}