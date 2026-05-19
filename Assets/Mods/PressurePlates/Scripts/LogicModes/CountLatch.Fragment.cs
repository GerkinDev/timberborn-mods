using System;
using Timberborn.AutomationUI;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using UnityEngine.UIElements;

namespace GerkinDev.PressurePlates.LogicModes
{
	public partial class CountLatch
	{
		public class Fragment : IPressurePlateLogicModeUI<CountLatch>, IEntityPanelFragment
		{
			private readonly ILoc _loc;
			private readonly TransmitterSelectorInitializer _transmitterSelectorInitializer;
			private readonly VisualElementLoader _visualElementLoader;
			private CountLatch? _countLatch;
			private NineSliceIntegerField? _currentCountField;
			private TransmitterSelector? _currentCountResetAutomatorContainer;
			private NineSliceButton? _currentCountResetButton;
			private VisualElement? _root;
			private NineSliceIntegerField? _thresholdField;

			public Fragment(
				VisualElementLoader visualElementLoader,
				TransmitterSelectorInitializer transmitterSelectorInitializer,
				ILoc loc
			)
			{
				_visualElementLoader = visualElementLoader;
				_transmitterSelectorInitializer = transmitterSelectorInitializer;
				_loc = loc;
			}

			private void _OnThresholdChanged(ChangeEvent<int> evt)
			{
				if (_countLatch is null)
				{
					return;
				}

				var newValue = Math.Clamp(evt.newValue, 1, int.MaxValue);
				_thresholdField?.SetValueWithoutNotify(newValue);
				_countLatch.ActivationThreshold = newValue;
			}

			private void _OnResetCount(ClickEvent evt)
			{
				if (_countLatch is null)
				{
					return;
				}

				_countLatch.Count = 0;
			}

			#region IPressurePlateLogicModeUI

			public VisualElement Element => _root ??
				throw new NullReferenceException($"{nameof(Fragment)} has not been initialized");

			public IPressurePlateLogicModeUI ConnectToLogicMode(IPressurePlateLogicMode logicMode) =>
				ConnectToLogicMode((CountLatch)logicMode);

			public IPressurePlateLogicModeUI<CountLatch> ConnectToLogicMode(CountLatch logicMode)
			{
				_countLatch = logicMode;
				return this;
			}

			public void Reset() => _countLatch = null;

			#endregion

			#region IEntityPanelFragment

			public VisualElement InitializeFragment()
			{
				PressurePlates.Log(nameof(InitializeFragment));
				_root = _visualElementLoader.LoadVisualElement("EntityPanel/PressurePlateLogicModes/CountLatch");
				_thresholdField = _root.Q<NineSliceIntegerField>("ThresholdInput");
				_thresholdField.RegisterValueChangedCallback(_OnThresholdChanged);
				_thresholdField.isDelayed = true;
				_currentCountField = _root.Q<NineSliceIntegerField>("CurrentCountInput");
				_currentCountField.SetEnabled(false);
				_currentCountResetButton = _root.Q<NineSliceButton>("CurrentCountResetButton");
				_currentCountResetButton.RegisterCallback<ClickEvent>(_OnResetCount);
				_currentCountResetAutomatorContainer =
					_root.Q<TransmitterSelector>("CurrentCountResetAutomatorContainer");
				_transmitterSelectorInitializer.InitializeOptional(
					_currentCountResetAutomatorContainer,
					() => _countLatch?.ResetTrigger.Automator,
					automator =>
					{
						if (_countLatch is null)
						{
							return;
						}

						_countLatch.ResetTrigger.Automator = automator;
					});
				return _root;
			}

			public void ShowFragment(BaseComponent entity)
			{
				PressurePlates.Log(nameof(ShowFragment));
				_currentCountResetAutomatorContainer?.Show(entity);
			}

			public void ClearFragment()
			{
				PressurePlates.Log(nameof(ClearFragment));
				_currentCountResetAutomatorContainer?.ClearItems();
			}


			public void UpdateFragment()
			{
				if (_countLatch is null)
				{
					return;
				}

				_currentCountField?.SetValueWithoutNotify(_countLatch.Count);
				_thresholdField?.SetValueWithoutNotify(_countLatch.ActivationThreshold);
				_currentCountResetAutomatorContainer?.UpdateStateIcon();
			}

			#endregion
		}
	}
}