#region Assembly Timberborn.WaterBuildings, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// D:\Work\github\timberborn-mod-dam-walkway\Assets\Plugins\Timberborn\Timberborn.WaterBuildings.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.DuplicationSystem;
using Timberborn.Persistence;
using Timberborn.WaterBuildings;
using Timberborn.WaterObjects;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Draft
{

	public class Floodgate : BaseComponent, IAwakableComponent, IFinishedStateListener, IUnfinishedStateListener, IPreviewStateListener, IPersistentEntity, IDuplicable<Floodgate>, IDuplicable, ITerminal
	{
		public static readonly ComponentKey FloodgateKey = new ComponentKey("Floodgate");

		public static readonly PropertyKey<bool> IsSynchronizedKey = new PropertyKey<bool>("IsSynchronized");

		public static readonly PropertyKey<float> HeightKey = new PropertyKey<float>("Height");

		public static readonly PropertyKey<float> AutomationHeightKey = new PropertyKey<float>("AutomationHeight");

		public static readonly float DefaultHeightOffset = 0.35f;

		public readonly FloodgateSynchronizer _floodgateSynchronizer;

		public BlockObject _blockObject;

		public WaterObstacle _waterObstacle;

		public Automatable _automatable;

		public FloodgateAnimationController _animationController;

		public FloodgateSpec _floodgateSpec;

		public float? _lastEffectiveHeight;

		public bool IsSynchronized { get; private set; } = true;


		public float Height { get; private set; }

		public float AutomationHeight { get; private set; }

		public int MaxHeight => _floodgateSpec.MaxHeight;

		public float PositionedHeight => _blockObject.Coordinates.z + Height;

		public float PositionedAutomationHeight => _blockObject.Coordinates.z + AutomationHeight;

		public bool IsAutomated => _automatable.IsAutomated;

		public bool IsInputOn => _automatable.State == ConnectionState.On;

		public Floodgate(FloodgateSynchronizer floodgateSynchronizer)
		{
			_floodgateSynchronizer = floodgateSynchronizer;
		}

		public void Awake()
		{
			_blockObject = GetComponent<BlockObject>();
			_waterObstacle = GetComponent<WaterObstacle>();
			_automatable = GetComponent<Automatable>();
			_animationController = GetComponent<FloodgateAnimationController>();
			_floodgateSpec = GetComponent<FloodgateSpec>();
			Height = MaxHeight - DefaultHeightOffset;
			AutomationHeight = MaxHeight;
			DisableComponent();
			_automatable.InputReconnected += OnAutomatableInputReconnected;
		}

		public void SetHeightAndSynchronize(float newHeight)
		{
			SetHeight(newHeight);
			SynchronizeAllNeighbors();
		}

		public void SetAutomationHeightAndSynchronize(float newAutomationHeight)
		{
			SetAutomationHeight(newAutomationHeight);
			SynchronizeAllNeighbors();
		}

		public void SetHeight(float newHeight)
		{
			Height = ClampHeight(newHeight);
			UpdateEffectiveHeight(forceInstant: false);
		}

		public void SetAutomationHeight(float newAutomationHeight)
		{
			AutomationHeight = ClampHeight(newAutomationHeight);
			UpdateEffectiveHeight(forceInstant: false);
		}

		public void ToggleSynchronization(bool newValue)
		{
			IsSynchronized = newValue;
			_floodgateSynchronizer.SynchronizeWithAllNeighbors((Timberborn.WaterBuildings.Floodgate)(object)this);
		}

		public void Save(IEntitySaver entitySaver)
		{
			var component = entitySaver.GetComponent(FloodgateKey);
			component.Set(HeightKey, Height);
			component.Set(AutomationHeightKey, AutomationHeight);
			component.Set(IsSynchronizedKey, IsSynchronized);
		}

		[BackwardCompatible(2025, 12, 15, Compatibility.Save)]
		public void Load(IEntityLoader entityLoader)
		{
			var component = entityLoader.GetComponent(FloodgateKey);
			Height = component.Get(HeightKey);
			if (component.Has(AutomationHeightKey))
			{
				AutomationHeight = component.Get(AutomationHeightKey);
			}

			IsSynchronized = component.Get(IsSynchronizedKey);
		}

		public void DuplicateFrom(Floodgate source)
		{
			IsSynchronized = source.IsSynchronized;
			Height = ClampHeight(source.Height);
			AutomationHeight = ClampHeight(source.AutomationHeight);
			UpdateEffectiveHeight(forceInstant: false);
			SynchronizeAllNeighbors();
		}

		public void OnEnterUnfinishedState()
		{
			_floodgateSynchronizer.SynchronizeWithUnfinishedNeighbors((Timberborn.WaterBuildings.Floodgate)(object)this);
			UpdateEffectiveHeight(forceInstant: true);
		}

		public void OnExitUnfinishedState()
		{
		}

		public void OnEnterFinishedState()
		{
			EnableComponent();
			UpdateEffectiveHeight(forceInstant: true);
		}

		public void OnExitFinishedState()
		{
			DisableComponent();
			_waterObstacle.RemoveFromWaterService();
		}

		public void OnEnterPreviewState()
		{
			UpdateEffectiveHeight(forceInstant: true);
		}

		public void Evaluate()
		{
			UpdateEffectiveHeight(forceInstant: false);
		}

		public void OnAutomatableInputReconnected(object sender, EventArgs e)
		{
			SynchronizeAllNeighbors();
		}

		public void UpdateEffectiveHeight(bool forceInstant)
		{
			var num = _automatable.State == ConnectionState.On ? AutomationHeight : Height;
			if (!_lastEffectiveHeight.Equals(num))
			{
				SetVisualHeight(num, forceInstant);
				if (_blockObject.IsFinished)
				{
					SetObstacleHeight(num);
					_lastEffectiveHeight = num;
				}
			}
		}

		public void SetVisualHeight(float effectiveHeight, bool forceInstant)
		{
			if (forceInstant || !_blockObject.IsFinished)
			{
				_animationController.MoveGateInstantly(effectiveHeight);
			}
			else
			{
				_animationController.MoveGateSmoothly(effectiveHeight);
			}
		}

		public void SetObstacleHeight(float effectiveHeight)
		{
			_waterObstacle.RemoveFromWaterService();
			if (effectiveHeight > 0f)
			{
				_waterObstacle.AddToWaterService(effectiveHeight);
			}
		}

		public void SynchronizeAllNeighbors()
		{
			_floodgateSynchronizer.SynchronizeAllNeighbors((Timberborn.WaterBuildings.Floodgate)(object)this);
		}

		public float ClampHeight(float newHeight)
		{
			return Mathf.Clamp(newHeight, 0f, MaxHeight);
		}
	}
}
