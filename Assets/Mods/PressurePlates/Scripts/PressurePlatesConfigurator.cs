using Bindito.Core;
using GerkinDev.PressurePlates.Assets.Mods.PressurePlates.Scripts.Services;

namespace GerkinDev.PressurePlates.Assets.Mods.PressurePlates.Scripts
{
	[Context("Game")]
	internal class PressurePlatesConfigurator : Configurator
	{
		protected override void Configure()
		{
			Bind<OccupantDetectorService>().AsSingleton();
			//MultiBind<TemplateModule>().ToProvider(_ProvideTemplateModule).AsSingleton();
		}

		//private static TemplateModule _ProvideTemplateModule()
		//{
		//	var builder = new TemplateModule.Builder();
		//	return builder.Build();
		//}
	}
}
