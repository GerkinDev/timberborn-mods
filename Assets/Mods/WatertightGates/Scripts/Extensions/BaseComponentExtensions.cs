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
			var who = component.Name;
			who += "@" + component.GameObject.name;
			if (component.TryGetComponent<BlockObject>(out var blockObject))
			{
				who += "@" + blockObject.Placement.Coordinates;
			}
			logStr = $"[{who}] {logStr}";
			Debug.Log(logStr);
		}
	}
}
