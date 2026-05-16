using System;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;

namespace GerkinDev.WatertightGates.Extensions
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
				if (component.TryGetComponent<BlockObject>(out BlockObject? blockObject))
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
			string who = _GetLogPrefix(component);
			WatertightGates.Log(who, format, args);
		}

		public static void Warn(this BaseComponent component, string format, params object[] args)
		{
			string who = _GetLogPrefix(component);
			WatertightGates.Warn(who, format, args);
		}
	}
}