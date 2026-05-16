using GerkinDev.PressurePlates.Services;
using Moq;
using NUnit.Framework;
using System;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using UnityEngine;

namespace GerkinDev.Tests.Assets.Tests.PressurePlates
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
		private Vector3Int _GameToUnityPosition(Vector3Int position) => new Vector3Int(position.x, position.z, position.y);

		private (
			OccupantDetectorService.Subscriber Subscriber,
			Mock<EventHandler<OccupantDetectorService.OccupancyEvent>> Enter,
			Mock<EventHandler<OccupantDetectorService.OccupancyEvent>> Exit
		) _InitSubscriber(out object key, params Vector3Int[] positions)
		{
			key = new { };
			var subscriber = _occupantDetectorService.Subscribe(key, positions);
			var enterMock = new Mock<EventHandler<OccupantDetectorService.OccupancyEvent>>();
			subscriber.OnEnter += enterMock.Object;
			var exitMock = new Mock<EventHandler<OccupantDetectorService.OccupancyEvent>>();
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
			_occupantDetectorService.Scan();
		}

		[Test]
		public void ShouldScanWithOneBeaverAndOneSubscriber()
		{
			var beaver = _CreateFakeBeaver();
			beaver.Transform.position = _GameToUnityPosition(new(1, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3));
			_occupantDetectorService.Scan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccupancyEvent>()), Times.Never());
		}

		[Test]
		public void ShouldScanWithTwoBeaversAndOneMultiCellSubscriber()
		{
			var beaver1 = _CreateFakeBeaver();
			beaver1.Transform.position = _GameToUnityPosition(new(1, 2, 3));
			var beaver2 = _CreateFakeBeaver();
			beaver2.Transform.position = _GameToUnityPosition(new(2, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3), new Vector3Int(2, 2, 3));
			_occupantDetectorService.Scan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccupancyEvent>()), Times.Never());
		}

		[Test]
		public void ShouldScanWithOneBeaverAndOneSubscriberNoDispatchUnchanged()
		{
			var beaver = _CreateFakeBeaver();
			beaver.Transform.position = _GameToUnityPosition(new(1, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3));
			_occupantDetectorService.Scan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccupancyEvent>()), Times.Never());
			_occupantDetectorService.Scan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccupancyEvent>()), Times.Never());
		}

		[Test]
		public void ShouldScanWithOneBeaverAndOneSubscriberDispatchLeft()
		{
			var beaver = _CreateFakeBeaver();
			beaver.Transform.position = _GameToUnityPosition(new(1, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3));
			_occupantDetectorService.Scan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccupancyEvent>()), Times.Never());
			beaver.Transform.position = _GameToUnityPosition(new(5, 2, 3));
			_occupantDetectorService.Scan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccupancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccupancyEvent>()), Times.Once());
		}
	}
}
