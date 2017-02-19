using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class WorkCollection<TKey, TWork, TOutcome>
	where TWork : class, IWork<TOutcome>
	{
		private readonly ConcurrentDictionary<TKey, TWork> pendingWork;

		private volatile bool closed;

		public WorkCollection() : this(null)
		{
		}

		public WorkCollection(IEqualityComparer<TKey> comparer)
		{
			if (comparer == null)
			{
				this.pendingWork = new ConcurrentDictionary<TKey, TWork>();
				return;
			}
			this.pendingWork = new ConcurrentDictionary<TKey, TWork>(comparer);
		}

		public void Abort()
		{
			this.closed = true;
			ActionItem.Schedule((object o) => {
				TWork tWork;
				WorkCollection<TKey, TWork, TOutcome> workCollection = (WorkCollection<TKey, TWork, TOutcome>)o;
				foreach (TKey tKey in new List<TKey>(workCollection.pendingWork.Keys))
				{
					if (!workCollection.pendingWork.TryRemove(tKey, out tWork))
					{
						continue;
					}
					tWork.Cancel(false, new OperationCanceledException());
				}
			}, this);
		}

		public void CompleteWork(TKey key, bool syncComplete, TOutcome outcome)
		{
			TWork tWork;
			if (this.pendingWork.TryRemove(key, out tWork))
			{
				if (syncComplete)
				{
					tWork.Done(true, outcome);
					return;
				}
				ActionItem.Schedule((object o) => {
					Tuple<TWork, TOutcome> tuple = (Tuple<TWork, TOutcome>)o;
					tuple.Item1.Done(false, tuple.Item2);
				}, new Tuple<TWork, TOutcome>(tWork, outcome));
			}
		}

		public void StartWork(TKey key, TWork work)
		{
			if (!this.pendingWork.TryAdd(key, work))
			{
				throw new InvalidOperationException();
			}
			if (this.closed && this.pendingWork.TryRemove(key, out work))
			{
				work.Cancel(true, new OperationCanceledException());
				return;
			}
			try
			{
				work.Start();
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				if (this.pendingWork.TryRemove(key, out work))
				{
					work.Cancel(true, exception);
				}
			}
		}

		public bool TryRemoveWork(TKey key, out TWork work)
		{
			return this.pendingWork.TryRemove(key, out work);
		}
	}
}