using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Transactions;

namespace Microsoft.ServiceBus.Common
{
	internal sealed class ConcurrentTransactionalDictionary<TKey, TValue>
	{
		private readonly IDictionary<TKey, TValue> internalDictionary;

		private readonly IDictionary<TKey, ConcurrentTransactionalDictionary<TKey, TValue>.TransactionInformation> pendingTransactions;

		private readonly object syncLock;

		public ConcurrentTransactionalDictionary() : this(null)
		{
		}

		public ConcurrentTransactionalDictionary(IDictionary<TKey, TValue> originalDictionary)
		{
			this.internalDictionary = (originalDictionary == null ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(originalDictionary));
			this.pendingTransactions = new Dictionary<TKey, ConcurrentTransactionalDictionary<TKey, TValue>.TransactionInformation>();
			this.syncLock = new object();
		}

		public void AddOrUpdateValue(TKey key, TValue value)
		{
			this.AddUpdateOrRemoveCore(key, value, false, true);
		}

		private void AddUpdateOrRemoveCore(TKey key, TValue value, bool delete, bool throwNotFound = true)
		{
			ConcurrentTransactionalDictionary<TKey, TValue>.TransactionInformation transactionInformation;
			TValue tValue;
			string localIdentifier;
			Transaction current = Transaction.Current;
			if (current != null)
			{
				localIdentifier = current.TransactionInformation.LocalIdentifier;
			}
			else
			{
				localIdentifier = null;
			}
			string str = localIdentifier;
			lock (this.syncLock)
			{
				bool flag = !this.internalDictionary.TryGetValue(key, out tValue);
				if (!delete || !flag)
				{
					if (this.pendingTransactions.TryGetValue(key, out transactionInformation))
					{
						if (!string.Equals(transactionInformation.TransactionId, str, StringComparison.OrdinalIgnoreCase))
						{
							throw new InvalidOperationException(SRCore.DictionaryKeyIsModified(key));
						}
					}
					else if (str != null)
					{
						this.pendingTransactions[key] = new ConcurrentTransactionalDictionary<TKey, TValue>.TransactionInformation(current, key, tValue, flag, this);
					}
					if (!delete)
					{
						this.internalDictionary[key] = value;
					}
					else
					{
						this.internalDictionary.Remove(key);
					}
				}
				else if (throwNotFound)
				{
					throw new InvalidOperationException(SRCore.DictionaryKeyNotExist(key));
				}
			}
		}

		public IAsyncResult BeginTryGetValue(TKey key, TimeSpan timeout, AsyncCallback callback, object state)
		{
			ConcurrentTransactionalDictionary<TKey, TValue>.TransactionInformation transactionInformation;
			TValue tValue;
			IAsyncResult asyncResult;
			string localIdentifier;
			Transaction current = Transaction.Current;
			if (current != null)
			{
				localIdentifier = current.TransactionInformation.LocalIdentifier;
			}
			else
			{
				localIdentifier = null;
			}
			string str = localIdentifier;
			lock (this.syncLock)
			{
				if (!this.pendingTransactions.TryGetValue(key, out transactionInformation) || string.Equals(transactionInformation.TransactionId, str, StringComparison.OrdinalIgnoreCase))
				{
					bool flag = this.internalDictionary.TryGetValue(key, out tValue);
					return new CompletedAsyncResult<bool, TValue>(flag, tValue, callback, state);
				}
				else
				{
					ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter transactionalAsyncWaiter = new ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter(callback, state);
					transactionInformation.Waiters.Enqueue(transactionalAsyncWaiter);
					transactionalAsyncWaiter.StartTimer(timeout);
					asyncResult = transactionalAsyncWaiter;
				}
			}
			return asyncResult;
		}

		public void Clear()
		{
			lock (this.syncLock)
			{
				foreach (ConcurrentTransactionalDictionary<TKey, TValue>.TransactionInformation value in this.pendingTransactions.Values)
				{
					if (!value.HasWaiters)
					{
						continue;
					}
					while (value.Waiters.Count > 0)
					{
						ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter transactionalAsyncWaiter = value.Waiters.Dequeue();
						ActionItem.Schedule((object w) => ((ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter)w).Cancel(), transactionalAsyncWaiter);
					}
				}
				this.internalDictionary.Clear();
				this.pendingTransactions.Clear();
			}
		}

		public bool EndTryGetValue(IAsyncResult result, out TValue value)
		{
			if (result is CompletedAsyncResult<bool, TValue>)
			{
				return CompletedAsyncResult<bool, TValue>.End(result, out value);
			}
			Tuple<bool, TValue> tuple = ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter.End(result);
			value = tuple.Item2;
			return tuple.Item1;
		}

