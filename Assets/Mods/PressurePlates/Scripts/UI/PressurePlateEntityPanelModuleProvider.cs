using Bindito.Core;
using Timberborn.EntityPanelSystem;

namespace GerkinDev.PressurePlates.UI
{
	internal class PressurePlateEntityPanelModuleProvider : IProvider<EntityPanelModule>
	{
		private readonly PressurePlateFragment _fragment;

		public PressurePlateEntityPanelModuleProvider(PressurePlateFragment fragment)
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