using System;
using System.Collections.Generic;
using GerkinDev.PressurePlates.Components;
using GerkinDev.PressurePlates.LogicModes;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using UnityEngine.UIElements;
using CountLatch = GerkinDev.PressurePlates.LogicModes.CountLatch;

namespace GerkinDev.PressurePlates.UI
{
	internal class PressurePlateFragment : IEntityPanelFragment
	{
		private readonly Dictionary<Type, IPressurePlateLogicModeUI> _modesDisplay = new();
		private readonly VisualElementLoader _visualElementLoader;
		private IPressurePlateLogicModeUI? _activeUI;
		private VisualElement? _logicModeUi;
		private VisualElement? _root;
		private PressurePlate? _target;

		public PressurePlateFragment(
			VisualElementLoader visualElementLoader,
			ILoc loc,
			CountLatch.Fragment countLatchFragment
		)
		{
			_visualElementLoader = visualElementLoader;
			_modesDisplay[typeof(CountLatch)] = countLatchFragment;
		}

		#region IEntityPanelFragment

		public VisualElement InitializeFragment()
		{
			_root = _visualElementLoader.LoadVisualElement("EntityPanel/PressurePlate");
			foreach (var (_, fragment) in _modesDisplay)
			{
				if (fragment is IEntityPanelFragment panelFragment)
				{
					panelFragment.InitializeFragment();
				}
			}

			_logicModeUi = _root.Q<VisualElement>("PressurePlateWrapper");
			ClearFragment();

			return _root;
		}

		public void ShowFragment(BaseComponent entity)
		{
			var component = entity.GetComponent<PressurePlate>();
			if (component is null)
			{
				if (_activeUI is not null)
				{
					_logicModeUi?.Remove(_activeUI.Element);
				}

				_activeUI?.Reset();
				_activeUI = null;
				return;
			}

			_target = component;
			_root.ToggleDisplayStyle(true);

			var newLogicModeUi = _modesDisplay[component.LogicMode.GetType()];
			if (newLogicModeUi == _activeUI)
			{
				return;
			}

			_activeUI?.Reset();
			_activeUI = _modesDisplay[component.LogicMode.GetType()].ConnectToLogicMode(component.LogicMode);
			_logicModeUi?.Add(_activeUI.Element);
			if (_activeUI is IEntityPanelFragment panelFragment)
			{
				panelFragment.ShowFragment(component);
			}

			UpdateFragment();
		}

		public void ClearFragment()
		{
			_target = null;
			_root?.ToggleDisplayStyle(false);
			if (_activeUI is IEntityPanelFragment panelFragment)
			{
				panelFragment.ClearFragment();
			}

			if (_activeUI is not null)
			{
				_logicModeUi?.Remove(_activeUI.Element);
			}

			_activeUI?.Reset();
			_activeUI = null;
		}

		public void UpdateFragment()
		{
			if (_target is null)
			{
				return;
			}

			if (_activeUI is IEntityPanelFragment panelFragment)
			{
				panelFragment.UpdateFragment();
			}
		}

		#endregion
	}
}