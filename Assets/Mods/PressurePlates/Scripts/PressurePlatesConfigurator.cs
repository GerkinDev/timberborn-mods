using Bindito.Core;
using GerkinDev.PressurePlates.Components;
using GerkinDev.PressurePlates.Components.Spec;
using GerkinDev.PressurePlates.Services;
using Timberborn.Illumination;
using Timberborn.TemplateInstantiation;

namespace GerkinDev.PressurePlates
{
	[Context("Game")]
	internal class PressurePlatesConfigurator : Configurator
	{
		protected override void Configure()
		{
			Bind<ModInfo>().ToInstance(PressurePlatesModStarter.ModInfo);
			Bind<LogicModeSerializer>().AsSingleton();
			Bind<PressurePlateVersionService>().AsSingleton();
			Bind<OccupantDetectorService>().AsSingleton();
			Bind<PressurePlate>().AsTransient();
			MultiBind<TemplateModule>().ToProvider(() =>
			{
				TemplateModule.Builder builder = new();
				builder.AddDecorator<PressurePlateSpec, PressurePlate>();
				builder.AddDecorator<PressurePlate, Illuminator>();
				return builder.Build();
			}).AsSingleton();
		}
	}
}