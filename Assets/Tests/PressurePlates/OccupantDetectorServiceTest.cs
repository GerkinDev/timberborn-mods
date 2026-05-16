using GerkinDev.PressurePlates.Services;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.CharacterModelSystem;
using Timberborn.EntitySystem;
using UnityEngine;
using static GerkinDev.PressurePlates.Services.OccupantDetectorService;
using Debug = UnityEngine.Debug;

namespace GerkinDev.Tests.PressurePlates
{
	public class OccupantDetectorServiceTest
	{
		private ComponentCacheService _compCacheService = null!;
		private RegisteredComponentService _componentRegistry = null!;
		private EntityComponentRegistry _entityRegistry = null!;
		private OccupantDetectorService _occupantDetectorService = null!;

		[SetUp]
		public void Init()
		{
			_compCacheService = new ComponentCacheService();
			_componentRegistry = new RegisteredComponentService();
			_entityRegistry = new EntityComponentRegistry(_componentRegistry);
			_occupantDetectorService = new OccupantDetectorService(_entityRegistry);
		}

		class FakeBeaver
		{
			public readonly CharacterModel characterModel;
			public readonly BlockOccupant blockOccupant;

			public FakeBeaver(EntityComponentRegistry entityComponentRegistry, ComponentCacheService componentCacheService)
			{
				var comp = new EntityComponent(null, entityComponentRegistry);
				var emptyGameObject = new GameObject();
				emptyGameObject.AddComponent<ComponentCache>();
				var cc = emptyGameObject.GetComponent<ComponentCache>();
				blockOccupant = new();
				var characterGameObject = new GameObject();
				characterModel = new(){ _model = characterGameObject.transform };
				cc.InjectDependencies(componentCacheService, new());
				cc.Initialize(new() { blockOccupant, characterModel }, "test", new());
				comp.RegisteredComponents.Add(blockOccupant);
				entityComponentRegistry.Register(comp);
			}

			public FakeBeaver SetPosition(Vector3 approx, Vector3? real = null)
			{
				real ??= approx;
				blockOccupant.Transform.position = approx;
				characterModel.Position = _GameToUnityPosition(real.Value);
				return this;
			}
		}

		private FakeBeaver _CreateFakeBeaver() => new FakeBeaver(_entityRegistry, _compCacheService);

		private static Vector3Int _GameToUnityPosition(Vector3Int position) =>
			new Vector3Int(position.x, position.z, position.y);

		private static Vector3 _GameToUnityPosition(Vector3 position) =>
			new Vector3(position.x, position.z, position.y);

		private int _counter = 0;

		private (
			Subscriber Subscriber,
			Mock<EventHandler<OccupancyEvent>> Enter,
			Mock<EventHandler<OccupancyEvent>> Exit
			) _InitSubscriber(out object key, params Vector3Int[] positions)
		{
			key = _counter++;
			var subscriber = _occupantDetectorService.Subscribe(key, positions);
			var enterMock = new Mock<EventHandler<OccupancyEvent>>();
			subscriber.OnEnter += enterMock.Object;
			var exitMock = new Mock<EventHandler<OccupancyEvent>>();
			subscriber.OnExit += exitMock.Object;
			return (subscriber, enterMock, exitMock);
		}

		[Test]
		public void ShouldInstantiate()
		{
			Assert.IsNotNull(_occupantDetectorService);
		}

		[Test]
		public void ShouldScanWithOneBeaverButNoSubscriber()
		{
			var beaver = _CreateFakeBeaver();
			beaver.SetPosition(new(1, 2, 3));
			_occupantDetectorService.FullScan();
		}

