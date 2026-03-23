using GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts;
using Moq;
using NUnit.Framework;
using System;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.TimeSystem;
using UnityEngine;

namespace GerkinDev.Tests.Assets.Tests.WatertightGates
{
	public class OccupantDetectorServiceTest
	{
		private ComponentCacheService _compCacheService;
		private RegisteredComponentService _componentRegistry;
		private EntityComponentRegistry _entityRegistry;
		private FakeDayNight _dayNight;
		private OccupantDetectorService _occupantDetectorService;

		class FakeDayNight : IDayNightCycle
		{
			public float DayLengthInSeconds => throw new NotImplementedException();

			public int DayNumber => throw new NotImplementedException();

			public float DaytimeLengthInHours => throw new NotImplementedException();

			public float NighttimeLengthInHours => throw new NotImplementedException();

			public float HoursPassedToday => throw new NotImplementedException();

			public float DayProgress => throw new NotImplementedException();

			public float PartialDayNumber => throw new NotImplementedException();

			public bool IsDaytime => throw new NotImplementedException();

			public bool IsNighttime => throw new NotImplementedException();

			public float FixedDeltaTimeInHours => throw new NotImplementedException();

			public float FluidSecondsPassedToday => throw new NotImplementedException();

			public (float start, float end) BoundsInHours(TimeOfDay timeOfDay) => throw new NotImplementedException();
			public float DayNumberHoursFromNow(float hours) => throw new NotImplementedException();
			public float FluidHoursToNextStartOf(TimeOfDay timeOfDay) => throw new NotImplementedException();
			public float HoursToNextStartOf(TimeOfDay timeOfDay) => throw new NotImplementedException();
			public int HoursToTicks(float hours) => throw new NotImplementedException();
			public void JumpTimeInHours(float hours) => throw new NotImplementedException();
			public float SecondsToHours(float seconds) => throw new NotImplementedException();
			public void SetTimeToNextDay() => throw new NotImplementedException();
			public float TicksToHours(int ticks) => throw new NotImplementedException();
		}
		[SetUp]
		public void Init()
		{
			_compCacheService = new ComponentCacheService();
			_componentRegistry = new RegisteredComponentService();
			_entityRegistry = new EntityComponentRegistry(_componentRegistry);
			_dayNight = new FakeDayNight();
			_occupantDetectorService = new OccupantDetectorService(_entityRegistry, _dayNight);
		}

		private BlockOccupant _CreateFakeBeaver()
		{
			var comp = new EntityComponent(null, _entityRegistry);
			var emptyGO = new GameObject();
			emptyGO.AddComponent<ComponentCache>();
			var cc = emptyGO.GetComponent<ComponentCache>();
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
			Mock<EventHandler<OccupantDetectorService.OccypancyEvent>> Enter,
			Mock<EventHandler<OccupantDetectorService.OccypancyEvent>> Exit
		) _InitSubscriber(out object key, params Vector3Int[] positions)
		{
			key = new { };
			var subscriber = _occupantDetectorService.Subscribe(key, positions);
			var enterMock = new Mock<EventHandler<OccupantDetectorService.OccypancyEvent>>();
			subscriber.OnEnter += enterMock.Object;
			var exitMock = new Mock<EventHandler<OccupantDetectorService.OccypancyEvent>>();
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
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccypancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccypancyEvent>()), Times.Never());
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
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccypancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccypancyEvent>()), Times.Never());
		}

		[Test]
		public void ShouldScanWithOneBeaverAndOneSubscriberNoDispatchUnchanged()
		{
			var beaver = _CreateFakeBeaver();
			beaver.Transform.position = _GameToUnityPosition(new(1, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3));
			_occupantDetectorService.Scan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccypancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccypancyEvent>()), Times.Never());
			_occupantDetectorService.Scan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccypancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccypancyEvent>()), Times.Never());
		}

		[Test]
		public void ShouldScanWithOneBeaverAndOneSubscriberDispatchLeft()
		{
			var beaver = _CreateFakeBeaver();
			beaver.Transform.position = _GameToUnityPosition(new(1, 2, 3));
			var subscriber = _InitSubscriber(out var key, new Vector3Int(1, 2, 3));
			_occupantDetectorService.Scan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccypancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccypancyEvent>()), Times.Never());
			beaver.Transform.position = _GameToUnityPosition(new(5, 2, 3));
			_occupantDetectorService.Scan();
			subscriber.Enter.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccypancyEvent>()), Times.Once());
			subscriber.Exit.Verify(_ => _(It.IsAny<object>(), It.IsAny<OccupantDetectorService.OccypancyEvent>()), Times.Once());
		}
	}
}
