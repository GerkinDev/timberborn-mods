using Bindito.Core;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.Specs;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.UI;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Services;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.UI;
using Timberborn.EntityPanelSystem;
using Timberborn.Illumination;
using Timberborn.TemplateInstantiation;
using UnityEngine;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts
{
	[Context("Game")]
	internal class WatertightGatesConfigurator : Configurator
	{
		private OptionalDependencies _optionalDependencies;

		protected override void Configure()
		{
			Bind<GateLikeUpdater>().AsSingleton();

			Bind<WatertightGateFragment>().AsSingleton();
			_optionalDependencies = new();
			Bind<OptionalDependencies>().ToInstance(_optionalDependencies);
			MultiBind<EntityPanelModule>().ToProvider<WatertightGateEntityPanelModuleProvider>().AsSingleton();

			Bind<WatertightGate>().AsTransient();
			Bind<WatertightGateTransformController>().AsTransient();
			Bind<NavMeshBlocker>().AsTransient();
			Bind<WaterBlocker>().AsTransient();
			Bind<WatertightGateConflictStatus>().AsTransient();
			Bind<WatertightGateCheckState>().AsTransient();
			Bind<FreePositionedDynamicPathModel>().AsTransient();
			MultiBind<TemplateModule>().ToProvider(() =>
			{
				var builder = new TemplateModule.Builder();
				builder.AddDecorator<WatertightGateSpec, WatertightGateTransformController>();
				builder.AddDecorator<WatertightGateTransformController, WaterBlocker>();
				builder.AddDecorator<WatertightGateSpec, WatertightGate>();
				builder.AddDecorator<WatertightGate, NavMeshBlocker>();
				builder.AddDecorator<WatertightGate, WatertightGateConflictStatus>();
				builder.AddDecorator<WatertightGate, WatertightGateCheckState>();
				builder.AddDecorator<WatertightGate, Illuminator>();
				builder.AddDecorator<FreePositionedDynamicPathModelSpec, FreePositionedDynamicPathModel>();
				return builder.Build();
			}).AsSingleton();
			if (_optionalDependencies.PressurePlates)
			{
				_ConfigurePressurePlateExtension();
			}
		}

		private void _ConfigurePressurePlateExtension()
		{
			Bind<GateAutoOpener>().AsTransient();
			MultiBind<TemplateModule>().ToProvider(() =>
			{
				var builder = new TemplateModule.Builder();
				builder.AddDecorator<WatertightGate, GateAutoOpener>();
				return builder.Build();
			}).AsSingleton();
		}
	}
}
