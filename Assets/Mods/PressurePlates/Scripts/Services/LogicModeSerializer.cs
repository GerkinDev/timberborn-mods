using System;
using System.Text.Json.Nodes;
using GerkinDev.PressurePlates.Components.LogicModes;

namespace GerkinDev.PressurePlates.Services
{
	public class LogicModeSerializer
	{
		private readonly PressurePlateVersionService _pressurePlateVersionService;

		public LogicModeSerializer(PressurePlateVersionService pressurePlateVersionService)
		{
			_pressurePlateVersionService = pressurePlateVersionService;
		}

		public IPressurePlateLogicMode? Deserialize(string saved)
		{
			var jsonDoc = (JsonObject)(JsonNode.Parse(saved) ?? new JsonObject());
			if (!(
					jsonDoc.TryGetPropertyValue("type", out var typeNode) &&
					(typeNode?.AsValue().TryGetValue<string>(out var type) ?? false) &&
					jsonDoc.TryGetPropertyValue("state", out var stateNode) &&
					stateNode?.AsObject() is { } stateNodeObject
				))
			{
				return null;
			}

			return type switch
			{
				nameof(CountLatch) => CountLatch.Load(stateNodeObject, _pressurePlateVersionService.PreviousVersion),
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
			return obj.ToString();
		}
	}
}