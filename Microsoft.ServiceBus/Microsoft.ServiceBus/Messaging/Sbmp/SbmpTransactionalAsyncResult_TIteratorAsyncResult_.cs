using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal abstract class SbmpTransactionalAsyncResult<TIteratorAsyncResult> : IteratorAsyncResult<TIteratorAsyncResult>
	where TIteratorAsyncResult : SbmpTransactionalAsyncResult<TIteratorAsyncResult>
	{
		private readonly SbmpMessagingFactory messagingFactory;

		private readonly IRequestSessionChannel channel;

		private readonly Lazy<SbmpMessageCreator> controlMessageCreator;

		private Message wcfMessage;

		private int txnSeqNumber;

		public SbmpMessageCreator MessageCreator
		{
			get;
			private set;
		}

		public Message Response
		{
			get;
			private set;
		}

		public System.Transactions.Transaction Transaction
		{
			get;
			private set;
		}

		protected SbmpTransactionalAsyncResult(SbmpMessagingFactory messagingFactory, SbmpMessageCreator messageCreator, Lazy<SbmpMessageCreator> controlMessageCreator, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
		{
			this.messagingFactory = messagingFactory;
			this.MessageCreator = messageCreator;
			this.channel = this.messagingFactory.Channel;
			this.controlMessageCreator = controlMessageCreator;
			this.Transaction = System.Transactions.Transaction.Current;
		}

		protected abstract Message CreateWcfMessage();

		protected override IEnumerator<IteratorAsyncResult<TIteratorAsyncResult>.AsyncStep> GetAsyncSteps()
		{
			if (this.Transaction == null)
			{
				this.wcfMessage = this.CreateWcfMessage();
			}
			else
			{
				SbmpMessageCreator sbmpMessageCreator1 = (this.controlMessageCreator == null ? this.MessageCreator : this.controlMessageCreator.Value);
				SbmpTransactionalAsyncResult<TIteratorAsyncResult> sbmpTransactionalAsyncResult = this;
				IteratorAsyncResult<TIteratorAsyncResult>.BeginCall beginCall = (TIteratorAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
					SbmpResourceManager resourceManager = thisPtr.messagingFactory.ResourceManager;
					System.Transactions.Transaction transaction = thisPtr.Transaction;
					IRequestSessionChannel requestSessionChannel = thisPtr.channel;
					SbmpMessageCreator sbmpMessageCreator = sbmpMessageCreator1;
					object obj = thisPtr;
					return resourceManager.BeginEnlist(transaction, requestSessionChannel, sbmpMessageCreator, new Action<RequestInfo>(obj.PartitionInfoSetter), t, c, s);
				};
				yield return sbmpTransactionalAsyncResult.CallAsync(beginCall, (TIteratorAsyncResult thisPtr, IAsyncResult a) => thisPtr.txnSeqNumber = thisPtr.messagingFactory.ResourceManager.EndEnlist(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				this.wcfMessage = this.CreateWcfMessage();
				WorkUnitInfo.AddTo(this.wcfMessage.Headers, "TxnWorkUnit", this.Transaction.TransactionInformation.LocalIdentifier, this.txnSeqNumber);
			}
			SbmpTransactionalAsyncResult<TIteratorAsyncResult> sbmpTransactionalAsyncResult1 = this;
			IteratorAsyncResult<TIteratorAsyncResult>.BeginCall beginCall1 = (TIteratorAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.channel.BeginRequest(thisPtr.wcfMessage, SbmpProtocolDefaults.BufferTimeout(t, this.messagingFactory.GetSettings().EnableAdditionalClientTimeout), c, s);
			yield return sbmpTransactionalAsyncResult1.CallAsync(beginCall1, (TIteratorAsyncResult thisPtr, IAsyncResult a) => thisPtr.Response = thisPtr.channel.EndRequest(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
		}

		protected abstract void PartitionInfoSetter(RequestInfo requestInfo);
	}
}