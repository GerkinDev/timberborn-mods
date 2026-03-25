using GerkinDev.WatertightGates.Components;
using GerkinDev.WatertightGates.Services;
using System;
using Timberborn.AutomationBuildingsUI;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.SingletonSystem;
using Timberborn.SliderToggleSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace GerkinDev.WatertightGates.UI
{
	internal class WatertightGateFragment : IEntityPanelFragment, ILoadableSingleton
	{
		private readonly VisualElementLoader _visualElementLoader;
		private readonly SliderToggleFactory _sliderToggleFactory;
		private readonly ILoc _loc;
		private readonly OptionalDependencies _optionalDependencies;
		private WatertightGate? _target;

		private VisualElement _root;
		private VisualElement _automatedContainer;
		private EnumSliderToggle<WatertightGate.EGateMainMode> _mainModeToggle;
		private EnumSliderToggle<WatertightGate.EGateMode> _activeStateToggle;
		private EnumSliderToggle<WatertightGate.EGateMode> _inactiveStateToggle;

		public WatertightGateFragment(VisualElementLoader visualElementLoader, SliderToggleFactory sliderToggleFactory, ILoc loc, OptionalDependencies optionalDependencies)
		{
			_visualElementLoader = visualElementLoader;
			_sliderToggleFactory = sliderToggleFactory;
			_loc = loc;
			_optionalDependencies = optionalDependencies;
		}

		private float? _activeWidth;
		private float? _inactiveWidth;
		private Label _activeDesc;
		private Label _inactiveDesc;
		private void _SetLabelSizes(Rect? active = null, Rect? inactive = null)
		{
			if (active.HasValue)
			{
				_activeWidth = active.Value.width;
			}
			if (inactive.HasValue)
			{
				_inactiveWidth = inactive.Value.width;
			}
			if (_activeWidth.HasValue && _inactiveWidth.HasValue)
			{
				var width = Mathf.Max(_activeWidth.Value, _inactiveWidth.Value);
				_activeDesc.style.width = width;
				_inactiveDesc.style.width = width;
			}
		}

		#region IEntityPanelFragment
		public VisualElement InitializeFragment()
		{
			ClearFragment();
			_activeDesc = _root.Q<Label>("ActiveStateDesc");
			_inactiveDesc = _root.Q<Label>("InactiveStateDesc");
			_activeDesc.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
			{
				_SetLabelSizes(active: evt.newRect);
			});
			_inactiveDesc.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
			{
				_SetLabelSizes(inactive: evt.newRect);
			});

			return _root;
		}

		public void ShowFragment(BaseComponent entity)
		{
			var component = entity.GetComponent<WatertightGate>();
			if (component is not null)
			{
				_target = component;
				UpdateFragment();
				_root.ToggleDisplayStyle(true);
			}
		}

		public void ClearFragment()
		{
			_target = null;
			_root.ToggleDisplayStyle(false);
		}

		public void UpdateFragment()
		{
			if (_target is null)
			{
				return;
			}
			_automatedContainer.ToggleDisplayStyle(_target.MainMode == WatertightGate.EGateMainMode.Automated);
			_mainModeToggle.Update();
			_activeStateToggle.Update();
			_inactiveStateToggle.Update();
		}
		#endregion

		#region ILoadableSingleton
		public void Load()
		{
			_root = _visualElementLoader.LoadVisualElement("EntityPanel/WatertightGate");
			_automatedContainer = _root.Q<VisualElement>("AutomatedContainer");
			_automatedContainer.ToggleDisplayStyle(false);
			_mainModeToggle = new(
				_sliderToggleFactory,
				_root.Q<VisualElement>("MainModeToggle"),
				_root.Q<Label>("MainModeLabel"),
				() => _target.MainMode,
				value => _target.MainMode = value
			)
			{
				IconClassGetter = (value) => value switch
				{
					WatertightGate.EGateMainMode.Open => GateToggle.OpenedClass,
					WatertightGate.EGateMainMode.Close => GateToggle.ClosedClass,
					WatertightGate.EGateMainMode.Pass => "WatertightGate-fragment__icon-pass",
					WatertightGate.EGateMainMode.Automated => GateToggle.AutomatedClass,
					_ => throw new ArgumentException($"Invalid value {value}")
				},
				LabelGetter = value => _loc.T(value switch
				{
					WatertightGate.EGateMainMode.Open => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Open.Label",
					WatertightGate.EGateMainMode.Close => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Close.Label",
					WatertightGate.EGateMainMode.Pass => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Pass.Label",
					WatertightGate.EGateMainMode.Automated => "GerkinDev.WatertightGates.UI.WatertightGate.MainMode.Automated.Label",
					_ => throw new ArgumentException($"Invalid value {value}")
				}),
				TooltipGetter = value => _loc.T(value switch
				{
					WatertightGate.EGateMainMode.Open => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Open.Tooltip",
					WatertightGate.EGateMainMode.Close => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Close.Tooltip",
					WatertightGate.EGateMainMode.Pass => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Pass.Tooltip",
					WatertightGate.EGateMainMode.Automated => "GerkinDev.WatertightGates.UI.WatertightGate.MainMode.Automated.Tooltip",
					_ => throw new ArgumentException($"Invalid value {value}")
				}),
				AllowedValues = _optionalDependencies.PressurePlates
					? new[] { WatertightGate.EGateMainMode.Open, WatertightGate.EGateMainMode.Close, WatertightGate.EGateMainMode.Pass, WatertightGate.EGateMainMode.Automated }
					: new[] { WatertightGate.EGateMainMode.Open, WatertightGate.EGateMainMode.Close, WatertightGate.EGateMainMode.Automated }
			};
			_mainModeToggle.Initialize();

			static string getModeClass(WatertightGate.EGateMode value) => value switch
			{
				WatertightGate.EGateMode.Open => GateToggle.OpenedClass,
				WatertightGate.EGateMode.Close => GateToggle.ClosedClass,
				WatertightGate.EGateMode.Pass => "WatertightGate-fragment__icon-pass",
				_ => throw new ArgumentException($"Invalid value {value}")
			};
			string getModeLabel(WatertightGate.EGateMode value) => _loc.T(value switch
			{
				WatertightGate.EGateMode.Open => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Open.Label",
				WatertightGate.EGateMode.Close => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Close.Label",
				WatertightGate.EGateMode.Pass => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Pass.Label",
				_ => throw new ArgumentException($"Invalid value {value}")
			});
			string getModeTooltip(WatertightGate.EGateMode value) => _loc.T(value switch
			{
				WatertightGate.EGateMode.Open => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Open.Tooltip",
				WatertightGate.EGateMode.Close => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Close.Tooltip",
				WatertightGate.EGateMode.Pass => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Pass.Tooltip",
				_ => throw new ArgumentException($"Invalid value {value}")
			});
			var allowedModes = _optionalDependencies.PressurePlates
				? new[] { WatertightGate.EGateMode.Open, WatertightGate.EGateMode.Close, WatertightGate.EGateMode.Pass }
				: new[] { WatertightGate.EGateMode.Open, WatertightGate.EGateMode.Close };
			_activeStateToggle = new(
				_sliderToggleFactory,
				_root.Q<VisualElement>("ActiveStateToggle"),
				_root.Q<Label>("ActiveStateLabel"),
				() => _target.ActiveGateMode,
				value => _target.ActiveGateMode = value
			)
			{ IconClassGetter = getModeClass, LabelGetter = getModeLabel, TooltipGetter = getModeTooltip, AllowedValues = allowedModes };
			_activeStateToggle.Initialize();
			_inactiveStateToggle = new(
				_sliderToggleFactory,
				_root.Q<VisualElement>("InactiveStateToggle"),
				_root.Q<Label>("InactiveStateLabel"),
				() => _target.InactiveGateMode,
				value => _target.InactiveGateMode = value
			)
			{ IconClassGetter = getModeClass, LabelGetter = getModeLabel, TooltipGetter = getModeTooltip, AllowedValues = allowedModes };
			_inactiveStateToggle.Initialize();
		}
		#endregion
	}
}
