using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Utils
{
	internal static class UIUtils
	{
		public static void DebugNode(this VisualElement debugged)
		{
			foreach (VisualElement? child in debugged.Query().ToList())
			{
				List<string> path = new();
				VisualElement? curr = child;
				while (curr != null && curr != debugged)
				{
					path.Add(curr.name + string.Join("", curr.GetClasses().Select(c => "." + c)));
					curr = curr.parent;
				}

				path.Reverse();
				Debug.Log(string.Join(" > ", path));
			}
		}
	}
}