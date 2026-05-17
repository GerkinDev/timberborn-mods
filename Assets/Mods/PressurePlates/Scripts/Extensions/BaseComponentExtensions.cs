using System;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;

namespace GerkinDev.PressurePlates.Extensions
{
	public static class BaseComponentExtensions
	{
		private static string _GetLogPrefix(BaseComponent component)
		{
			string who;
			try
			{
				who = component.GetType().Name;
				who += ":" + component.Name;
				who += "<" + component.GameObject.GetEntityId() + ">";
				if (component.TryGetComponent<BlockObject>(out var blockObject))
				{
					who += "@" + blockObject.Placement.Coordinates;
				}
			}
			catch (NullReferenceException)
			{
				who = "Unknown";
			}

			return who;
		}

		public static void Log(this BaseComponent component, string format, params object[] args)
		{
			var who = _GetLogPrefix(component);
			PressurePlates.Log(who, format, args);
		}

		public static void Warn(this BaseComponent component, string format, params object[] args)
		{
			var who = _GetLogPrefix(component);
			PressurePlates.Warn(who, format, args);
		}
	}
}