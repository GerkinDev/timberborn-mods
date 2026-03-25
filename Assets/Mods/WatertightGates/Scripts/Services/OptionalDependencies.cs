using System;
using System.Linq;

namespace GerkinDev.WatertightGates.Services
{
	internal record OptionalDependencies
	{
		private static bool _IsClassLoaded(string className) { return AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetType(className) != null); }
		public OptionalDependencies()
		{
			PressurePlates = _IsClassLoaded("GerkinDev.PressurePlates.PressurePlatesConfigurator");
			if (PressurePlates)
			{
				WatertightGates.Log("GerkinDev.PressurePlates mod active");
			}
			else
			{
				WatertightGates.Log("GerkinDev.PressurePlates mod missing");
			}
		}
		public bool PressurePlates { get; private init; }
	}
}
