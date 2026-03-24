using Bindito.Core;
using GerkinDev.PressurePlates.Assets.Mods.PressurePlates.Scripts.Components;
using GerkinDev.PressurePlates.Assets.Mods.PressurePlates.Scripts.Components.Spec;
using GerkinDev.PressurePlates.Assets.Mods.PressurePlates.Scripts.Services;
using Timberborn.Illumination;
using Timberborn.TemplateInstantiation;

namespace GerkinDev.PressurePlates.Assets.Mods.PressurePlates.Scripts
{
	[Context("Game")]
	internal class PressurePlatesConfigurator : Configurator
	{
		protected override void Configure()
		{
			Bind<OccupantDetectorService>().AsSingleton();
			Bind<PressurePlate>().AsTransient();
			MultiBind<TemplateModule>().ToProvider(() =>
			{
				var builder = new TemplateModule.Builder();
				builder.AddDecorator<PressurePlateSpec, PressurePlate>();
				builder.AddDecorator<PressurePlate, Illuminator>();
				return builder.Build();
			}).AsSingleton();
		}
	}
}
