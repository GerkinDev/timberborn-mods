using Bindito.Core;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.Specs;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.UI;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Services;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.UI;
using System;
using System.Linq;
using Timberborn.EntityPanelSystem;
using Timberborn.Illumination;
using Timberborn.TemplateInstantiation;
using UnityEngine;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts
{
	[Context("Game")]
	internal class WatertightGatesConfigurator : Configurator
	{
		private static bool _IsClassLoaded(string className) { return AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetType(className) != null); }
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

		private TemplateModule _ProvideTemplateModule()
		{
			var builder = new TemplateModule.Builder();
			builder.AddDecorator<WatertightGateSpec, WatertightGateTransformController>();
			builder.AddDecorator<WatertightGateTransformController, WaterBlocker>();
			builder.AddDecorator<WatertightGateSpec, WatertightGate>();
			builder.AddDecorator<WatertightGate, NavMeshBlocker>();
			builder.AddDecorator<WatertightGate, WatertightGateConflictStatus>();
			builder.AddDecorator<WatertightGate, Illuminator>();
			if (_IsClassLoaded("GerkinDev.PressurePlates.Assets.Mods.PressurePlates.Scripts.PressurePlatesConfigurator"))
			{
				Debug.Log("GerkinDev.PressurePlates mod active");
				_InitPressurePlateInterop(builder);
			}
			else
			{
				Debug.Log("GerkinDev.PressurePlates mod missing");
			}
			builder.AddDecorator<FreePositionedDynamicPathModelSpec, FreePositionedDynamicPathModel>();
			return builder.Build();
		}

		private void _InitPressurePlateInterop(TemplateModule.Builder builder)
		{
			Bind<GateAutoOpener>().AsTransient();
			builder.AddDecorator<WatertightGate, GateAutoOpener>();
		}
	}
}
