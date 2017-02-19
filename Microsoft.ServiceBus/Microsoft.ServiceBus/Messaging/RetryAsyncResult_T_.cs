using System;
using System.Runtime.CompilerServices;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging
{
	internal abstract class RetryAsyncResult<T> : IteratorAsyncResult<T>
	where T : RetryAsyncResult<T>
	{
		protected Transaction AmbientTransaction
		{
			get;
			private set;
		}

		protected bool TransactionExists
		{
			get;
			private set;
		}

		protected RetryAsyncResult(TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
		{
			this.AmbientTransaction = Transaction.Current;
			this.TransactionExists = Transaction.Current != null;
		}
	}
}