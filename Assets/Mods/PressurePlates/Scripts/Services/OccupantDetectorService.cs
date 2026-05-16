using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Timberborn.BlockSystem;
using Timberborn.Common;
using Timberborn.EntitySystem;
using Timberborn.TickSystem;
using UnityEngine;

namespace GerkinDev.PressurePlates.Services
{
	/// <see cref="BlockOccupancyService" />
	public class OccupantDetectorService : ITickableSingleton
	{
		private readonly EntityComponentRegistry _entityComponentRegistry;
		private readonly Dictionary<Vector3Int, HashSet<Subscriber>> _posToSubscribers = new();
		private readonly Dictionary<object, Subscriber> _subscribers = new();
		private readonly Dictionary<Subscriber, SubscriberState> _subscribersState = new();

		public OccupantDetectorService(EntityComponentRegistry entityComponentRegistry)
		{
			_entityComponentRegistry = entityComponentRegistry;
		}

		#region ITickableSingleton

		public void Tick()
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
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

			Dictionary<Subscriber, HashSet<BlockOccupant>> dispatchesCache = new();
			IEnumerable<BlockOccupant> occupants = _entityComponentRegistry.GetEnabled<BlockOccupant>();
			Dictionary<Vector3Int, IGrouping<Vector3Int, BlockOccupant>> occupantPositions =
				occupants.GroupBy(occupant => Vector3Int.FloorToInt(occupant.GridCoordinates))
					.ToDictionary(group => group.Key, group => group);
			foreach ((Vector3Int cell, HashSet<Subscriber>? subscribers) in _posToSubscribers)
			{
				if (occupantPositions.TryGetValue(cell, out IGrouping<Vector3Int, BlockOccupant>? cellOccupants))
				{
					// Occupants are in a single cell. When matched, remove them from check list
					occupantPositions.Remove(cell);
					foreach (Subscriber? sub in subscribers)
					{
						HashSet<BlockOccupant>? subscriberOccupants = dispatchesCache.GetOrAdd(sub, () => new());
						subscriberOccupants.UnionWith(cellOccupants);
					}
				}
				else
				{
					foreach (Subscriber? sub in subscribers)
					{
						dispatchesCache.GetOrAdd(sub, () => new());
					}
				}
			}

			bool dispatched = false;
			foreach ((Subscriber? subscriber, HashSet<BlockOccupant>? subscriberOccupants) in dispatchesCache)
			{
				SubscriberState? subscriberState = _subscribersState.GetOrAdd(subscriber, () => new());
				if (subscriberState.Within.SetEquals(subscriberOccupants))
				{
					continue;
				}

				HashSet<BlockOccupant> exited = subscriberState.Within.Except(subscriberOccupants).ToHashSet();
				HashSet<BlockOccupant> entered = subscriberOccupants.Except(subscriberState.Within).ToHashSet();

				_subscribersState[subscriber] = subscriberState;
				OccupancyEvent e = new()
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
			Dictionary<Vector3Int, List<Subscriber>> buildDict = new();
			foreach (Subscriber? subscriber in _subscribers.Values)
			{
				foreach (Vector3Int position in subscriber.Positions)
				{
					HashSet<Subscriber>? subscribersAtPos = _posToSubscribers.GetOrAdd(position, () => new());
					subscribersAtPos.Add(subscriber);
				}
			}
		}

		public Subscriber Subscribe(object key, params Vector3Int[] position)
		{
			Subscriber subscriber = new() { Key = key, Positions = position };
			_subscribers.Add(key, subscriber);
			_RebuildPosToSubscribers();
			return subscriber;
		}

		public void Unsubscribe(object key)
		{
			_subscribers.Remove(key);
			_RebuildPosToSubscribers();
		}

		public class Subscriber
		{
			private static int _instancesCount;
			public int Id { get; } = _instancesCount++;
			public Vector3Int[] Positions { get; init; } = Array.Empty<Vector3Int>();
			public object Key { get; init; } = null!;
			public event EventHandler<OccupancyEvent> OnEnter = null!;

			internal void DispatchEnter(OccupancyEvent e) => OnEnter?.Invoke(this, e);

			public event EventHandler<OccupancyEvent> OnExit = null!;

			internal void DispatchExit(OccupancyEvent e) => OnExit?.Invoke(this, e);

			public override string ToString() => $"OccupancySubscriber@{Id}{{{string.Join(',', Positions)}}}";
		}

		public readonly struct OccupancyEvent
		{
			public ImmutableArray<BlockOccupant> Entered { get; init; }
			public ImmutableArray<BlockOccupant> Left { get; init; }
			public ImmutableArray<BlockOccupant> Within { get; init; }
		}

		private class SubscriberState
		{
			public HashSet<BlockOccupant> Within { get; set; } = new();
		}
	}
}