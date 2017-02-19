using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Transactions;

namespace Microsoft.ServiceBus.Common
{
	internal sealed class TransactionResultManager
	{
		private readonly static TransactionResultManager instance;

		private readonly object syncLock;

		private readonly Dictionary<string, TransactionResult> transactionResults;

		public static TransactionResultManager Instance
		{
			get
			{
				return TransactionResultManager.instance;
			}
		}

		static TransactionResultManager()
		{
			TransactionResultManager.instance = new TransactionResultManager();
		}

		private TransactionResultManager()
		{
			this.syncLock = new object();
			this.transactionResults = new Dictionary<string, TransactionResult>();
		}

		public T FindTransactionResultExtension<T>(string transactionId)
		{
			TransactionResult transactionResult;
			T t;
			lock (this.syncLock)
			{
				if (!this.transactionResults.TryGetValue(transactionId, out transactionResult))
				{
					throw Fx.AssertAndFailFastService(SRClient.CannotFindTransactionResult(transactionId));
				}
				t = transactionResult.Extensions.Find<T>();
			}
			return t;
		}

		public void RegisterForTransactionalResult(Action<object, TransactionEventArgs, TransactionResult> onTransactionCompleted)
		{
			TransactionResult transactionResult1;
			if (onTransactionCompleted != null)
			{
				Transaction current = Transaction.Current;
				if (current != null)
				{
					string localIdentifier = current.TransactionInformation.LocalIdentifier;
					lock (this.syncLock)
					{
						if (!this.transactionResults.TryGetValue(localIdentifier, out transactionResult1))
						{
							transactionResult1 = new TransactionResult();
							this.transactionResults.Add(localIdentifier, transactionResult1);
						}
						TransactionResult transactionResult2 = transactionResult1;
						transactionResult2.ReferenceCount = transactionResult2.ReferenceCount + 1;
					}
					current.TransactionCompleted += Fx.ThunkTransactionEventHandler((object s, TransactionEventArgs e) => {
						TransactionResult item;
						lock (this.syncLock)
						{
							item = this.transactionResults[localIdentifier];
							TransactionResult transactionResult = item;
							int referenceCount = transactionResult.ReferenceCount - 1;
							int num = referenceCount;
							transactionResult.ReferenceCount = referenceCount;
							if (num == 0)
							{
								this.transactionResults.Remove(localIdentifier);
							}
						}
						onTransactionCompleted(s, e, item);
					});
				}
			}
		}

		public void SetTransactionResult(string transactionId, Exception completionException, TrackingContext trackingContext)
		{
			TransactionResult transactionResult;
			lock (this.syncLock)
			{
				if (this.transactionResults.TryGetValue(transactionId, out transactionResult))
				{
					transactionResult.CompletionException = completionException;
					transactionResult.TrackingContext = trackingContext;
				}
			}
		}

		public void SetTransactionResultExtension(string transactionId, IExtension<TransactionResult> extensionData)
		{
			TransactionResult transactionResult;
			lock (this.syncLock)
			{
				if (this.transactionResults.TryGetValue(transactionId, out transactionResult))
				{
					transactionResult.Extensions.Add(extensionData);
				}
			}
		}
	}
}