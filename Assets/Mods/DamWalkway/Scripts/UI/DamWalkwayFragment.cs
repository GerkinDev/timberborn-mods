using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.SingletonSystem;
using Timberborn.SliderToggleSystem;
using UnityEngine.UIElements;

namespace GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Scripts.UI
{
	internal class DamWalkwayFragment : IEntityPanelFragment, ILoadableSingleton
	{
		private readonly VisualElementLoader _visualElementLoader;
		private readonly SliderToggleFactory _sliderToggleFactory;
		private VisualElement _root;
		private SliderToggle _toggle;
		private DamWalkway? _target;

		private Label? _label;
		private VisualElement? _modeToggleContainer;

		public DamWalkwayFragment(VisualElementLoader visualElementLoader, SliderToggleFactory sliderToggleFactory)
		{
			_visualElementLoader = visualElementLoader;
			_sliderToggleFactory = sliderToggleFactory;
		}

		public VisualElement InitializeFragment()
		{
			// Delegates bellow are ran only on update, thus only if the target is set
			var sliderToggleItemOpen = _CreateToggleItem(DamWalkway.EState.Open);
			var sliderToggleItemClose = _CreateToggleItem(DamWalkway.EState.Close);
			var sliderToggleItemPass = _CreateToggleItem(DamWalkway.EState.Pass);
			var sliderToggleItemAutomated = _CreateToggleItem(DamWalkway.EState.Automated);
			_toggle = _sliderToggleFactory.Create(_modeToggleContainer, sliderToggleItemOpen, sliderToggleItemClose, sliderToggleItemPass, sliderToggleItemAutomated);

			return _root;
		}

		private SliderToggleItem _CreateToggleItem(DamWalkway.EState state)
		{
			var name = state.ToString();
			return SliderToggleItem.Create(
				() => name,
				$"is-{name}",
				() => _TargetState = state,
				() => _TargetState == state
			);
		}

		private DamWalkway.EState _TargetState
		{
			get
			{
				if (_target is null)
				{
					return DamWalkway.EState.Open;
				}
				return _target.state;
			}

			set
			{
				if (_target is null)
				{
					return;
				}
				_target.state = value;
				_UpdateLabel();
			}
		}

		private void _UpdateLabel()
		{
			_label.text = _TargetState.ToString();
		}

		public void ShowFragment(BaseComponent entity)
		{
			var component = entity.GetComponent<DamWalkway>();
			if (component is not null)
			{
				_target = component;
				_UpdateLabel();
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
			_toggle.Update();
		}

		public void Load()
		{
			_root = _visualElementLoader.LoadVisualElement("EntityPanel/DamWalkway");
			_label = _root.Q<Label>("ModeLabel");
			_modeToggleContainer = _root.Q<VisualElement>("ModeToggle");
			_modeToggleContainer.Clear();
		}
	}
}
