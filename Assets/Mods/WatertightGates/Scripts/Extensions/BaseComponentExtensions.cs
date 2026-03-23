using System;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using UnityEngine;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Extensions
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
			var logStr = string.Format(format, args);
			string who = _GetLogPrefix(component);
			logStr = $"[{WatertightGatesModStarter.MOD_ID}:{who}] {logStr}";
			Debug.Log(logStr);
		}
		public static void Warn(this BaseComponent component, string format, params object[] args)
		{
			var logStr = string.Format(format, args);
			string who = _GetLogPrefix(component);
			logStr = $"[{WatertightGatesModStarter.MOD_ID}:{who}] {logStr}";
			Debug.LogWarning(logStr);
		}
	}
}
