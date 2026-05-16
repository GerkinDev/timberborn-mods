using GerkinDev.PressurePlates.Services;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using UnityEngine;
using static GerkinDev.PressurePlates.Services.OccupantDetectorService;

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

		private BlockOccupant _CreateFakeBeaver()
		{
			var comp = new EntityComponent(null, _entityRegistry);
			var emptyGameObject = new GameObject();
			emptyGameObject.AddComponent<ComponentCache>();
			var cc = emptyGameObject.GetComponent<ComponentCache>();
			var blockOccupant = new BlockOccupant();
			cc.InjectDependencies(_compCacheService, null);
			cc.AddEnabledComponent(blockOccupant);
			cc.Initialize(new() { blockOccupant }, "test", null);
			comp.RegisteredComponents.Add(blockOccupant);
			_entityRegistry.Register(comp);
			return blockOccupant;
		}

		private Vector3Int _GameToUnityPosition(Vector3Int position) =>
			new Vector3Int(position.x, position.z, position.y);

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
		public void ShouldInstanciate()
		{
			Assert.IsNotNull(_occupantDetectorService);
		}

		[Test]
		public void ShouldScanWithOneBeaverButNoSubscriber()
		{
			var beaver = _CreateFakeBeaver();
			beaver.Transform.position = _GameToUnityPosition(new(1, 2, 3));
			_occupantDetectorService.FullScan();
		}

		[Test]
		public void ShouldScanWithOneBeaverAndOneSubscriber()
		{
			var beaver = _CreateFakeBeaver();
			beaver.Transform.position = _GameToUnityPosition(new(1, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Never());
		}

		[Test]
		public void ShouldScanWithTwoBeaversAndOneMultiCellSubscriber()
		{
			var beaver1 = _CreateFakeBeaver();
			beaver1.Transform.position = _GameToUnityPosition(new(1, 2, 3));
			var beaver2 = _CreateFakeBeaver();
			beaver2.Transform.position = _GameToUnityPosition(new(2, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3), new Vector3Int(2, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Never());
		}

		[Test]
		public void ShouldScanWithOneBeaverAndOneSubscriberNoDispatchUnchanged()
		{
			var beaver = _CreateFakeBeaver();
			beaver.Transform.position = _GameToUnityPosition(new(1, 2, 3));
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
			beaver.Transform.position = _GameToUnityPosition(new(1, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Never());
			beaver.Transform.position = _GameToUnityPosition(new(5, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
		}

		[Test]
		public void ShouldScanWithEnterExitEnter()
		{
			var beaver = _CreateFakeBeaver();
			beaver.Transform.position = _GameToUnityPosition(new(1, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Never());
			beaver.Transform.position = _GameToUnityPosition(new(5, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
			beaver.Transform.position = _GameToUnityPosition(new(1, 2, 3));
			_occupantDetectorService.FullScan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Exactly(2));
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupancyEvent>()), Times.Once());
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
						beaver.Transform.position = _GameToUnityPosition(new(x, y, z));
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