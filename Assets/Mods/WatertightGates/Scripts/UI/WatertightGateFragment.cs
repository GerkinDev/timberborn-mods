using System;
using GerkinDev.WatertightGates.Components;
using GerkinDev.WatertightGates.Services;
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
		private readonly ILoc _loc;
		private readonly OptionalDependencies _optionalDependencies;
		private readonly SliderToggleFactory _sliderToggleFactory;
		private readonly VisualElementLoader _visualElementLoader;
		private Label? _activeDesc;
		private EnumSliderToggle<WatertightGate.EGateMode>? _activeStateToggle;
		private float? _activeWidth;
		private VisualElement? _automatedContainer;
		private Label? _inactiveDesc;
		private EnumSliderToggle<WatertightGate.EGateMode>? _inactiveStateToggle;
		private float? _inactiveWidth;
		private EnumSliderToggle<WatertightGate.EGateMainMode>? _mainModeToggle;

		private VisualElement? _root;
		private WatertightGate? _target;

		public WatertightGateFragment(VisualElementLoader visualElementLoader, SliderToggleFactory sliderToggleFactory,
			ILoc loc, OptionalDependencies optionalDependencies)
		{
			_visualElementLoader = visualElementLoader;
			_sliderToggleFactory = sliderToggleFactory;
			_loc = loc;
			_optionalDependencies = optionalDependencies;
		}

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
				() => _target!.MainMode,
				value => _target!.MainMode = value,
				_optionalDependencies.PressurePlates
					? new[]
					{
						WatertightGate.EGateMainMode.OPEN,
						WatertightGate.EGateMainMode.CLOSE,
						WatertightGate.EGateMainMode.PASS,
						WatertightGate.EGateMainMode.AUTOMATED
					}
					: new[]
					{
						WatertightGate.EGateMainMode.OPEN,
						WatertightGate.EGateMainMode.CLOSE,
						WatertightGate.EGateMainMode.AUTOMATED
					}
			)
			{
				IconClassGetter = value => value switch
				{
					WatertightGate.EGateMainMode.OPEN => GateToggle.OpenedClass,
					WatertightGate.EGateMainMode.CLOSE => GateToggle.ClosedClass,
					WatertightGate.EGateMainMode.PASS => "WatertightGate-fragment__icon-pass",
					WatertightGate.EGateMainMode.AUTOMATED => GateToggle.AutomatedClass,
					_ => throw new ArgumentException($"Invalid value {value}")
				},
				LabelGetter = value => _loc.T(value switch
				{
					WatertightGate.EGateMainMode.OPEN => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Open.Label",
					WatertightGate.EGateMainMode.CLOSE =>
						"GerkinDev.WatertightGates.UI.WatertightGate.Modes.Close.Label",
					WatertightGate.EGateMainMode.PASS => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Pass.Label",
					WatertightGate.EGateMainMode.AUTOMATED =>
						"GerkinDev.WatertightGates.UI.WatertightGate.MainMode.Automated.Label",
					_ => throw new ArgumentException($"Invalid value {value}")
				}),
				TooltipGetter = value => _loc.T(value switch
				{
					WatertightGate.EGateMainMode.OPEN =>
						"GerkinDev.WatertightGates.UI.WatertightGate.Modes.Open.Tooltip",
					WatertightGate.EGateMainMode.CLOSE =>
						"GerkinDev.WatertightGates.UI.WatertightGate.Modes.Close.Tooltip",
					WatertightGate.EGateMainMode.PASS =>
						"GerkinDev.WatertightGates.UI.WatertightGate.Modes.Pass.Tooltip",
					WatertightGate.EGateMainMode.AUTOMATED =>
						"GerkinDev.WatertightGates.UI.WatertightGate.MainMode.Automated.Tooltip",
					_ => throw new ArgumentException($"Invalid value {value}")
				})
			};
			_mainModeToggle.Initialize();

			_activeStateToggle = new(
				_sliderToggleFactory,
				_root.Q<VisualElement>("ActiveStateToggle"),
				_root.Q<Label>("ActiveStateLabel"),
				() => _target!.ActiveGateMode,
				value => _target!.ActiveGateMode = value
			) { IconClassGetter = GetModeClass, LabelGetter = GetModeLabel, TooltipGetter = GetModeTooltip };
			_activeStateToggle.Initialize();
			_inactiveStateToggle = new(
				_sliderToggleFactory,
				_root.Q<VisualElement>("InactiveStateToggle"),
				_root.Q<Label>("InactiveStateLabel"),
				() => _target!.InactiveGateMode,
				value => _target!.InactiveGateMode = value
			) { IconClassGetter = GetModeClass, LabelGetter = GetModeLabel, TooltipGetter = GetModeTooltip };
			_inactiveStateToggle.Initialize();

			static string GetModeClass(WatertightGate.EGateMode value) =>
				value switch
				{
					WatertightGate.EGateMode.OPEN => GateToggle.OpenedClass,
					WatertightGate.EGateMode.CLOSE => GateToggle.ClosedClass,
					WatertightGate.EGateMode.PASS => "WatertightGate-fragment__icon-pass",
					_ => throw new ArgumentException($"Invalid value {value}")
				};

			string GetModeLabel(WatertightGate.EGateMode value) =>
				_loc.T(value switch
				{
					WatertightGate.EGateMode.OPEN => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Open.Label",
					WatertightGate.EGateMode.CLOSE => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Close.Label",
					WatertightGate.EGateMode.PASS => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Pass.Label",
					_ => throw new ArgumentException($"Invalid value {value}")
				});

			string GetModeTooltip(WatertightGate.EGateMode value) =>
				_loc.T(value switch
				{
					WatertightGate.EGateMode.OPEN => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Open.Tooltip",
					WatertightGate.EGateMode.CLOSE => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Close.Tooltip",
					WatertightGate.EGateMode.PASS => "GerkinDev.WatertightGates.UI.WatertightGate.Modes.Pass.Tooltip",
					_ => throw new ArgumentException($"Invalid value {value}")
				});
		}

		#endregion

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

			if (!_activeWidth.HasValue || !_inactiveWidth.HasValue)
			{
				return;
			}

			var width = Mathf.Max(_activeWidth.Value, _inactiveWidth.Value);

			if (_activeDesc != null)
			{
				_activeDesc.style.width = width;
			}

			if (_inactiveDesc != null)
			{
				_inactiveDesc.style.width = width;
			}
		}

		#region IEntityPanelFragment

		public VisualElement InitializeFragment()
		{
			if (_root == null)
			{
				throw new NullReferenceException($"{nameof(WatertightGateFragment)} has not been initialized");
			}

			ClearFragment();
			_activeDesc = _root.Q<Label>("ActiveStateDesc");
			_inactiveDesc = _root.Q<Label>("InactiveStateDesc");
			_activeDesc.RegisterCallbackOnce<GeometryChangedEvent>(evt => { _SetLabelSizes(evt.newRect); });
			_inactiveDesc.RegisterCallbackOnce<GeometryChangedEvent>(evt => { _SetLabelSizes(inactive: evt.newRect); });

			return _root;
		}

		public void ShowFragment(BaseComponent entity)
		{
			var component = entity.GetComponent<WatertightGate>();
			if (component is null)
			{
				return;
			}

			_target = component;
			UpdateFragment();
			_root.ToggleDisplayStyle(true);
		}

		public void ClearFragment()
		{
			_target = null;
			_root.ToggleDisplayStyle(false);
		}

		public void UpdateFragment()
		{
			if (
				_target is null ||
				_mainModeToggle is null ||
				_activeStateToggle is null ||
				_inactiveStateToggle is null
			)
			{
				return;
			}

			_automatedContainer.ToggleDisplayStyle(_target.MainMode == WatertightGate.EGateMainMode.AUTOMATED);
			_mainModeToggle.Update();
			_activeStateToggle.Update();
			_inactiveStateToggle.Update();
		}

		#endregion
	}
}