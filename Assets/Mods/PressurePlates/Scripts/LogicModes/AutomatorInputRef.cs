using System;
using Timberborn.Automation;
using Timberborn.EntitySystem;

namespace GerkinDev.PressurePlates.LogicModes
{
	public class AutomatorInputRef : IDisposable
	{
		private readonly AutomatorConnection _connection;
		private readonly AutomatorRegistry _registry;
		private Guid? _automatorId;
		private bool _loaded;

		public AutomatorInputRef(Automator owner, AutomatorRegistry registry)
		{
			_registry = registry;
			_connection = owner.AddInput();
		}

		public bool BooleanState => _connection.BooleanState;

		public Automator? Automator
		{
			get => _connection.Transmitter;
			set
			{
				var oldValue = _connection.Transmitter;
				if (value is null)
				{
					_connection.Disconnect();
				}
				else
				{
					_connection.Connect(value);
				}

				Changed?.Invoke(oldValue);
			}
		}

		public Guid? AutomatorId
		{
			get => _loaded ? Automator?.GetComponent<EntityComponent>().EntityId : _automatorId;
			set
			{
				if (!_loaded)
				{
					_automatorId = value;
					return;
				}

				if (!value.HasValue)
				{
					Automator = null;
					return;
				}

				var target = _registry.FindTransmitterById(value.Value);
				if (target is null)
				{
					PressurePlates.Log("Failed to load automator input target with id {0}", value);
				}

				Automator = target;
			}
		}

		public void Dispose()
		{
			_connection.Remove();
			foreach (var invocation in Changed?.GetInvocationList() ?? Array.Empty<Delegate>())
			{
				Changed -= (Action<Automator?>)invocation;
			}
		}

		public event Action<Automator?>? Changed;

		public void Load()
		{
			if (_loaded)
			{
				return;
			}

			_loaded = true;
			if (_automatorId.HasValue)
			{
				AutomatorId = _automatorId.Value;
			}
		}
	}
}