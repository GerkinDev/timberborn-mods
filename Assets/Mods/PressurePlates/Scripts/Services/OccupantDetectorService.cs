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
		private const float _PARTITION_DISTANCE = 2f;
		private readonly EntityComponentRegistry _entityComponentRegistry;

		private readonly Dictionary<Subscriber, ImmutableArray<BlockOccupant>> _partitions = new();
		private readonly Stopwatch _stopwatch = new();

		private readonly Dictionary<object, Subscriber> _subscribers = new();
		private readonly Dictionary<Subscriber, SubscriberState> _subscribersState = new();

		public OccupantDetectorService(EntityComponentRegistry entityComponentRegistry)
		{
			_entityComponentRegistry = entityComponentRegistry;
		}

		#region ITickableSingleton

		public void Tick() => FullScan();

		#endregion

		/// <summary>
		///     Find beavers near watched positions. Beavers within the partitions will be checked on each frame in
		///     <see cref="ScanPartitions" />
		/// </summary>
		public void BuildPartitions()
		{
			_partitions.Clear();
			if (_subscribers.Count == 0)
			{
				return;
			}

			_stopwatch.Restart();
			ImmutableArray<BlockOccupant> occupants =
				_entityComponentRegistry.GetEnabled<BlockOccupant>().ToImmutableArray();
			foreach (Subscriber? subscriber in _subscribers.Values)
			{
				List<BlockOccupant> subscriberPartitionOccupants = new(occupants.Length / 2);
				List<BlockOccupant> tempOccupants = occupants.ToList();
				foreach (Vector3Int cell in subscriber.Positions)
				{
					for (int i = 0; i < tempOccupants.Count; i++)
					{
						BlockOccupant? occupant = tempOccupants[i];
						float distance = Vector3.Distance(occupant.GridCoordinates, cell);
						// Add to partition, remove from further checks
						if (distance < _PARTITION_DISTANCE)
						{
							subscriberPartitionOccupants.Add(occupant);
							tempOccupants.RemoveAt(i);
							i--;
						}
					}
				}

				if (subscriberPartitionOccupants.Count > 0)
				{
					_partitions[subscriber] = subscriberPartitionOccupants.ToImmutableArray();
				}
			}

			_stopwatch.Stop();
			PressurePlates.Log("Partition ended in {0}ms", _stopwatch.Elapsed.TotalMilliseconds);
		}

		public bool ScanPartitions()
		{
			if (_partitions.Count == 0 && _subscribersState.Count == 0)
			{
				return false;
			}

			_stopwatch.Restart();
			Dictionary<Subscriber, HashSet<BlockOccupant>> subscriberCurrentOccupants = new();
			// Ensure previously occupied subscriber will be checked even if no one is within
			foreach (Subscriber? subscriber in _subscribersState.Keys)
			{
				subscriberCurrentOccupants.Add(subscriber, new());
			}

			// Check each partition
			foreach ((Subscriber? subscriber, ImmutableArray<BlockOccupant> partitionOccupants) in _partitions)
			{
				Dictionary<Vector3Int, IGrouping<Vector3Int, BlockOccupant>> occupantPositions = partitionOccupants
					.GroupBy(occupant => Vector3Int.FloorToInt(occupant.GridCoordinates))
					.ToDictionary(group => group.Key, group => group);
				foreach (Vector3Int cell in subscriber.Positions)
				{
					if (occupantPositions.Remove(cell, out IGrouping<Vector3Int, BlockOccupant>? cellOccupants))
					{
						// Occupants are in a single cell. When matched, remove them from check list
						HashSet<BlockOccupant>? subscriberOccupants =
							subscriberCurrentOccupants.GetOrAdd(subscriber, () => new());
						subscriberOccupants.UnionWith(cellOccupants);
					}
					else
					{
						subscriberCurrentOccupants.GetOrAdd(subscriber, () => new());
					}
				}
			}

			bool dispatched = false;
			foreach ((Subscriber? subscriber, HashSet<BlockOccupant>? occupants) in subscriberCurrentOccupants)
			{
				SubscriberState? subscriberState = _subscribersState.GetOrDefault(subscriber);
				OccupancyEvent e;
				if (subscriberState == null)
				{
					if (occupants.Count == 0) // No previous occupants, no current occupants, nothing to do
					{
						continue;
					}

					ImmutableArray<BlockOccupant> immutableOccupants = occupants.ToImmutableArray();
					e = new()
					{
						Entered = immutableOccupants,
						Exited = ImmutableArray<BlockOccupant>.Empty,
						Within = immutableOccupants
					};
					_subscribersState[subscriber] = new() { Within = occupants };
				}
				else
				{
					if (subscriberState.Within.SetEquals(occupants)) // No occupants changes
					{
						continue;
					}

					ImmutableArray<BlockOccupant> exited = subscriberState.Within.Except(occupants).ToImmutableArray();
					ImmutableArray<BlockOccupant> entered = occupants.Except(subscriberState.Within).ToImmutableArray();
					subscriberState.Within = occupants;

					e = new() { Entered = entered, Exited = exited, Within = occupants.ToImmutableArray() };
				}

				if (e.Entered.Any())
				{
					dispatched = true;
					subscriber.DispatchEnter(e);
				}

				if (e.Exited.Any())
				{
					dispatched = true;
					subscriber.DispatchExit(e);
				}
			}

			PressurePlates.Log("Scan ended in {0}ms", _stopwatch.Elapsed.TotalMilliseconds);
			_stopwatch.Stop();
			return dispatched;
		}

		public bool FullScan()
		{
			BuildPartitions();
			return ScanPartitions();
		}

		public Subscriber Subscribe(object key, BlockObject blockObject) =>
			Subscribe(key,
				blockObject.Blocks.GetAllCoordinates().Select(relCoords => blockObject.TransformCoordinates(relCoords))
					.ToArray());

		public Subscriber Subscribe(object key, params Vector3Int[] position)
		{
			Subscriber subscriber = new() { Key = key, Positions = position };
			_subscribers.Add(key, subscriber);
			return subscriber;
		}

		public void Unsubscribe(object key) => _subscribers.Remove(key);

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
			public ImmutableArray<BlockOccupant> Exited { get; init; }
			public ImmutableArray<BlockOccupant> Within { get; init; }
		}

		private class SubscriberState
		{
			public HashSet<BlockOccupant> Within { get; set; } = new();
		}
	}
}