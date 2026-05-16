using Bindito.Core;
using GerkinDev.WatertightGates.Components;
using GerkinDev.WatertightGates.Components.Specs;
using GerkinDev.WatertightGates.Components.UI;
using GerkinDev.WatertightGates.Services;
using GerkinDev.WatertightGates.UI;
using Timberborn.EntityPanelSystem;
using Timberborn.Illumination;
using Timberborn.TemplateInstantiation;

namespace GerkinDev.WatertightGates
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
			Bind<WatertightGateTransformController>().AsTransient();
			Bind<NavMeshBlocker>().AsTransient();
			Bind<WaterBlocker>().AsTransient();
			Bind<WatertightGateConflictStatus>().AsTransient();
			Bind<FreePositionedDynamicPathModel>().AsTransient();
			MultiBind<TemplateModule>().ToProvider(_ProvideTemplateModule).AsSingleton();
		}

		private static TemplateModule _ProvideTemplateModule()
		{
			TemplateModule.Builder builder = new();
			builder.AddDecorator<WatertightGateSpec, WatertightGateTransformController>();
			builder.AddDecorator<WatertightGateTransformController, WaterBlocker>();
			builder.AddDecorator<WatertightGateSpec, WatertightGate>();
			builder.AddDecorator<WatertightGate, NavMeshBlocker>();
			builder.AddDecorator<WatertightGate, WatertightGateConflictStatus>();
			builder.AddDecorator<WatertightGate, Illuminator>();
			builder.AddDecorator<FreePositionedDynamicPathModelSpec, FreePositionedDynamicPathModel>();
			return builder.Build();
		}
	}
}