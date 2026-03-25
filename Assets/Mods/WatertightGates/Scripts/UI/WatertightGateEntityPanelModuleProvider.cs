using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace GerkinDev.WatertightGates.UI
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
			var builder = new EntityPanelModule.Builder();
			builder.AddTopFragment(_fragment, -100);
			return builder.Build();
		}

	}
}
