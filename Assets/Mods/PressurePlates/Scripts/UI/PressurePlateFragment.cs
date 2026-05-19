using System;
using System.Collections.Generic;
using GerkinDev.PressurePlates.Components;
using GerkinDev.PressurePlates.Components.LogicModes;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.SingletonSystem;
using Timberborn.SliderToggleSystem;
using UnityEngine.UIElements;

namespace GerkinDev.PressurePlates.UI
{
	internal class PressurePlateFragment : IEntityPanelFragment, ILoadableSingleton
	{
		private readonly VisualElementLoader _visualElementLoader;
		private VisualElement? _root;
		private PressurePlate? _target;
		private readonly Dictionary<Type, IPressurePlateLogicModeUI> _modesDisplay = new();
		private IPressurePlateLogicModeUI? _activeUI;
		private VisualElement? _logicModeUi;

		public PressurePlateFragment(
			VisualElementLoader visualElementLoader,
			SliderToggleFactory sliderToggleFactory,
			ILoc loc
		)
		{
			_visualElementLoader = visualElementLoader;
			_modesDisplay[typeof(CountLatch)] = new CountLatchFragment(visualElementLoader, loc);
		}

		#region ILoadableSingleton

		public void Load()
		{
			PressurePlates.Log("PressurePlateFragment.Load");
			_root = _visualElementLoader.LoadVisualElement("EntityPanel/PressurePlate");
			foreach (var (_, display) in _modesDisplay)
			{
				display.Load();
			}
		}

		#endregion

		#region IEntityPanelFragment

		public VisualElement InitializeFragment()
		{
			PressurePlates.Log("PressurePlateFragment.InitializeFragment");
			if (_root == null)
			{
				throw new NullReferenceException($"{nameof(PressurePlateFragment)} has not been initialized");
			}

			_logicModeUi = _root.Q<VisualElement>("LogicModeUi");
			ClearFragment();

			return _root;
		}

		public void ShowFragment(BaseComponent entity)
		{
			var component = entity.GetComponent<PressurePlate>();
			if (component is null)
			{
				_activeUI?.Reset();
				_activeUI = null;
				return;
			}

			_target = component;
			UpdateFragment();
			_root.ToggleDisplayStyle(true);
			_activeUI?.Reset();
			_activeUI = _modesDisplay[component.LogicMode.GetType()].ConnectToLogicMode(component.LogicMode);
			_logicModeUi.Add(_activeUI.Element);
		}

		public void ClearFragment()
		{
			_target = null;
			_root?.ToggleDisplayStyle(false);
			_logicModeUi?.Clear();
			_activeUI?.Reset();
		}

		public void UpdateFragment()
		{
			if (_target is null)
			{
				return;
			}

			_activeUI?.UpdateFragment();
		}

		#endregion
	}
}