		public IDictionary<TKey, TValue> GetSnapshot()
		{
			ConcurrentTransactionalDictionary<TKey, TValue>.TransactionInformation transactionInformation;
			IDictionary<TKey, TValue> tKeys = new Dictionary<TKey, TValue>();
			lock (this.syncLock)
			{
				foreach (KeyValuePair<TKey, TValue> keyValuePair in this.internalDictionary)
				{
					if (this.pendingTransactions.TryGetValue(keyValuePair.Key, out transactionInformation))
					{
						if (transactionInformation.NewEntry)
						{
							continue;
						}
						tKeys[keyValuePair.Key] = transactionInformation.PreviousValue;
					}
					else
					{
						tKeys[keyValuePair.Key] = keyValuePair.Value;
					}
				}
			}
			return tKeys;
		}

		public void RemoveKey(TKey key)
		{
			this.AddUpdateOrRemoveCore(key, default(TValue), true, true);
		}

		public void TryRemoveKey(TKey key)
		{
			this.AddUpdateOrRemoveCore(key, default(TValue), true, false);
		}

		private sealed class TransactionalAsyncWaiter : AsyncResult
		{
			private readonly static AsyncCallback onWaiterSignaled;

			private readonly AsyncWaiter waiter;

			private bool keyFound;

			private TValue @value;

			static TransactionalAsyncWaiter()
			{
				ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter.onWaiterSignaled = new AsyncCallback(ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter.OnWaiterSignaled);
			}

			public TransactionalAsyncWaiter(AsyncCallback callback, object state) : base(callback, state)
			{
				this.waiter = new AsyncWaiter(TimeSpan.MaxValue, ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter.onWaiterSignaled, this, false);
			}

			public void Cancel()
			{
				this.waiter.Cancel();
			}

			public static new Tuple<bool, TValue> End(IAsyncResult result)
			{
				ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter transactionalAsyncWaiter = AsyncResult.End<ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter>(result);
				return new Tuple<bool, TValue>(transactionalAsyncWaiter.keyFound, transactionalAsyncWaiter.@value);
			}

			private static void OnWaiterSignaled(IAsyncResult result)
			{
				ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter asyncState = (ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter)result.AsyncState;
				Exception timeoutException = null;
				try
				{
					if (!AsyncWaiter.End(result))
					{
						timeoutException = new TimeoutException();
					}
				}
				catch (OperationCanceledException operationCanceledException)
				{
					timeoutException = operationCanceledException;
				}
				asyncState.Complete(false, timeoutException);
			}

			public void Signal(bool keyFound, TValue value)
			{
				this.keyFound = keyFound;
				this.@value = value;
				this.waiter.Signal();
			}

			public void StartTimer(TimeSpan timeout)
			{
				this.waiter.StartTimer(timeout);
			}
		}

		private sealed class TransactionInformation
		{
			private readonly TKey key;

			private readonly ConcurrentTransactionalDictionary<TKey, TValue> owner;

			private readonly Transaction transaction;

			private Queue<ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter> waiters;

			public bool HasWaiters
			{
				get
				{
					return this.waiters != null;
				}
			}

			public bool NewEntry
			{
				get;
				private set;
			}

			public TValue PreviousValue
			{
				get;
				private set;
			}

			public string TransactionId
			{
				get
				{
					return this.transaction.TransactionInformation.LocalIdentifier;
				}
			}

			public Queue<ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter> Waiters
			{
				get
				{
					if (this.waiters == null)
					{
						this.waiters = new Queue<ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter>();
					}
					return this.waiters;
				}
			}

			public TransactionInformation(Transaction transaction, TKey key, TValue previousValue, bool newEntry, ConcurrentTransactionalDictionary<TKey, TValue> owner)
			{
				this.key = key;
				this.NewEntry = newEntry;
				this.owner = owner;
				this.PreviousValue = previousValue;
				this.transaction = transaction;
				this.transaction.TransactionCompleted += Fx.ThunkTransactionEventHandler((object s, TransactionEventArgs e) => {
					bool status = e.Transaction.TransactionInformation.Status == TransactionStatus.Committed;
					bool flag = false;
					TValue tValue = default(TValue);
					lock (this.owner.syncLock)
					{
						this.owner.pendingTransactions.Remove(this.key);
						if (!status)
						{
							if (!this.NewEntry)
							{
								this.owner.internalDictionary[this.key] = this.PreviousValue;
							}
							else
							{
								this.owner.internalDictionary.Remove(this.key);
							}
						}
						if (this.waiters != null)
						{
							flag = this.owner.internalDictionary.TryGetValue(this.key, out tValue);
						}
					}
					if (this.waiters != null)
					{
						while (this.waiters.Count > 0)
						{
							ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter transactionalAsyncWaiter = this.waiters.Dequeue();
							ActionItem.Schedule((object w) => ((ConcurrentTransactionalDictionary<TKey, TValue>.TransactionalAsyncWaiter)w).Signal(flag, tValue), transactionalAsyncWaiter);
						}
					}
				});
			}
		}
	}
}