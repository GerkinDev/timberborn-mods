#region Assembly Timberborn.AutomationBuildings, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// D:\Work\github\timberborn-mod-dam-walkway\Assets\Plugins\Timberborn\Timberborn.AutomationBuildings.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using Timberborn.Automation;
using Timberborn.AutomationBuildings;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.DuplicationSystem;
using Timberborn.EntitySystem;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;

namespace GerkinDev.DamWalkway.Assets.Mods.DamWalkway.Draft
{

	public class Gate : BaseComponent, IAwakableComponent, IDeletableEntity, IPersistentEntity, IFinishedStateListener, IAutomatableNeeder, IDuplicable<Gate>, IDuplicable, ITerminal
	{
		public static readonly ComponentKey ComponentKey = new ComponentKey("Gate");

		public static readonly PropertyKey<GateOpeningMode> OpeningModeKey = new PropertyKey<GateOpeningMode>("OpeningMode");

		public readonly GateUpdater _gateUpdater;

		public BlockObject _blockObject;

		public Automatable _automatable;

		public GateNavMeshBlocker _gateNavMeshBlocker;

		public GateOpeningMode _gateOpeningMode;

		public bool IsConflict { get; private set; }

		public bool OpenMode => _gateOpeningMode == GateOpeningMode.Open;

		public bool ClosedMode => _gateOpeningMode == GateOpeningMode.Closed;

		public bool AutomatedMode => _gateOpeningMode == GateOpeningMode.Automated;

		public bool NeedsAutomatable => AutomatedMode;

		public bool IsOpenByAutomation
		{
			get
			{
				if (AutomatedMode)
				{
					return _automatable.State != ConnectionState.Off;
				}

				return false;
			}
		}

		public event EventHandler StateChanged;

		public Gate(GateUpdater gateUpdater)
		{
			_gateUpdater = gateUpdater;
		}

		public void Awake()
		{
			_blockObject = GetComponent<BlockObject>();
			_automatable = GetComponent<Automatable>();
			_gateNavMeshBlocker = GetComponent<GateNavMeshBlocker>();
		}

		public void DeleteEntity()
		{
			if (_gateNavMeshBlocker.NavMeshBlocked)
			{
				_gateNavMeshBlocker.Unblock();
			}

			_gateUpdater.Remove((Timberborn.AutomationBuildings.Gate)(object)this);
		}

		public void Save(IEntitySaver entitySaver)
		{
			entitySaver.GetComponent(ComponentKey).Set(OpeningModeKey, _gateOpeningMode);
		}

		public void Load(IEntityLoader entityLoader)
		{
			IObjectLoader component = entityLoader.GetComponent(ComponentKey);
			_gateOpeningMode = component.Get(OpeningModeKey);
		}

		public void OnEnterFinishedState()
		{
			UpdateState();
		}

		public void OnExitFinishedState()
		{
		}

		public void DuplicateFrom(Gate source)
		{
			_gateOpeningMode = source._gateOpeningMode;
			UpdateState();
		}

		public void Evaluate()
		{
			if (_gateOpeningMode == GateOpeningMode.Automated)
			{
				UpdateState();
			}
		}

		public void Open()
		{
			SetOpeningMode(GateOpeningMode.Open);
		}

		public void Close()
		{
			SetOpeningMode(GateOpeningMode.Closed);
		}

		public void Automate()
		{
			SetOpeningMode(GateOpeningMode.Automated);
		}

		public void EnableConflict()
		{
			IsConflict = true;
			NotifyStateChanged();
		}

		public void DisableConflict()
		{
			IsConflict = false;
			NotifyStateChanged();
		}

		public void BlockNavMesh()
		{
			_gateNavMeshBlocker.Block();
			NotifyStateChanged();
		}

		public void UnblockNavMesh()
		{
			_gateNavMeshBlocker.Unblock();
			NotifyStateChanged();
		}

		public void SetOpeningMode(GateOpeningMode gateOpeningMode)
		{
			if (_gateOpeningMode != gateOpeningMode)
			{
				_gateOpeningMode = gateOpeningMode;
				UpdateState();
			}
		}

		public void UpdateState()
		{
			if (_blockObject.IsFinished)
			{
				if (_gateOpeningMode == GateOpeningMode.Open || IsOpenByAutomation)
				{
					_gateUpdater.ScheduleToOpen((Timberborn.AutomationBuildings.Gate)(object)this);
				}
				else
				{
					_gateUpdater.ScheduleToClose((Timberborn.AutomationBuildings.Gate)(object)this);
				}
			}
		}

		public void NotifyStateChanged()
		{
			this.StateChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
