using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus
{
	internal class NullableArray<T>
	{
		public static NullableArray<T> NullArray;

		public T[] Value
		{
			get;
			private set;
		}

		static NullableArray()
		{
			NullableArray<T>.NullArray = new NullableArray<T>(null);
		}

		public NullableArray(T[] array)
		{
			this.Value = array;
		}
	}
}