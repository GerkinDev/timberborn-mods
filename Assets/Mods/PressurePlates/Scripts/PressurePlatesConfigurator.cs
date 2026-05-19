using Bindito.Core;
using GerkinDev.PressurePlates.Components;
using GerkinDev.PressurePlates.Components.Spec;
using GerkinDev.PressurePlates.Services;
using GerkinDev.PressurePlates.UI;
using Timberborn.EntityPanelSystem;
using Timberborn.Illumination;
using Timberborn.TemplateInstantiation;
using CountLatch = GerkinDev.PressurePlates.LogicModes.CountLatch;

namespace GerkinDev.PressurePlates
{
	[Context("Game")]
	internal class PressurePlatesConfigurator : Configurator
	{
		protected override void Configure()
		{
			Bind<PressurePlateFragment>().AsSingleton();
			Bind<CountLatch.Fragment>().AsSingleton();
			MultiBind<EntityPanelModule>().ToProvider<PressurePlateEntityPanelModuleProvider>().AsSingleton();

			Bind<ModInfo>().ToInstance(PressurePlatesModStarter.ModInfo);
			Bind<LogicModeSerializer>().AsSingleton();
			Bind<PressurePlateVersionService>().AsSingleton();
			Bind<OccupantDetectorService>().AsSingleton();
			Bind<PressurePlate>().AsTransient();
			MultiBind<TemplateModule>().ToProvider(() =>
			{
				TemplateModule.Builder builder = new();
				builder.AddDecorator<PressurePlateSpec, PressurePlate>();
				builder.AddDecorator<PressurePlate, CustomizableIlluminator>();
				return builder.Build();
			}).AsSingleton();
		}
	}
}