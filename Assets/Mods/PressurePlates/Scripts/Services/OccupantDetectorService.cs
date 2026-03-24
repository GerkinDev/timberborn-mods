using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.EntitySystem;
using Timberborn.TickSystem;
using UnityEngine;

namespace GerkinDev.PressurePlates.Assets.Mods.PressurePlates.Scripts.Services
{
	/// <see cref="BlockOccupancyService">
	public class OccupantDetectorService : ITickableSingleton
	{
		public class Subscriber
		{
			private static int _instancesCount = 0;
			public int Id { get; } = _instancesCount++;
			public Vector3Int[] Positions { get; init; }
			public object Key { get; init; }
			public event EventHandler<OccypancyEvent> OnEnter;
			internal void DispatchEnter(OccypancyEvent e)
			{
				OnEnter?.Invoke(this, e);
			}
			public event EventHandler<OccypancyEvent> OnExit;
			internal void DispatchExit(OccypancyEvent e)
			{
				OnExit?.Invoke(this, e);
			}
			public override string ToString() => $"OccupancySubscriber@{Id}{{{string.Join(',', Positions)}}}";
		}
		public readonly struct OccypancyEvent
		{
			public ImmutableArray<BlockOccupant> Entered { get; init; }
			public ImmutableArray<BlockOccupant> Left { get; init; }
			public ImmutableArray<BlockOccupant> Within { get; init; }
		}
		private class SubscriberState
		{
			public HashSet<BlockOccupant> Within { get; set; } = new();
		}
		private readonly Dictionary<object, Subscriber> _subscribers = new();
		private readonly Dictionary<Vector3Int, HashSet<Subscriber>> _posToSubscribers = new();
		private readonly Dictionary<Subscriber, SubscriberState> _subscribersState = new();
		private readonly EntityComponentRegistry _entityComponentRegistry;

		public OccupantDetectorService(EntityComponentRegistry entityComponentRegistry)
		{
			_entityComponentRegistry = entityComponentRegistry;
		}

		#region ITickableSingleton
		public void Tick()
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			stopwatch.Start();
			if (Scan())
			{
				PressurePlates.Log("Scan ended in {0}ms", stopwatch.ElapsedMilliseconds);
			}
			stopwatch.Stop();
		}
		#endregion

		public bool Scan()
		{
			if (_subscribers.Count == 0)
			{
				return false;
			}
			var dispatchesCache = new Dictionary<Subscriber, HashSet<BlockOccupant>>();
			IEnumerable<BlockOccupant> occupants = _entityComponentRegistry.GetEnabled<BlockOccupant>();
			var occupantPositions = occupants.GroupBy(occupant => Vector3Int.FloorToInt(occupant.GridCoordinates)).ToDictionary(group => group.Key, group => group);
			foreach (var (cell, subscribers) in _posToSubscribers)
			{
				if (occupantPositions.TryGetValue(cell, out var cellOccupants))
				{
					// Occupants are in a single cell. When matched, remove them from check list
					occupantPositions.Remove(cell);
					foreach (var sub in subscribers)
					{
						var subscriberOccupants = dispatchesCache.GetOrAdd(sub, () => new HashSet<BlockOccupant>());
						subscriberOccupants.UnionWith(cellOccupants);
					}
				}
				else
				{
					foreach (var sub in subscribers)
					{
						dispatchesCache.GetOrAdd(sub, () => new HashSet<BlockOccupant>());
					}
				}
			}
			var dispatched = false;
			foreach (var (subscriber, subscriberOccupants) in dispatchesCache)
			{
				var subscriberState = _subscribersState.GetOrAdd(subscriber, () => new SubscriberState());
				if (subscriberState.Within.SetEquals(subscriberOccupants))
				{
					continue;
				}
				var exited = subscriberState.Within.Except(subscriberOccupants).ToHashSet();
				var entered = subscriberOccupants.Except(subscriberState.Within).ToHashSet();

				_subscribersState[subscriber] = subscriberState;
				OccypancyEvent e = new()
				{
					Entered = entered.ToImmutableArray(),
					Left = exited.ToImmutableArray(),
					Within = subscriberOccupants.ToImmutableArray()
				};
				if (entered.Any())
				{
					dispatched = true;
					subscriber.DispatchEnter(e);
				}
				if (exited.Any())
				{
					dispatched = true;
					subscriber.DispatchExit(e);
				}
				subscriberState.Within = subscriberOccupants;
				_subscribersState[subscriber] = subscriberState;
			}
			return dispatched;
		}

		private void _RebuildPosToSubscribers()
		{
			_posToSubscribers.Clear();
			var buildDict = new Dictionary<Vector3Int, List<Subscriber>>();
			foreach (var subscriber in _subscribers.Values)
			{
				foreach (var position in subscriber.Positions)
				{
					var subscribersAtPos = _posToSubscribers.GetOrAdd(position, () => new HashSet<Subscriber>());
					subscribersAtPos.Add(subscriber);
				}
			}
		}

		public Subscriber Subscribe(object key, BlockObject blockObject) =>
			Subscribe(key, blockObject.Blocks.GetAllCoordinates().Select(relCoords => blockObject.TransformCoordinates(relCoords)).ToArray());

		public Subscriber Subscribe(object key, params Vector3Int[] position)
		{
			var subscriber = new Subscriber { Key = key, Positions = position };
			_subscribers.Add(key, subscriber);
			_RebuildPosToSubscribers();
			return subscriber;
		}

		public void Unsubscribe(object key)
		{
			_subscribers.Remove(key);
			_RebuildPosToSubscribers();
		}
	}
}
