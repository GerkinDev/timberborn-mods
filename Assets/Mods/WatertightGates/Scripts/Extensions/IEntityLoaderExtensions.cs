using System;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;

namespace GerkinDev.WatertightGates.Assets.Mods.WatertightGates.Scripts.Extensions
{
	public static class IEntityLoaderExtensions
	{
		internal class PersistenceException : ApplicationException
		{
			public readonly struct PropertyKeyType
			{
				public Type Type { get; init; }
				public string Name { get; init; }

				public static PropertyKeyType FromKey<T>(PropertyKey<T> key) =>
					new PropertyKeyType { Type = typeof(T), Name = key.Name };
			}

			public PersistenceException(ComponentKey component, PropertyKeyType property,
				Exception? innerException = null) :
				base($"Failed to load {component.Name}::{property.Name}", innerException)
			{
				Component = component;
				Property = property;
			}

			public ComponentKey Component { get; }
			public PropertyKeyType Property { get; }
		}

		public static T GetRequired<T>(this IEntityLoader entityLoader, ComponentKey componentKey,
			PropertyKey<T> propertyKey) where T : Enum
		{
			if (!entityLoader.TryGetComponent(componentKey, out var objectLoader))
			{
				throw new PersistenceException(componentKey, PersistenceException.PropertyKeyType.FromKey(propertyKey));
			}

			try
			{
				return objectLoader.Get(propertyKey);
			}
			catch (Exception ex)
			{
				throw new PersistenceException(componentKey, PersistenceException.PropertyKeyType.FromKey(propertyKey),
					ex);
			}
		}

		public static T GetOrDefault<T>(this IEntityLoader entityLoader, ComponentKey componentKey,
			PropertyKey<T> propertyKey, Func<T> defaultValueFactory) where T : Enum
		{
			if (!entityLoader.TryGetComponent(componentKey, out var objectLoader))
			{
				return defaultValueFactory();
			}

			return objectLoader.Has(propertyKey) ? objectLoader.Get(propertyKey) : defaultValueFactory();
		}

		public static T GetOrDefault<T>(this IEntityLoader entityLoader, ComponentKey componentKey,
			PropertyKey<T> propertyKey, T defaultValue) where T : Enum
			=> entityLoader.GetOrDefault(componentKey, propertyKey, () => defaultValue);

		public static string GetOrDefault(this IEntityLoader entityLoader, ComponentKey componentKey,
			PropertyKey<string> propertyKey, Func<string> defaultValueFactory)
		{
			if (!entityLoader.TryGetComponent(componentKey, out var objectLoader))
			{
				return defaultValueFactory();
			}

			return objectLoader.Has(propertyKey) ? objectLoader.Get(propertyKey) : defaultValueFactory();
		}

		public static string GetOrDefault(this IEntityLoader entityLoader, ComponentKey componentKey,
			PropertyKey<string> propertyKey, string defaultValue)
			=> entityLoader.GetOrDefault(componentKey, propertyKey, () => defaultValue);

		public static string GetOrDefaultAsString<T>(this IEntityLoader entityLoader, ComponentKey componentKey,
			PropertyKey<T> propertyKey, Func<string> defaultValueFactory)
		{
			if (!entityLoader.TryGetComponent(componentKey, out var objectLoader))
			{
				return defaultValueFactory();
			}

			var newPropertyKey = new PropertyKey<string>(propertyKey.Name);
			return objectLoader.Has(propertyKey) ? objectLoader.Get(newPropertyKey) : defaultValueFactory();
		}

		public static string GetOrDefaultAsString<T>(this IEntityLoader entityLoader, ComponentKey componentKey,
			PropertyKey<T> propertyKey, string defaultValue)
			=> entityLoader.GetOrDefaultAsString(componentKey, propertyKey, () => defaultValue);

		public static string? GetAsString<T>(this IEntityLoader entityLoader, ComponentKey componentKey,
			PropertyKey<T> propertyKey)
		{
			if (!entityLoader.TryGetComponent(componentKey, out var objectLoader))
			{
				return null;
			}

			var newPropertyKey = new PropertyKey<string>(propertyKey.Name);
			return objectLoader.Has(propertyKey) ? objectLoader.Get(newPropertyKey) : null;
		}
	}
}