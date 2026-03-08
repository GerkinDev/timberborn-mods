using Bindito.Core;
using GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts.UI;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts
{
	[Context("Game")]
	internal class DamServiceGateConfigurator : Configurator
	{
		protected override void Configure()
		{
			Bind<GateLikeUpdater>().AsSingleton();

			Bind<DamServiceGateFragment>().AsSingleton();
			MultiBind<EntityPanelModule>().ToProvider<DamServiceGateEntityPanelModuleProvider>().AsSingleton();

			Bind<DamServiceGate>().AsTransient();
			Bind<NavMeshBlocker>().AsTransient();
			Bind<WaterBlocker>().AsTransient();
			Bind<DamServiceGateConflictStatus>().AsTransient();
			Bind<CustomDynamicPathModel>().AsTransient();
			MultiBind<TemplateModule>().ToProvider(_ProvideTemplateModule).AsSingleton();
		}

		private static TemplateModule _ProvideTemplateModule()
		{
			var builder = new TemplateModule.Builder();
			builder.AddDecorator<DamServiceGateSpec, DamServiceGate>();
			builder.AddDecorator<DamServiceGate, NavMeshBlocker>();
			builder.AddDecorator<DamServiceGate, WaterBlocker>();
			builder.AddDecorator<DamServiceGate, DamServiceGateConflictStatus>();
			builder.AddDecorator<CustomDynamicPathModelSpec, CustomDynamicPathModel>();
			return builder.Build();
		}
	}
}
