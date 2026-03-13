using System;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using UnityEngine;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Extensions
{
	public static class BaseComponentExtensions
	{
		public static void Log(this BaseComponent component, string format, params object[] args)
		{
			var logStr = string.Format(format, args);
			string who;
			try
			{
				who = component.Name;
				who += "@" + component.GameObject.GetEntityId();
				if (component.TryGetComponent<BlockObject>(out var blockObject))
				{
					who += "@" + blockObject.Placement.Coordinates;
				}
			}
			catch (NullReferenceException)
			{
				who = "Unknown";
			}
			logStr = $"[{who}] {logStr}";
			Debug.Log(logStr);
		}
	}
}
