using System;
using Timberborn.AutomationBuildingsUI;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.SingletonSystem;
using Timberborn.SliderToggleSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.UI
{
	internal class WatertightGateFragment : IEntityPanelFragment, ILoadableSingleton
	{
		private readonly VisualElementLoader _visualElementLoader;
		private readonly SliderToggleFactory _sliderToggleFactory;
		private WatertightGate? _target;

		private VisualElement _root;
		private EnumSliderToggle<WatertightGate.EActivationMode> _activationModeToggle;
		private EnumSliderToggle<WatertightGate.EGateMode> _activeStateToggle;
		private EnumSliderToggle<WatertightGate.EGateMode> _inactiveStateToggle;

		public WatertightGateFragment(VisualElementLoader visualElementLoader, SliderToggleFactory sliderToggleFactory)
		{
			_visualElementLoader = visualElementLoader;
			_sliderToggleFactory = sliderToggleFactory;
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
				Debug.LogFormat("Active width: {0}", _activeWidth);
			}
			if (inactive.HasValue)
			{
				_inactiveWidth = inactive.Value.width;
				Debug.LogFormat("Inactive width: {0}", _inactiveWidth);
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
			_activationModeToggle.Update();
			_activeStateToggle.Update();
			_inactiveStateToggle.Update();
		}
		#endregion

		#region ILoadableSingleton
		public void Load()
		{
			_root = _visualElementLoader.LoadVisualElement("EntityPanel/WatertightGate");
			_activationModeToggle = new(
				_sliderToggleFactory,
				_root.Q<VisualElement>("ActivationModeToggle"),
				_root.Q<Label>("ActivationModeLabel"),
				() => _target.ActivationMode,
				value => _target.ActivationMode = value)
			{
				IconClassGetter = (value) => (value switch
				{
					WatertightGate.EActivationMode.Active => "WatertightGate-fragment__activation-mode-active",
					WatertightGate.EActivationMode.Inactive => "WatertightGate-fragment__activation-mode-inactive",
					WatertightGate.EActivationMode.Automated => GateToggle.AutomatedClass,
					_ => throw new Exception($"Invalid value {value}")
				})
			};
			_activationModeToggle.Initialize();

			static string getModeClass(WatertightGate.EGateMode value) => (value switch
			{
				WatertightGate.EGateMode.Open => GateToggle.OpenedClass,
				WatertightGate.EGateMode.Close => GateToggle.ClosedClass,
				WatertightGate.EGateMode.Pass => "WatertightGate-fragment__gate-mode-pass",
				_ => throw new Exception($"Invalid value {value}")
			});
			_activeStateToggle = new(
				_sliderToggleFactory,
				_root.Q<VisualElement>("ActiveStateToggle"),
				_root.Q<Label>("ActiveStateLabel"),
				() => _target.ActiveGateMode,
				value => _target.ActiveGateMode = value)
			{ IconClassGetter = getModeClass };
			_activeStateToggle.Initialize();
			_inactiveStateToggle = new(
				_sliderToggleFactory,
				_root.Q<VisualElement>("InactiveStateToggle"),
				_root.Q<Label>("InactiveStateLabel"),
				() => _target.InactiveGateMode,
				value => _target.InactiveGateMode = value)
			{ IconClassGetter = getModeClass };
			_inactiveStateToggle.Initialize();
		}
		#endregion
	}
}
