using System;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;

namespace GerkinDev.DamServiceGate.Assets.Mods.DamServiceGate.Scripts.Extensions
{
	public static class IEntityLoaderExtensions
	{
		public static T GetOrDefault<T>(this IEntityLoader entityLoader, ComponentKey componentKey, PropertyKey<T> propertyKey, Func<T> defaultValueFactory) where T : Enum
		{
			if (entityLoader.TryGetComponent(componentKey, out var objectLoader))
			{
				return objectLoader.Has(propertyKey) ? objectLoader.Get(propertyKey) : defaultValueFactory();
			}
			return defaultValueFactory();
		}
		public static T GetOrDefault<T>(this IEntityLoader entityLoader, ComponentKey componentKey, PropertyKey<T> propertyKey, T defaultValue) where T : Enum
			=> entityLoader.GetOrDefault(componentKey, propertyKey, () => defaultValue);
	}
}
