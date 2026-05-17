using System.Collections.Generic;
using Timberborn.Localization;

namespace GerkinDev.Tests.Utils
{
	public class MockLoc: ILoc
	{
		public void Initialize(Dictionary<string, string> localization) => throw new System.NotImplementedException();

		public IEnumerable<string> GetRawTexts() => throw new System.NotImplementedException();

		public string T(string key) => key;

		public string T<T1>(string key, T1 param1) => string.Format(key, param1);

		public string T<T1, T2>(string key, T1 param1, T2 param2) => string.Format(key, param1, param2);

		public string T<T1, T2, T3>(string key, T1 param1, T2 param2, T3 param3) => throw new System.NotImplementedException();

		public string T(Phrase phrase) => throw new System.NotImplementedException();

		public string T<T1>(Phrase phrase, T1 param1) => throw new System.NotImplementedException();

		public string T<T1, T2>(Phrase phrase, T1 param1, T2 param2) => throw new System.NotImplementedException();

		public string T<T1, T2, T3>(Phrase phrase, T1 param1, T2 param2, T3 param3) => throw new System.NotImplementedException();
	}
}