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
			public ImmutableArray<BlockOccupant> Exited { get; init; }
			public ImmutableArray<BlockOccupant> Within { get; init; }
		}
		private class SubscriberState
		{
			public HashSet<BlockOccupant> Within { get; set; } = new();
		}
		private readonly Dictionary<object, Subscriber> _subscribers = new();
		private readonly Dictionary<Subscriber, SubscriberState> _subscribersState = new();
		private readonly EntityComponentRegistry _entityComponentRegistry;
		private const float _PARTITION_DISTANCE = 2f;

		public OccupantDetectorService(EntityComponentRegistry entityComponentRegistry)
		{
			_entityComponentRegistry = entityComponentRegistry;
		}

		#region ITickableSingleton
		public void Tick()
		{
			FullScan();
		}
		#endregion

		private readonly Dictionary<Subscriber, ImmutableArray<BlockOccupant>> _partitions = new();
		private readonly Stopwatch _stopwatch = new();

		/// <summary>
		/// Find beavers near watched positions. Beavers within the partitions will be checked on each frame in <see cref="ScanPartitions"/>
		/// </summary>
		public void BuildPartitions()
		{
			_partitions.Clear();
			if (_subscribers.Count == 0)
			{
				return;
			}
			_stopwatch.Restart();
			var occupants = _entityComponentRegistry.GetEnabled<BlockOccupant>().ToImmutableArray();
			foreach (var subscriber in _subscribers.Values)
			{
				var subscriberPartitionOccupants = new List<BlockOccupant>(occupants.Length / 2);
				var tempOccupants = occupants.ToList();
				foreach (var cell in subscriber.Positions)
				{
					for (var i = 0; i < tempOccupants.Count; i++)
					{
						var occupant = tempOccupants[i];
						var distance = Vector3.Distance(occupant.GridCoordinates, cell);
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
			var subscriberCurrentOccupants = new Dictionary<Subscriber, HashSet<BlockOccupant>>();
			// Ensure previously occupied subscriber will be checked even if no one is within
			foreach (var subscriber in _subscribersState.Keys)
			{
				subscriberCurrentOccupants.Add(subscriber, new());
			}
			// Check each partition
			foreach (var (subscriber, partitionOccupants) in _partitions)
			{
				var occupantPositions = partitionOccupants.GroupBy(occupant => Vector3Int.FloorToInt(occupant.GridCoordinates)).ToDictionary(group => group.Key, group => group);
				foreach (var cell in subscriber.Positions)
				{
					if (occupantPositions.Remove(cell, out var cellOccupants))
					{
						// Occupants are in a single cell. When matched, remove them from check list
						var subscriberOccupants = subscriberCurrentOccupants.GetOrAdd(subscriber, () => new());
						subscriberOccupants.UnionWith(cellOccupants);
					}
					else
					{
						subscriberCurrentOccupants.GetOrAdd(subscriber, () => new HashSet<BlockOccupant>());
					}
				}
			}
			var dispatched = false;
			foreach (var (subscriber, occupants) in subscriberCurrentOccupants)
			{
				var subscriberState = _subscribersState.GetOrDefault(subscriber);
				OccypancyEvent e;
				if (subscriberState == null)
				{
					if (occupants.Count == 0) // No previous occupants, no current occupants, nothing to do
					{
						continue;
					}

					var immutableOccupants = occupants.ToImmutableArray();
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

					var exited = subscriberState.Within.Except(occupants).ToImmutableArray();
					var entered = occupants.Except(subscriberState.Within).ToImmutableArray();
					subscriberState.Within = occupants;

					e = new()
					{
						Entered = entered,
						Exited = exited,
						Within = occupants.ToImmutableArray()
					};
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
			Subscribe(key, blockObject.Blocks.GetAllCoordinates().Select(relCoords => blockObject.TransformCoordinates(relCoords)).ToArray());

		public Subscriber Subscribe(object key, params Vector3Int[] position)
		{
			var subscriber = new Subscriber { Key = key, Positions = position };
			_subscribers.Add(key, subscriber);
			return subscriber;
		}

		public void Unsubscribe(object key)
		{
			_subscribers.Remove(key);
		}
	}
}
