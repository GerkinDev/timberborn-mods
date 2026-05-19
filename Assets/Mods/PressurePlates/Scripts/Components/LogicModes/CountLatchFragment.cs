using System;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.SingletonSystem;
using UnityEngine.UIElements;

namespace GerkinDev.PressurePlates.Components.LogicModes
{
	public class CountLatchFragment: IPressurePlateLogicModeUI<CountLatch>, ILoadableSingleton
	{
		private readonly VisualElementLoader _visualElementLoader;
		private readonly ILoc _loc;
		private VisualElement? _root;
		private CountLatch? _countLatch;

		public CountLatchFragment(VisualElementLoader visualElementLoader, ILoc loc)
		{
			_visualElementLoader = visualElementLoader;
			_loc = loc;
		}

		#region IPressurePlateLogicModeUI
		public VisualElement Element => _root ?? throw new NullReferenceException($"{nameof(CountLatchFragment)} has not been initialized");
		public IPressurePlateLogicModeUI ConnectToLogicMode(IPressurePlateLogicMode logicMode) => ConnectToLogicMode((CountLatch)logicMode);

		public IPressurePlateLogicModeUI<CountLatch> ConnectToLogicMode(CountLatch logicMode)
		{
			_countLatch = logicMode;
			return this;
		}

		public void Reset()
		{
			_countLatch = null;
		}

		public void InitializeFragment(){
			PressurePlates.Log("CountLatchFragment.InitializeFragment");
			if (_root == null)
			{
				throw new NullReferenceException($"{nameof(CountLatchFragment)} has not been initialized");
			}
		}

		public void UpdateFragment(){}
		#endregion

		#region ILoadableSingleton

		public void Load()
		{
			PressurePlates.Log("CountLatchFragment.Load");
			_root = _visualElementLoader.LoadVisualElement("EntityPanel/PressurePlateLogicModes/CountLatch");
		}

		#endregion
	}
}