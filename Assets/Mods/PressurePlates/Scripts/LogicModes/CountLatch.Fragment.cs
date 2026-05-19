using System;
using Timberborn.CoreUI;
using Timberborn.Localization;
using UnityEngine.UIElements;

namespace GerkinDev.PressurePlates.Components.LogicModes
{
	public partial class CountLatch
	{
		public class Fragment : IPressurePlateLogicModeUI<CountLatch>
		{
			private readonly ILoc _loc;
			private readonly VisualElementLoader _visualElementLoader;
			private CountLatch? _countLatch;
			private NineSliceIntegerField? _currentCountField;
			private NineSliceButton? _currentCountResetButton;
			private VisualElement? _root;
			private NineSliceIntegerField? _thresholdField;

			public Fragment(VisualElementLoader visualElementLoader, ILoc loc)
			{
				_visualElementLoader = visualElementLoader;
				_loc = loc;
			}

			#region ILoadableSingleton

			public void Load() => PressurePlates.Log("CountLatchFragment.Load");

			#endregion

			private void _OnThresholdChanged(ChangeEvent<int> evt)
			{
				if (_countLatch is null)
				{
					return;
				}

				var newValue = Math.Clamp(evt.newValue, 1, int.MaxValue);
				_thresholdField?.SetValueWithoutNotify(newValue);
				_countLatch._activationThreshold = newValue;
				_countLatch.Update();
			}

			private void _OnResetCount(ClickEvent evt)
			{
				if (_countLatch is null)
				{
					return;
				}

				_countLatch._count = 0;
				_countLatch.Update();
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

			public void InitializeFragment()
			{
				_root = _visualElementLoader.LoadVisualElement("EntityPanel/PressurePlateLogicModes/CountLatch");
				_thresholdField = _root.Q<NineSliceIntegerField>("ThresholdInput");
				_thresholdField.RegisterValueChangedCallback(_OnThresholdChanged);
				_thresholdField.isDelayed = true;
				_currentCountField = _root.Q<NineSliceIntegerField>("CurrentCountInput");
				_currentCountField.SetEnabled(false);
				_currentCountResetButton = _root.Q<NineSliceButton>("CurrentCountResetButton");
				_currentCountResetButton.RegisterCallback<ClickEvent>(_OnResetCount);
			}

			public void UpdateFragment()
			{
				if (_countLatch is null)
				{
					return;
				}

				_currentCountField?.SetValueWithoutNotify(_countLatch._count);
				_thresholdField?.SetValueWithoutNotify(_countLatch._activationThreshold);
			}

			#endregion
		}
	}
}