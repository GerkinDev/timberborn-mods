using GerkinDev.Tests.Utils;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components;
using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.Specs;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Timberborn.Automation;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.Illumination;
using Timberborn.QuickNotificationSystem;
using Timberborn.TransformControl;
using UnityEngine;
using static GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Components.WatertightGate;

namespace GerkinDev.Tests.WatertightGates
{
	public class WatertightGatesLoadingInteropTests
	{
		private static WatertightGate _InitGate()
		{
			ComponentCacheService compCacheService = new();
			RegisteredComponentService componentRegistry = new();
			EntityComponentRegistry entityRegistry = new(componentRegistry);
			EntityComponent comp = new(null, entityRegistry);
			GameObject emptyGameObject = new();
			emptyGameObject.AddComponent<ComponentCache>();
			ComponentCache? cc = emptyGameObject.GetComponent<ComponentCache>();
			GameObject child = new("anchor");
			child.transform.parent = emptyGameObject.transform;
			cc.InjectDependencies(compCacheService, new());

			QuickNotificationService quickNotifService = new();
			WatertightGate gate = new(null, quickNotifService);
			List<object> awakeComponents = new()
			{
				new BlockObjectSpec { Size = new(1, 1, 1), Blocks = ImmutableArray.Create(new BlockSpec()) },
				new TransformController(),
				new BlockObjectState(null),
				new BlockObject(default, default, default, default, default, default),
				new Automator(default),
				new Automatable(default),
				new Illuminator(default, default),
				new WatertightGateTransformController(),
				gate
			};
			List<object> instantiatedComponents = new()
			{
				new WatertightGateSpec { Anchor = "anchor", OpenTransform = new(), CloseTransform = new() },
				new NavMeshBlocker(default, default, default)
			};
			cc.Initialize(awakeComponents.Concat(instantiatedComponents).ToList(), "test", new());
			foreach (object? instantiatedComponent in awakeComponents)
			{
				if (instantiatedComponent is IAwakableComponent awakableComponent)
				{
					awakableComponent.Awake();
				}
			}

			entityRegistry.Register(comp);

			gate.Awake();
			return gate;
		}

		#region Empty/invalid

