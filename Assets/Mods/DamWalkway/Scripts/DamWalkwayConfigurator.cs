using Bindito.Core;
using GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts.UI;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateInstantiation;

namespace GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts
{
	[Context("Game")]
	internal class DamWalkwayConfigurator : Configurator
	{
		protected override void Configure()
		{
			Bind<DamWalkwayFragment>().AsSingleton();
			MultiBind<EntityPanelModule>().ToProvider<DamWalkwayEntityPanelModuleProvider>().AsSingleton();

			Bind<DamWalkway>().AsTransient();
			Bind<CustomDynamicPathModel>().AsTransient();
			MultiBind<TemplateModule>().ToProvider(_ProvideTemplateModule).AsSingleton();
		}

		//class GateNavMeshBlockerInitializer : IDedicatedDecoratorInitializer<DamWalkway, GateNavMeshBlocker>
		//{
		//	public void Initialize(DamWalkway subject, GateNavMeshBlocker decorator)
		//	{
		//		decorator._
		//	}
		//}
		private static TemplateModule _ProvideTemplateModule()
		{
			var builder = new TemplateModule.Builder();
			builder.AddDecorator<DamWalkwaySpec, DamWalkway>();
			builder.AddDecorator<CustomDynamicPathModelSpec, CustomDynamicPathModel>();
			//builder.
			//builder.AddDedicatedDecorator<DamWalkwaySpec, GateNavMeshBlocker>(new GateNavMeshBlockerInitializer());
			return builder.Build();
		}
	}
}
