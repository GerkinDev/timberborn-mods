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
			Bind<DamServiceGateFragment>().AsSingleton();
			MultiBind<EntityPanelModule>().ToProvider<DamServiceGateEntityPanelModuleProvider>().AsSingleton();

			Bind<DamServiceGate>().AsTransient();
			Bind<CustomDynamicPathModel>().AsTransient();
			MultiBind<TemplateModule>().ToProvider(_ProvideTemplateModule).AsSingleton();
		}

		//class GateNavMeshBlockerInitializer : IDedicatedDecoratorInitializer<DamServiceGate, GateNavMeshBlocker>
		//{
		//	public void Initialize(DamServiceGate subject, GateNavMeshBlocker decorator)
		//	{
		//		decorator._
		//	}
		//}
		private static TemplateModule _ProvideTemplateModule()
		{
			var builder = new TemplateModule.Builder();
			builder.AddDecorator<DamServiceGateSpec, DamServiceGate>();
			builder.AddDecorator<CustomDynamicPathModelSpec, CustomDynamicPathModel>();
			//builder.
			//builder.AddDedicatedDecorator<DamServiceGateSpec, GateNavMeshBlocker>(new GateNavMeshBlockerInitializer());
			return builder.Build();
		}
	}
}
