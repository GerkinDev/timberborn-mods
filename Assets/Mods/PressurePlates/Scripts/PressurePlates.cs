using UnityEngine;

namespace GerkinDev.PressurePlates
{
	public static class PressurePlates
	{
		public const string MOD_ID = "GerkinDev.PressurePlates";

		private static string _GetPrefix(string? name)
		{
			var prefix = MOD_ID;
			if (!string.IsNullOrEmpty(name))
			{
				prefix += "::" + name;
			}

			return prefix;
		}

		public static void Log(string? name, string format, params object?[] args)
		{
			var logStr = string.Format(format, args);
			var prefix = _GetPrefix(name);
			logStr = $"[{prefix}] {logStr}";
			Debug.Log(logStr);
		}

		public static void Log(string format, params object?[] args) => Log(null, format, args);

		public static void Warn(string? name, string format, params object?[] args)
		{
			var logStr = string.Format(format, args);
			var prefix = _GetPrefix(name);
			logStr = $"[{prefix}] {logStr}";
			Debug.LogWarning(logStr);
		}

		public static void Warn(string format, params object?[] args) => Warn(null, format, args);
	}
}