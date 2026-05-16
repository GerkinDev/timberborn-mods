using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.CharacterModelSystem;
using Timberborn.Characters;
using Timberborn.Common;
using Timberborn.Coordinates;
using Timberborn.EntityNaming;
using Timberborn.EntitySystem;
using Timberborn.TickSystem;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GerkinDev.PressurePlates.Services
{
	/// <see cref="BlockOccupancyService" />
	public class OccupantDetectorService : ITickableSingleton
	{
		private const float _PARTITION_DISTANCE = 5f;
		private readonly EntityComponentRegistry _entityComponentRegistry;

		private readonly Dictionary<object, Subscriber> _subscribers = new();
		private readonly Dictionary<Subscriber, SubscriberState> _subscribersState = new();
		private readonly GameObject _tickMasterOwner;
		private readonly TickMaster _tickMaster;

		public OccupantDetectorService(EntityComponentRegistry entityComponentRegistry)
		{
			_entityComponentRegistry = entityComponentRegistry;
			PressurePlates.Log("Initializing the tickmaster");

			_tickMasterOwner = new GameObject("Ticker");
			_tickMaster = _tickMasterOwner.AddComponent<TickMaster>();
			_tickMaster.OccupantDetectorService = this;
			_tickMaster.ScanInterval = 0.2f;
		}

		#region ITickableSingleton

		public void Tick()
		{
			FullScan();
		}

		#endregion

		private readonly Dictionary<Subscriber, ImmutableArray<CharacterMeta>> _partitions = new();
		private readonly Stopwatch _stopwatch = new();

		private int _buildPartitionCount = 0;

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
				List<CharacterMeta> subscriberPartitionOccupants = new(occupants.Length / 2);
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
							if (occupant.TryGetComponent<CharacterModel>(out var characterModel))
							{
								subscriberPartitionOccupants.Add(new()
								{
									BlockOccupant = occupant, CharacterModel = characterModel
								});
							}

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
			PressurePlates.Log(
				"Partition {0} ended in {1}ms",
				_buildPartitionCount++,
				_stopwatch.Elapsed.TotalMilliseconds
			);
		}

		private int _scanCount = 0;

		public bool ScanPartitions()
		{
			if (_partitions.Count == 0 && _subscribersState.Count == 0)
			{
				return false;
			}

			_stopwatch.Restart();
			var subscriberCurrentOccupants = new Dictionary<Subscriber, HashSet<BlockOccupant>>(_subscribers.Count);
			// Ensure previously occupied subscriber will be checked even if no one is within
			foreach (Subscriber? subscriber in _subscribersState.Keys)
			{
				subscriberCurrentOccupants.Add(subscriber, new());
			}

			// Check each partition
			foreach ((Subscriber? subscriber, ImmutableArray<CharacterMeta> partitionOccupants) in _partitions)
			{
				subscriberCurrentOccupants[subscriber] = _FilterOccupantsInPartition(subscriber, partitionOccupants);
			}

			bool dispatched = false;
			foreach ((Subscriber? subscriber, HashSet<BlockOccupant>? occupants) in subscriberCurrentOccupants)
			{
				dispatched |= _MaybeDispatchToSubscriber(subscriber, occupants);
			}

			PressurePlates.Log("Scan {0} ended in {1}ms", _scanCount++, _stopwatch.Elapsed.TotalMilliseconds);
			_stopwatch.Stop();
			return dispatched;
		}

		private static HashSet<BlockOccupant> _FilterOccupantsInPartition(
			Subscriber subscriber,
			ImmutableArray<CharacterMeta> toCheck
		)
		{
			var occupantPositions = toCheck
				.GroupBy(characterMeta => CoordinateSystem.WorldToGridInt(characterMeta.CharacterModel.Position))
				.ToDictionary(group => group.Key, group => group);
			var occupants = new HashSet<BlockOccupant>(occupantPositions.Count);

			foreach (var cell in subscriber.Positions)
			{
				// Occupants are in a single cell. When matched, remove them from check list
				if (occupantPositions.Remove(cell, out var cellOccupants))
				{
					occupants.UnionWith(
						cellOccupants
							.Select(cellOccupant => cellOccupant.BlockOccupant)
					);
				}
			}

			return occupants;
		}

		private bool _MaybeDispatchToSubscriber(Subscriber subscriber, HashSet<BlockOccupant> occupants)
		{
			var dispatched = false;
			var subscriberState = _subscribersState.GetOrDefault(subscriber);
			OccupancyEvent e;
			if (subscriberState == null)
			{
				if (occupants.Count == 0) // No previous occupants, no current occupants, nothing to do
				{
					return dispatched;
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
					return dispatched;
				}

				var exited = subscriberState.Within.Except(occupants).ToImmutableArray();
				var entered = occupants.Except(subscriberState.Within).ToImmutableArray();
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

			return dispatched;
		}

		public bool FullScan()
		{
			BuildPartitions();
			return ScanPartitions();
		}

		public bool ScanImmediate(object key)
		{
			var subscriber = _subscribers[key];

			var occupants = _entityComponentRegistry.GetEnabled<BlockOccupant>().ToHashSet();
			return _MaybeDispatchToSubscriber(subscriber, occupants);
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

		private readonly struct CharacterMeta
		{
			public BlockOccupant BlockOccupant { get; init; }
			public CharacterModel CharacterModel { get; init; }
		}
	}
}