using UnityEngine;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts
{
	public class WatertightGates
	{
		public const string MOD_ID = "GerkinDev.WatertightGates";

		private static string _GetPrefix(string name)
		{
			var prefix = MOD_ID;
			if (!string.IsNullOrEmpty(prefix))
			{
				prefix += "::" + name;
			}

			return prefix;
		}
		public static void Log(string? name, string format, params object[] args)
		{
			var logStr = string.Format(format, args);
			string prefix = _GetPrefix(name);
			logStr = $"[{prefix}] {logStr}";
			Debug.Log(logStr);
		}

		public static void Log(string format, params object[] args) => Warn(null, format, args);
		public static void Warn(string? name, string format, params object[] args)
		{
			var logStr = string.Format(format, args);
			string prefix = _GetPrefix(name);
			logStr = $"[{prefix}] {logStr}";
			Debug.LogWarning(logStr);
		}

		public static void Warn(string format, params object[] args) => Warn(null, format, args);
	}
}
