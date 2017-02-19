using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	internal class ReceiveContext
	{
		public ReceiveContext InnerContext
		{
			get;
			protected set;
		}

		public Guid LockToken
		{
			get;
			protected set;
		}

		public Microsoft.ServiceBus.Messaging.MessageReceiver MessageReceiver
		{
			get;
			protected set;
		}

		public TimeSpan OperationTimeout
		{
			get;
			protected set;
		}

		public ReceiveContext(ReceiveContext innerContext)
		{
			if (innerContext == null)
			{
				throw FxTrace.Exception.ArgumentNull("innerContext");
			}
			this.InnerContext = innerContext;
			this.LockToken = innerContext.LockToken;
			this.OperationTimeout = innerContext.OperationTimeout;
		}

		public ReceiveContext(Microsoft.ServiceBus.Messaging.MessageReceiver messageReceiver, Guid lockToken)
		{
			if (messageReceiver == null)
			{
				throw FxTrace.Exception.ArgumentNull("messageReceiver");
			}
			this.MessageReceiver = messageReceiver;
			this.LockToken = lockToken;
			this.OperationTimeout = messageReceiver.OperationTimeout;
		}

		internal ReceiveContext(Guid lockToken)
		{
			this.MessageReceiver = null;
			this.LockToken = lockToken;
			this.OperationTimeout = Constants.DefaultOperationTimeout;
		}

		public IAsyncResult BeginAbandon(AsyncCallback callback, object state)
		{
			return this.BeginAbandon(null, this.OperationTimeout, callback, state);
		}

		public virtual IAsyncResult BeginAbandon(IDictionary<string, object> propertiesToModify, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.MessageReceiver == null)
			{
				return this.InnerContext.BeginAbandon(propertiesToModify, timeout, callback, state);
			}
			Microsoft.ServiceBus.Messaging.MessageReceiver messageReceiver = this.MessageReceiver;
			Guid[] lockToken = new Guid[] { this.LockToken };
			return messageReceiver.BeginAbandon(lockToken, propertiesToModify, timeout, callback, state);
		}

		public virtual IAsyncResult BeginComplete(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.MessageReceiver == null)
			{
				return this.InnerContext.BeginComplete(timeout, callback, state);
			}
			Microsoft.ServiceBus.Messaging.MessageReceiver messageReceiver = this.MessageReceiver;
			Guid[] lockToken = new Guid[] { this.LockToken };
			return messageReceiver.BeginComplete(lockToken, timeout, callback, state);
		}

		public IAsyncResult BeginDeadLetter(string deadLetterReason, string deadLetterErrorDescription, AsyncCallback callback, object state)
		{
			return this.BeginDeadLetter(null, deadLetterReason, deadLetterErrorDescription, this.OperationTimeout, callback, state);
		}

		public virtual IAsyncResult BeginDeadLetter(IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.MessageReceiver == null)
			{
				return this.InnerContext.BeginDeadLetter(propertiesToModify, deadLetterReason, deadLetterErrorDescription, timeout, callback, state);
			}
			Microsoft.ServiceBus.Messaging.MessageReceiver messageReceiver = this.MessageReceiver;
			Guid[] lockToken = new Guid[] { this.LockToken };
			return messageReceiver.BeginDeadLetter(lockToken, propertiesToModify, deadLetterReason, deadLetterErrorDescription, timeout, callback, state);
		}

		public IAsyncResult BeginDefer(AsyncCallback callback, object state)
		{
			return this.BeginDefer(null, this.OperationTimeout, callback, state);
		}

		public virtual IAsyncResult BeginDefer(IDictionary<string, object> propertiesToModify, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.MessageReceiver == null)
			{
				return this.InnerContext.BeginDefer(propertiesToModify, timeout, callback, state);
			}
			Microsoft.ServiceBus.Messaging.MessageReceiver messageReceiver = this.MessageReceiver;
			Guid[] lockToken = new Guid[] { this.LockToken };
			return messageReceiver.BeginDefer(lockToken, propertiesToModify, timeout, callback, state);
		}

		public virtual IAsyncResult BeginRenewLock(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (this.MessageReceiver == null)
			{
				return this.InnerContext.BeginRenewLock(timeout, callback, state);
			}
			Microsoft.ServiceBus.Messaging.MessageReceiver messageReceiver = this.MessageReceiver;
			Guid[] lockToken = new Guid[] { this.LockToken };
			return messageReceiver.BeginRenewMessageLocks(null, lockToken, timeout, callback, state);
		}

		public virtual void Complete()
		{
			if (this.MessageReceiver == null)
			{
				this.InnerContext.Complete();
				return;
			}
			Microsoft.ServiceBus.Messaging.MessageReceiver messageReceiver = this.MessageReceiver;
			Guid[] lockToken = new Guid[] { this.LockToken };
			messageReceiver.Complete(lockToken, this.OperationTimeout);
		}

		public virtual void EndAbandon(IAsyncResult result)
		{
			if (this.MessageReceiver != null)
			{
				this.MessageReceiver.EndAbandon(result);
				return;
			}
			this.InnerContext.EndAbandon(result);
		}

		public virtual void EndComplete(IAsyncResult result)
		{
			if (this.MessageReceiver != null)
			{
				this.MessageReceiver.EndComplete(result);
				return;
			}
			this.InnerContext.EndComplete(result);
		}

		public virtual void EndDeadLetter(IAsyncResult result)
		{
			if (this.MessageReceiver != null)
			{
				this.MessageReceiver.EndDeadLetter(result);
				return;
			}
			this.InnerContext.EndDeadLetter(result);
		}

		public virtual void EndDefer(IAsyncResult result)
		{
			if (this.MessageReceiver != null)
			{
				this.MessageReceiver.EndDefer(result);
				return;
			}
			this.InnerContext.EndDefer(result);
		}

		public virtual IEnumerable<DateTime> EndRenewLock(IAsyncResult result)
		{
			if (this.MessageReceiver != null)
			{
				return this.MessageReceiver.EndRenewMessageLocks(result);
			}
			return this.InnerContext.EndRenewLock(result);
		}
	}
}