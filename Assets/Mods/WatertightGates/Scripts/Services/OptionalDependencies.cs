using System;
using System.Linq;

namespace GerkinDev.WatertightGates.Services
{
	internal record OptionalDependencies
	{
		public OptionalDependencies()
		{
			PressurePlates = _IsClassLoaded("GerkinDev.PressurePlates.PressurePlatesConfigurator");
			WatertightGates.Log(
				PressurePlates
					? "GerkinDev.PressurePlates mod active"
					: "GerkinDev.PressurePlates mod missing"
			);
		}

		public bool PressurePlates { get; }

		private static bool _IsClassLoaded(string className) => AppDomain.CurrentDomain.GetAssemblies()
			.Any(assembly => assembly.GetType(className) != null);
	}
}