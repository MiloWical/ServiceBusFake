using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Threading.Async
{
	internal sealed class AsyncCache<TKey, TValue>
	{
		private readonly Func<TKey, Task<TValue>> valueFactory;

		private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> map;

		public Task<TValue> this[TKey key]
		{
			get
			{
				return this.map.GetOrAdd(key, (TKey toAdd) => new Lazy<Task<TValue>>(() => this.valueFactory(toAdd))).Value;
			}
		}

		public AsyncCache(Func<TKey, Task<TValue>> valueFactory)
		{
			this.valueFactory = valueFactory;
			this.map = new ConcurrentDictionary<TKey, Lazy<Task<TValue>>>();
		}

		public bool TryRemoveKey(TKey key)
		{
			Lazy<Task<TValue>> lazy;
			return this.map.TryRemove(key, out lazy);
		}
	}
}