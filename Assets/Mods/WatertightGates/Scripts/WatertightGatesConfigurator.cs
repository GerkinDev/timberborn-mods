using Bindito.Core;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.UI;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts
{
	[Context("Game")]
	internal class WatertightGatesConfigurator : Configurator
	{
		protected override void Configure()
		{
			Bind<GateLikeUpdater>().AsSingleton();

			Bind<WatertightGateFragment>().AsSingleton();
			MultiBind<EntityPanelModule>().ToProvider<WatertightGateEntityPanelModuleProvider>().AsSingleton();

			Bind<WatertightGate>().AsTransient();
			Bind<NavMeshBlocker>().AsTransient();
			Bind<WaterBlocker>().AsTransient();
			Bind<WatertightGateConflictStatus>().AsTransient();
			Bind<FreePositionedDynamicPathModel>().AsTransient();
			MultiBind<TemplateModule>().ToProvider(_ProvideTemplateModule).AsSingleton();
		}

		private static TemplateModule _ProvideTemplateModule()
		{
			var builder = new TemplateModule.Builder();
			builder.AddDecorator<WatertightGateSpec, WatertightGate>();
			builder.AddDecorator<WatertightGate, NavMeshBlocker>();
			builder.AddDecorator<WatertightGate, WaterBlocker>();
			builder.AddDecorator<WatertightGate, WatertightGateConflictStatus>();
			builder.AddDecorator<FreePositionedDynamicPathModelSpec, FreePositionedDynamicPathModel>();
			return builder.Build();
		}
	}
}
