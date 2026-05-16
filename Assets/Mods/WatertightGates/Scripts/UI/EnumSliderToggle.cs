using System;
using System.Collections.Generic;
using Timberborn.SliderToggleSystem;
using UnityEngine.UIElements;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.UI
{
	/// <see cref="Timberborn.AutomationBuildingsUI.GateToggle" />
	internal class EnumSliderToggle<T> where T : struct, Enum
	{
		private readonly VisualElement _container;
		private readonly Func<T> _getValue;
		private readonly Action<T> _setValue;
		private readonly SliderToggleFactory _sliderToggleFactory;
		private readonly Label _valueDisplay;
		private SliderToggle? _sliderToggle;

		public EnumSliderToggle(SliderToggleFactory sliderToggleFactory, VisualElement container, Label valueDisplay,
			Func<T> getValue, Action<T> setValue)
		{
			_sliderToggleFactory = sliderToggleFactory ?? throw new ArgumentNullException(nameof(sliderToggleFactory));
			_container = container ?? throw new ArgumentNullException(nameof(container));
			_valueDisplay = valueDisplay ?? throw new ArgumentNullException(nameof(valueDisplay));
			_getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
			_setValue = setValue ?? throw new ArgumentNullException(nameof(setValue));
		}

		public Func<T, string> TooltipGetter { get; init; } = value => value.ToString();
		public Func<T, string> LabelGetter { get; init; } = value => value.ToString();
		public Func<T, string?> IconClassGetter { get; init; } = value => null;

		public void Initialize()
		{
			_container.Clear();
			List<SliderToggleItem> options = new();
			foreach (T value in Enum.GetValues(typeof(T)))
			{
				options.Add(SliderToggleItem.Create(
					() => TooltipGetter(value),
					IconClassGetter(value),
					() => _DoSetValue(value),
					() => value.Equals(_getValue())
				));
			}

			_sliderToggle = _sliderToggleFactory.Create(_container, options.ToArray());
		}

		private void _DoSetValue(T value)
		{
			_setValue(value);
			_valueDisplay.text = LabelGetter(value);
		}

		public void Update()
		{
			if (_sliderToggle == null)
			{
				return;
			}

			_sliderToggle.Update();
			_valueDisplay.text = LabelGetter(_getValue());
		}
	}
}