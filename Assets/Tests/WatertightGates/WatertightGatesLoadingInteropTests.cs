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
			var compCacheService = new ComponentCacheService();
			var componentRegistry = new RegisteredComponentService();
			var entityRegistry = new EntityComponentRegistry(componentRegistry);
			var comp = new EntityComponent(null, entityRegistry);
			var emptyGameObject = new GameObject();
			emptyGameObject.AddComponent<ComponentCache>();
			var cc = emptyGameObject.GetComponent<ComponentCache>();
			var child = new GameObject("anchor");
			child.transform.parent = emptyGameObject.transform;
			cc.InjectDependencies(compCacheService, new());

			var quickNotifService = new QuickNotificationService();
			var gate = new WatertightGate(null, quickNotifService);
			List<object> awakeComponents = new()
			{
				new BlockObjectSpec { Size = new(1, 1, 1), Blocks = ImmutableArray.Create(new BlockSpec { }) },
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
				new WatertightGateSpec { Anchor = "anchor", OpenTransform = new(), CloseTransform = new(), },
				new NavMeshBlocker(default, default, default),
			};
			cc.Initialize(awakeComponents.Concat(instantiatedComponents).ToList(), "test", new());
			foreach (var instantiatedComponent in awakeComponents)
			{
				if (instantiatedComponent is IAwakableComponent awakableComponent)
					awakableComponent.Awake();
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
					EGateControlMode.Open,
					EGateMode.Open,
					EGateMode.Close
				).SetName("No persistence");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>() { { _persistenceKey.Name, new() { } } },
					EGateControlMode.Open,
					EGateMode.Open,
					EGateMode.Close
				).SetName("No data in object loader");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>()
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "invalid" },
								{ "_activeGateMode", "invalid" },
								{ "_inactiveGateMode", "invalid" },
							}
						}
					},
					EGateControlMode.Open,
					EGateMode.Open,
					EGateMode.Close
				).SetName("Activation: invalid");
			}
		}

		[TestCaseSource(typeof(WatertightGatesLoadingInteropTests), nameof(Data_Other))]
		public void LoadFrom_Other(
			Dictionary<string, Dictionary<string, object>> saveData,
			object expectedActivationMode,
			object expectedActiveGateMode,
			object expectedInactiveGateMode
		)
		{
			WatertightGate gate = _InitGate();
			gate.Load(new MockEntityLoader(saveData));
			Assert.That(gate.ActivationMode, Is.EqualTo((EGateControlMode)expectedActivationMode));
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
					new Dictionary<string, Dictionary<string, object>>()
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "Automated" },
								{ "_activeGateMode", "Close" },
								{ "_inactiveGateMode", "Open" },
							}
						}
					},
					EGateControlMode.Automated,
					EGateMode.Close,
					EGateMode.Open
				).SetName("Activation: Automated");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>()
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "Active" },
								{ "_activeGateMode", "Pass" },
								{ "_inactiveGateMode", "Close" },
							}
						}
					},
					EGateControlMode.Pass,
					EGateMode.Pass,
					EGateMode.Close
				).SetName("Activation: active");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>()
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "Inactive" },
								{ "_activeGateMode", "Open" },
								{ "_inactiveGateMode", "Pass" },
							}
						}
					},
					EGateControlMode.Pass,
					EGateMode.Open,
					EGateMode.Pass
				).SetName("Activation: inactive");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>()
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ "_activationMode", "invalid" },
								{ "_activeGateMode", "Pass" },
								{ "_inactiveGateMode", "Pass" },
							}
						}
					},
					EGateControlMode.Open,
					EGateMode.Pass,
					EGateMode.Pass
				).SetName("Activation: invalid");
			}
		}

		[TestCaseSource(typeof(WatertightGatesLoadingInteropTests), nameof(Data_1_0_0_1))]
		public void LoadFrom_1_0_0_1(
			Dictionary<string, Dictionary<string, object>> saveData,
			object expectedActivationMode,
			object expectedActiveGateMode,
			object expectedInactiveGateMode
		)
		{
			WatertightGate gate = _InitGate();
			gate.Load(new MockEntityLoader(saveData));
			Assert.That(gate.ActivationMode, Is.EqualTo((EGateControlMode)expectedActivationMode));
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
					new Dictionary<string, Dictionary<string, object>>()
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ _activationModeKey.Name, EGateControlMode.Automated },
								{ _activeGateModeKey.Name, EGateMode.Close },
								{ _inactiveGateModeKey.Name, EGateMode.Open },
							}
						}
					},
					EGateControlMode.Automated,
					EGateMode.Close,
					EGateMode.Open
				).SetName("activation: Automated");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>()
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ _activationModeKey.Name, EGateControlMode.Open },
								{ _activeGateModeKey.Name, EGateMode.Pass },
								{ _inactiveGateModeKey.Name, EGateMode.Pass },
							}
						}
					},
					EGateControlMode.Open,
					EGateMode.Pass,
					EGateMode.Pass
				).SetName("activation: Open");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>()
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ _activationModeKey.Name, EGateControlMode.Close },
								{ _activeGateModeKey.Name, EGateMode.Pass },
								{ _inactiveGateModeKey.Name, EGateMode.Pass },
							}
						}
					},
					EGateControlMode.Close,
					EGateMode.Pass,
					EGateMode.Pass
				).SetName("activation: Close");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>()
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ _activationModeKey.Name, EGateControlMode.Pass },
								{ _activeGateModeKey.Name, EGateMode.Pass },
								{ _inactiveGateModeKey.Name, EGateMode.Pass },
							}
						}
					},
					EGateControlMode.Pass,
					EGateMode.Pass,
					EGateMode.Pass
				).SetName("activation: pass");
				yield return new TestCaseData(
					new Dictionary<string, Dictionary<string, object>>()
					{
						{
							_persistenceKey.Name,
							new()
							{
								{ _activationModeKey.Name, "Nope" },
								{ _activeGateModeKey.Name, EGateMode.Pass },
								{ _inactiveGateModeKey.Name, EGateMode.Pass },
							}
						}
					},
					EGateControlMode.Open,
					EGateMode.Pass,
					EGateMode.Pass
				).SetName("activation: invalid");
			}
		}

		[TestCaseSource(typeof(WatertightGatesLoadingInteropTests), nameof(Data_Current))]
		public void LoadFrom_Current(
			Dictionary<string, Dictionary<string, object>> saveData,
			object expectedActivationMode,
			object expectedActiveGateMode,
			object expectedInactiveGateMode
		)
		{
			WatertightGate gate = _InitGate();
			gate.Load(new MockEntityLoader(saveData));
			Assert.That(gate.ActivationMode, Is.EqualTo((EGateControlMode)expectedActivationMode));
			Assert.That(gate.ActiveGateMode, Is.EqualTo((EGateMode)expectedActiveGateMode));
			Assert.That(gate.InactiveGateMode, Is.EqualTo((EGateMode)expectedInactiveGateMode));
		}

		#endregion
	}
}