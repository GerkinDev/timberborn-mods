using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.Versioning;
using Timberborn.WorldPersistence;

namespace GerkinDev.PressurePlates.Services
{
	public class PressurePlateVersionService : ILoadableSingleton, ISaveableSingleton
	{
		private static readonly SingletonKey _singletonKey = new(typeof(PressurePlateVersionService).FullName);
		private static readonly PropertyKey<string> _currentVersionKey = new("version");
		private readonly ModInfo _modInfo;
		private readonly ISingletonLoader _singletonLoader;

		public PressurePlateVersionService(ISingletonLoader singletonLoader, ModInfo modInfo)
		{
			_singletonLoader = singletonLoader;
			_modInfo = modInfo;
		}

		public Version? PreviousVersion { get; private set; }

		#region ILoadableSingleton

		public void Load()
		{
			string? versionStr = null;
			if (_singletonLoader.TryGetSingleton(_singletonKey, out var objectLoader))
			{
				versionStr = objectLoader.Has(_currentVersionKey) ? objectLoader.Get(_currentVersionKey) : null;
			}

			if (versionStr is not null)
			{
				PreviousVersion = Version.Create(versionStr);
			}
		}

		#endregion

		#region ISaveableSingleton

		public void Save(ISingletonSaver singletonSaver)
		{
			var saver = singletonSaver.GetSingleton(_singletonKey);
			saver.Set(_currentVersionKey, _modInfo.Version);
		}

		#endregion
	}
}