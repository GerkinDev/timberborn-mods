using System;
using System.Text.Json.Nodes;
using GerkinDev.PressurePlates.LogicModes;
using Timberborn.Automation;

namespace GerkinDev.PressurePlates.Services
{
	public class LogicModeSerializer
	{
		private readonly AutomatorRegistry _automatorRegistry;
		private readonly PressurePlateVersionService _pressurePlateVersionService;

		public LogicModeSerializer(PressurePlateVersionService pressurePlateVersionService,
			AutomatorRegistry automatorRegistry)
		{
			_pressurePlateVersionService = pressurePlateVersionService;
			_automatorRegistry = automatorRegistry;
		}

		public IPressurePlateLogicMode Deserialize(Automator automator, string? saved)
		{
			if (saved is null)
			{
				return new CountLatch(automator, _automatorRegistry);
			}

			var jsonDoc = (JsonObject)(JsonNode.Parse(saved) ?? new JsonObject());
			if (!(
					jsonDoc.TryGetPropertyValue("type", out var typeNode) &&
					(typeNode?.AsValue().TryGetValue<string>(out var type) ?? false) &&
					jsonDoc.TryGetPropertyValue("state", out var stateNode) &&
					stateNode?.AsObject() is { } stateNodeObject
				))
			{
				PressurePlates.Warn(format: "Failed to deserialize state", saved);
				return new CountLatch(automator, _automatorRegistry);
			}

			PressurePlates.Log(format: "Loading logic mode {0} with state {1}", type, stateNodeObject.ToJsonString());
			return type switch
			{
				nameof(CountLatch) => CountLatch.Load(automator, stateNodeObject,
					_pressurePlateVersionService.PreviousVersion, _automatorRegistry),
				_ => throw new IndexOutOfRangeException()
			};
		}

		public string Serialize(IPressurePlateLogicMode logicMode)
		{
			var obj = new JsonObject
			{
				{ "type", logicMode.GetType().Name },
				{ "state", logicMode.SerializeState() }
			};
			var stringified = obj.ToJsonString();
			PressurePlates.Log(format: "Serialized state to {0}", stringified);
			return obj.ToJsonString();
		}

		public class Helpers
		{
		}
	}
}