using System;
using System.Collections.Generic;
using Timberborn.Persistence;
using Timberborn.SerializationSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace GerkinDev.Tests.Utils
{
	public class MockEntityLoader : IEntityLoader
	{
		private readonly Dictionary<string, Dictionary<string, object>> _initData;

		public MockEntityLoader(Dictionary<string, Dictionary<string, object>> initData)
		{
			_initData = initData;
		}

		public IObjectLoader GetComponent(ComponentKey key) => throw new NotImplementedException();

		public IObjectLoader GetComponent(ComponentKey key, string suffix) =>
			throw new NotImplementedException();

		public bool TryGetComponent(ComponentKey key, out IObjectLoader objectLoader)
		{
			if (_initData.TryGetValue(key.Name, out Dictionary<string, object>? initData))
			{
				objectLoader = new MockObjectLoader(initData);
				return true;
			}

			objectLoader = null;
			return false;
		}

		public bool TryGetComponent(ComponentKey key, string suffix, out IObjectLoader objectLoader) =>
			throw new NotImplementedException();

		private class MockObjectLoader : IObjectLoader
		{
			private readonly Dictionary<string, object> _initData;

			public MockObjectLoader(Dictionary<string, object> initData)
			{
				_initData = initData;
			}

			public int Get(PropertyKey<int> key) => throw new NotImplementedException();
			public float Get(PropertyKey<float> key) => throw new NotImplementedException();
			public bool Get(PropertyKey<bool> key) => throw new NotImplementedException();
			public string Get(PropertyKey<string> key) => _GetAsString(key.Name);
			public char Get(PropertyKey<char> key) => throw new NotImplementedException();
			public Quaternion Get(PropertyKey<Quaternion> key) => throw new NotImplementedException();
			public Vector3 Get(PropertyKey<Vector3> key) => throw new NotImplementedException();
			public Vector3Int Get(PropertyKey<Vector3Int> key) => throw new NotImplementedException();
			public Vector2 Get(PropertyKey<Vector2> key) => throw new NotImplementedException();
			public Vector2Int Get(PropertyKey<Vector2Int> key) => throw new NotImplementedException();
			public Color Get(PropertyKey<Color> key) => throw new NotImplementedException();
			public Guid Get(PropertyKey<Guid> key) => throw new NotImplementedException();

			public T Get<T>(PropertyKey<T> key) where T : Enum =>
				(T)PrimitiveTypeSerialization.Deserialize(_GetAsString(key.Name), typeof(T));

			public T Get<T>(PropertyKey<T> key, IValueSerializer<T> serializer) => throw new NotImplementedException();

			public bool GetObsoletable<T>(PropertyKey<T> key, IValueSerializer<T> serializer, out T value) =>
				throw new NotImplementedException();

			public List<int> Get(ListKey<int> key) => throw new NotImplementedException();
			public List<float> Get(ListKey<float> key) => throw new NotImplementedException();
			public List<bool> Get(ListKey<bool> key) => throw new NotImplementedException();
			public List<string> Get(ListKey<string> key) => throw new NotImplementedException();
			public List<char> Get(ListKey<char> key) => throw new NotImplementedException();
			public List<Quaternion> Get(ListKey<Quaternion> key) => throw new NotImplementedException();
			public List<Vector3> Get(ListKey<Vector3> key) => throw new NotImplementedException();
			public List<Vector3Int> Get(ListKey<Vector3Int> key) => throw new NotImplementedException();
			public List<Vector2> Get(ListKey<Vector2> key) => throw new NotImplementedException();
			public List<Vector2Int> Get(ListKey<Vector2Int> key) => throw new NotImplementedException();
			public List<Color> Get(ListKey<Color> key) => throw new NotImplementedException();
			public List<Guid> Get(ListKey<Guid> key) => throw new NotImplementedException();
			public List<T> Get<T>(ListKey<T> key) where T : Enum => throw new NotImplementedException();

			public List<T> Get<T>(ListKey<T> key, IValueSerializer<T> serializer) =>
				throw new NotImplementedException();

			public bool Has<T>(PropertyKey<T> key) => _initData.ContainsKey(key.Name);
			public bool Has<T>(ListKey<T> key) => throw new NotImplementedException();

			private string _GetAsString(string key) => _initData[key].ToString();
		}
	}
}