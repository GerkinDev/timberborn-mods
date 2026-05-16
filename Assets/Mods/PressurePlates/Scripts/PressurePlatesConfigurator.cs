using Bindito.Core;
using GerkinDev.PressurePlates.Services;

namespace GerkinDev.PressurePlates
{
	[Context("Game")]
	internal class PressurePlatesConfigurator : Configurator
	{
		protected override void Configure() => Bind<OccupantDetectorService>().AsSingleton();
	}
}