using Bindito.Core;
using GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts.UI;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;

namespace GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts
{
	[Context("Game")]
	internal partial class DamWalkwayConfigurator : Configurator
	{
		protected override void Configure()
		{
			Bind<DamWalkwayFragment>().AsSingleton();
			MultiBind<EntityPanelModule>().ToProvider<DamWalkwayEntityPanelModuleProvider>().AsSingleton();

			Bind<DamWalkway>().AsTransient();
			MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
		}
		private static TemplateModule ProvideTemplateModule()
		{
			var builder = new TemplateModule.Builder();
			builder.AddDecorator<DamWalkwaySpec, DamWalkway>();
			return builder.Build();
		}
	}
}
