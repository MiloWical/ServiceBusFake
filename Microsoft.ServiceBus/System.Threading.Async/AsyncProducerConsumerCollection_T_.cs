using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Threading.Async
{
	[DebuggerDisplay("Count={CurrentCount}")]
	internal sealed class AsyncProducerConsumerCollection<T> : IDisposable
	{
		private AsyncSemaphore _semaphore;

		private IProducerConsumerCollection<T> _collection;

		public int Count
		{
			get
			{
				return this._collection.Count;
			}
		}

		public AsyncProducerConsumerCollection() : this(new ConcurrentQueue<T>())
		{
		}

		public AsyncProducerConsumerCollection(IProducerConsumerCollection<T> collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			this._collection = collection;
		}

		public void Add(T item)
		{
			if (!this._collection.TryAdd(item))
			{
				throw new InvalidOperationException("Invalid collection");
			}
			this._semaphore.Release();
		}

		public void CancelAllExisting()
		{
			this._semaphore.CancelAllExisting();
		}

		public void Dispose()
		{
			if (this._semaphore != null)
			{
				this._semaphore.Dispose();
				this._semaphore = null;
			}
		}

		public Task<T> Take()
		{
			return this._semaphore.WaitAsync().ContinueWith<T>((Task _) => {
				T t;
				if (!this._collection.TryTake(out t))
				{
					throw new InvalidOperationException("Invalid collection");
				}
				return t;
			}, TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
		}
	}
}