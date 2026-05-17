using UnityEngine;

namespace GerkinDev.PressurePlates.Services
{
	/// <see cref="Timberborn.TickSystem.TickerUnityAdapter">
	///     <see cref="Timberborn.TickSystem.TickService">
	internal class TickMaster : MonoBehaviour
	{
		private float _timeSinceLastDispatch;
		public OccupantDetectorService? OccupantDetectorService { get; internal set; }
		public float ScanInterval { get; internal set; }

		public void Update()
		{
			if (OccupantDetectorService is null)
			{
				return;
			}

			if (Time.deltaTime == 0)
			{
				return;
			}

			_timeSinceLastDispatch += Time.deltaTime;
			if (_timeSinceLastDispatch < ScanInterval)
			{
				return;
			}

			_timeSinceLastDispatch = 0;
			OccupantDetectorService.ScanPartitions();
		}
	}
}