using UnityEngine;

namespace GerkinDev.PressurePlates.Services
{
	/// <see cref="Timberborn.TickSystem.TickerUnityAdapter">
	/// <see cref="Timberborn.TickSystem.TickService">
	internal class TickMaster : MonoBehaviour
	{
		public OccupantDetectorService? OccupantDetectorService { get; internal set; }
		public float ScanInterval { get; internal set; }
		private float _timeSinceLastDispatch = 0f;

		public void Update()
		{
			if (OccupantDetectorService == null)
			{
				return;
			}
			if (Time.deltaTime == 0)
			{
				return;
			}
			_timeSinceLastDispatch += Time.deltaTime;
			if (_timeSinceLastDispatch > ScanInterval)
			{
				_timeSinceLastDispatch = 0;
				OccupantDetectorService.ScanPartitions();
			}
		}
	}
}
