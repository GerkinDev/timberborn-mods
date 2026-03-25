using System.Collections.Generic;

namespace GerkinDev.WatertightGates.Utils
{
	internal struct CommittableState<T>
	{
		public T DesiredValue { get; set; }
		public T Value { get; private set; }
		public readonly bool HasChange => !EqualityComparer<T>.Default.Equals(Value, DesiredValue);

		public void Commit()
		{
			Value = DesiredValue;
		}
	}
}