		public static IEnumerable Data_Other
		{
			get
			{
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>(),
					EGateMainMode.OPEN,
					EGateMode.OPEN,
					EGateMode.CLOSE
				).SetName("No persistence");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>> { { _persistenceKey.Name, new() } },
					EGateMainMode.OPEN,
					EGateMode.OPEN,
					EGateMode.CLOSE
				).SetName("No data in object loader");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "invalid" },
								{ "_mainMode", "invalid" },
								{ "_activeGateMode", "invalid" },
								{ "_inactiveGateMode", "invalid" }
							}
						}
					},
					EGateMainMode.OPEN,
					EGateMode.OPEN,
					EGateMode.CLOSE
				).SetName("Activation: invalid");
			}
		}

		[TestCaseSource(typeof(WatertightGatesLoadingInteropTests), nameof(Data_Other))]
		public void LoadFrom_Other(
			Dictionary<string, Dictionary<string, object>> saveData,
			object expectedMainMode,
			object expectedActiveGateMode,
			object expectedInactiveGateMode
		)
		{
			WatertightGate gate = _InitGate();
			gate.Load(new MockEntityLoader(saveData));
			Assert.That(gate.MainMode, Is.EqualTo((EGateMainMode)expectedMainMode));
			Assert.That(gate.ActiveGateMode, Is.EqualTo((EGateMode)expectedActiveGateMode));
			Assert.That(gate.InactiveGateMode, Is.EqualTo((EGateMode)expectedInactiveGateMode));
		}

		#endregion

		#region 1.0.0.1

		public static IEnumerable Data_1_0_0_1
		{
			get
			{
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "Automated" },
								{ "_activeGateMode", "Close" },
								{ "_inactiveGateMode", "Open" }
							}
						}
					},
					EGateMainMode.AUTOMATED,
					EGateMode.CLOSE,
					EGateMode.OPEN
				).SetName("Activation: automated");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "Active" },
								{ "_activeGateMode", "Pass" },
								{ "_inactiveGateMode", "Close" }
							}
						}
					},
					EGateMainMode.PASS,
					EGateMode.PASS,
					EGateMode.CLOSE
				).SetName("Activation: active");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "Inactive" },
								{ "_activeGateMode", "Open" },
								{ "_inactiveGateMode", "Pass" }
							}
						}
					},
					EGateMainMode.PASS,
					EGateMode.OPEN,
					EGateMode.PASS
				).SetName("Activation: inactive");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "invalid" },
								{ "_activeGateMode", "Pass" },
								{ "_inactiveGateMode", "Pass" }
							}
						}
					},
					EGateMainMode.OPEN,
					EGateMode.PASS,
					EGateMode.PASS
				).SetName("Activation: invalid");
			}
		}

		[TestCaseSource(typeof(WatertightGatesLoadingInteropTests), nameof(Data_1_0_0_1))]
		public void LoadFrom_1_0_0_1(
			Dictionary<string, Dictionary<string, object>> saveData,
			object expectedMainMode,
			object expectedActiveGateMode,
			object expectedInactiveGateMode
		)
		{
			WatertightGate gate = _InitGate();
			gate.Load(new MockEntityLoader(saveData));
			Assert.That(gate.MainMode, Is.EqualTo((EGateMainMode)expectedMainMode));
			Assert.That(gate.ActiveGateMode, Is.EqualTo((EGateMode)expectedActiveGateMode));
			Assert.That(gate.InactiveGateMode, Is.EqualTo((EGateMode)expectedInactiveGateMode));
		}

		#endregion

		#region 1.0.1.2

		public static IEnumerable Data_1_0_1_2
		{
			get
			{
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "Automated" },
								{ "_activeGateMode", "Close" },
								{ "_inactiveGateMode", "Open" }
							}
						}
					},
					EGateMainMode.AUTOMATED,
					EGateMode.CLOSE,
					EGateMode.OPEN
				).SetName("Activation: automated");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "Open" },
								{ "_activeGateMode", "Pass" },
								{ "_inactiveGateMode", "Close" }
							}
						}
					},
					EGateMainMode.OPEN,
					EGateMode.PASS,
					EGateMode.CLOSE
				).SetName("Activation: open");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "Close" },
								{ "_activeGateMode", "Open" },
								{ "_inactiveGateMode", "Pass" }
							}
						}
					},
					EGateMainMode.CLOSE,
					EGateMode.OPEN,
					EGateMode.PASS
				).SetName("Activation: close");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "invalid" },
								{ "_activeGateMode", "Pass" },
								{ "_inactiveGateMode", "Pass" }
							}
						}
					},
					EGateMainMode.OPEN,
					EGateMode.PASS,
					EGateMode.PASS
				).SetName("Activation: invalid");
			}
		}

		[TestCaseSource(typeof(WatertightGatesLoadingInteropTests), nameof(Data_1_0_1_2))]
		public void LoadFrom_1_0_1_2(
			Dictionary<string, Dictionary<string, object>> saveData,
			object expectedMainMode,
			object expectedActiveGateMode,
			object expectedInactiveGateMode
		)
		{
			WatertightGate gate = _InitGate();
			gate.Load(new MockEntityLoader(saveData));
			Assert.That(gate.MainMode, Is.EqualTo((EGateMainMode)expectedMainMode));
			Assert.That(gate.ActiveGateMode, Is.EqualTo((EGateMode)expectedActiveGateMode));
			Assert.That(gate.InactiveGateMode, Is.EqualTo((EGateMode)expectedInactiveGateMode));
		}

		#endregion

		#region Current

		public static IEnumerable Data_Current
		{
			get
			{
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ _mainModeKey.Name, EGateMainMode.AUTOMATED },
								{ _activeGateModeKey.Name, EGateMode.CLOSE },
								{ _inactiveGateModeKey.Name, EGateMode.OPEN }
							}
						}
					},
					EGateMainMode.AUTOMATED,
					EGateMode.CLOSE,
					EGateMode.OPEN
				).SetName("main: automated");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ _mainModeKey.Name, EGateMainMode.OPEN },
								{ _activeGateModeKey.Name, EGateMode.PASS },
								{ _inactiveGateModeKey.Name, EGateMode.PASS }
							}
						}
					},
					EGateMainMode.OPEN,
					EGateMode.PASS,
					EGateMode.PASS
				).SetName("main: open");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ _mainModeKey.Name, EGateMainMode.CLOSE },
								{ _activeGateModeKey.Name, EGateMode.PASS },
								{ _inactiveGateModeKey.Name, EGateMode.PASS }
							}
						}
					},
					EGateMainMode.CLOSE,
					EGateMode.PASS,
					EGateMode.PASS
				).SetName("main: close");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ _mainModeKey.Name, EGateMainMode.PASS },
								{ _activeGateModeKey.Name, EGateMode.PASS },
								{ _inactiveGateModeKey.Name, EGateMode.PASS }
							}
						}
					},
					EGateMainMode.PASS,
					EGateMode.PASS,
					EGateMode.PASS
				).SetName("main: pass");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ _mainModeKey.Name, "Nope" },
								{ _activeGateModeKey.Name, EGateMode.PASS },
								{ _inactiveGateModeKey.Name, EGateMode.PASS }
							}
						}
					},
					EGateMainMode.OPEN,
					EGateMode.PASS,
					EGateMode.PASS
				).SetName("main: invalid");
			}
		}

		[TestCaseSource(typeof(WatertightGatesLoadingInteropTests), nameof(Data_Current))]
		public void LoadFrom_Current(
			Dictionary<string, Dictionary<string, object>> saveData,
			object expectedMainMode,
			object expectedActiveGateMode,
			object expectedInactiveGateMode
		)
		{
			WatertightGate gate = _InitGate();
			gate.Load(new MockEntityLoader(saveData));
			Assert.That(gate.MainMode, Is.EqualTo((EGateMainMode)expectedMainMode));
			Assert.That(gate.ActiveGateMode, Is.EqualTo((EGateMode)expectedActiveGateMode));
			Assert.That(gate.InactiveGateMode, Is.EqualTo((EGateMode)expectedInactiveGateMode));
		}

		#endregion
	}
}