		[Test]
		public void ShouldScanWithOneBeaverAndOneSubscriber()
		{
			var beaver = _CreateFakeBeaver();
			beaver.SetPosition(new(1, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Never());
		}

		[Test]
		public void ShouldScanWithTwoBeaversAndOneMultiCellSubscriber()
		{
			var beaver1 = _CreateFakeBeaver();
			beaver1.SetPosition(new(1, 2, 3));
			var beaver2 = _CreateFakeBeaver();
			beaver2.SetPosition(new(2, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3), new Vector3Int(2, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Never());
		}

		[Test]
		public void ShouldScanWithOneBeaverAndOneSubscriberNoDispatchUnchanged()
		{
			var beaver = _CreateFakeBeaver();
			beaver.SetPosition(new(1, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Never());
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Never());
		}

		[Test]
		public void ShouldScanWithOneBeaverAndOneSubscriberDispatchLeft()
		{
			var beaver = _CreateFakeBeaver();
			beaver.SetPosition(new(1, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Never());
			beaver.SetPosition(new(5, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
		}

		[Test]
		public void ShouldScanWithEnterExitEnter()
		{
			var beaver = _CreateFakeBeaver();
			beaver.SetPosition(new(1, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Never());
			beaver.SetPosition(new(5, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			beaver.SetPosition(new(1, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Exactly(2));
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
		}

		[Test]
		public void ShouldScanWithBackAndForth()
		{
			var beaver = _CreateFakeBeaver();
			beaver.SetPosition(new Vector3(1f, 2f, 3f));
			var count = 0;

			void Scan()
			{
				if ((count++ % 10) == 0)
				{
					_occupantDetectorService.FullScan();
				}
				else
				{
					_occupantDetectorService.ScanPartitions();
				}
			}

			var subscriber = _InitSubscriber(out var key, new Vector3Int(5, 2, 3));
			for (var passage = 0; passage < 10; passage++)
			{
				Debug.Log("Forward");
				for (var x = 1f; x < 10f; x += .1f)
				{
					beaver.SetPosition(new Vector3(x, 2f, 3f));
					Scan();
				}

				Debug.Log("Forward end");
				subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()),
					Times.Exactly(passage * 2 + 1));
				subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()),
					Times.Exactly(passage * 2 + 1));

				Debug.Log("Backward");
				for (var x = 10f; x > 1f; x -= .1f)
				{
					beaver.SetPosition(new Vector3(x, 2f, 3f));
					Scan();
				}

				Debug.Log("Backward end");

				subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()),
					Times.Exactly(passage * 2 + 2));
				subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()),
					Times.Exactly(passage * 2 + 2));
			}
		}

		public static TestCaseData[] benchmarkSubscribers = new[]
		{
			new TestCaseData(arg: new Vector3Int[][] { new Vector3Int[] { new(4, 4, 4) } }).SetName(
				"Single at center"),
			new TestCaseData(arg: new Vector3Int[][] { new Vector3Int[] { new(4, 4, 4), new(5, 5, 5) } }).SetName(
				"Single with 2 points"),
			new TestCaseData(arg: new Vector3Int[][] { new Vector3Int[] { new(0, 0, 0) } }).SetName(
				"Single at corner"),
			new TestCaseData(arg: new Vector3Int[][] { new Vector3Int[] { new(100, 100, 100) } }).SetName(
				"Single out of bound"),
			new TestCaseData(arg: new Vector3Int[][]
			{
				new Vector3Int[] { new(0, 0, 0) }, new Vector3Int[] { new(100, 100, 100) }
			}).SetName("Two with one out of bound"),
		};

		[TestCaseSource(nameof(benchmarkSubscribers))]
		public void ShouldBeQuick(Vector3Int[][] subscribers)
		{
			for (var x = 0; x < 10; x++)
			{
				for (var y = 0; y < 10; y++)
				{
					for (var z = 0; z < 10; z++)
					{
						var beaver = _CreateFakeBeaver();
						beaver.SetPosition(new(x, y, z));
					}
				}
			}

			foreach (var positions in subscribers)
			{
				_InitSubscriber(out var key, positions);
			}

			var retries = 10;
			var times = new List<double>(retries);
			var stopWatch = new Stopwatch();
			for (var i = 0; i < retries; i++)
			{
				stopWatch.Restart();
				_occupantDetectorService.FullScan();
				stopWatch.Stop();
				times.Add(stopWatch.Elapsed.TotalMilliseconds);
			}

			TestContext.WriteLine(
				$"Time report (ms): {string.Join(";", times.Select(t => t.ToString("F2")))}; Average: {times.Average()}");
		}
	}
}