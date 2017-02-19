using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal class CloseOrAbortLinkAsyncResult : IteratorAsyncResult<CloseOrAbortLinkAsyncResult>
	{
		private readonly SbmpMessageCreator linkMessageCreator;

		private readonly IRequestSessionChannel channel;

		private readonly ICommunicationObject batchManager;

		private readonly bool aborting;

		private readonly string parentLinkId;

		private Message closeMessage;

		public CloseOrAbortLinkAsyncResult(SbmpMessageCreator linkMessageCreator, IRequestSessionChannel channel, ICommunicationObject batchManager, TimeSpan closeTimeout, bool aborting, AsyncCallback callback, object state) : this(linkMessageCreator, channel, batchManager, string.Empty, closeTimeout, aborting, callback, state)
		{
		}

		public CloseOrAbortLinkAsyncResult(SbmpMessageCreator linkMessageCreator, IRequestSessionChannel channel, ICommunicationObject batchManager, string parentLinkId, TimeSpan closeTimeout, bool aborting, AsyncCallback callback, object state) : base(closeTimeout, callback, state)
		{
			this.linkMessageCreator = linkMessageCreator;
			this.channel = channel;
			this.batchManager = batchManager;
			this.aborting = aborting;
			this.parentLinkId = parentLinkId;
		}

		protected override IEnumerator<IteratorAsyncResult<CloseOrAbortLinkAsyncResult>.AsyncStep> GetAsyncSteps()
		{
			Exception exception;
			if (this.batchManager != null)
			{
				if (!this.aborting)
				{
					CloseOrAbortLinkAsyncResult closeOrAbortLinkAsyncResult = this;
					IteratorAsyncResult<CloseOrAbortLinkAsyncResult>.BeginCall beginCall = (CloseOrAbortLinkAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.batchManager.BeginClose(t, c, s);
					IteratorAsyncResult<CloseOrAbortLinkAsyncResult>.EndCall endCall = (CloseOrAbortLinkAsyncResult thisPtr, IAsyncResult a) => thisPtr.batchManager.EndClose(a);
					yield return closeOrAbortLinkAsyncResult.CallAsync(beginCall, endCall, (CloseOrAbortLinkAsyncResult thisPtr, TimeSpan t) => thisPtr.batchManager.Close(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				else
				{
					this.batchManager.Abort();
				}
			}
			try
			{
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(SbmpProtocolDefaults.BufferTimeout(base.RemainingTime(), true))
				};
				RequestInfo requestInfo1 = requestInfo;
				this.closeMessage = this.linkMessageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/CloseLink", new CloseLinkCommand(), this.parentLinkId, null, null, requestInfo1);
			}
			catch (Exception exception2)
			{
				Exception exception1 = exception2;
				if (Fx.IsFatal(exception1))
				{
					throw;
				}
				CloseOrAbortLinkAsyncResult closeOrAbortLinkAsyncResult1 = this;
				if (this.aborting)
				{
					exception = null;
				}
				else
				{
					exception = exception1;
				}
				closeOrAbortLinkAsyncResult1.Complete(exception);
				goto Label0;
			}
			IteratorAsyncResult<CloseOrAbortLinkAsyncResult>.ExceptionPolicy exceptionPolicy = (this.aborting ? IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue : IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			CloseOrAbortLinkAsyncResult closeOrAbortLinkAsyncResult2 = this;
			IteratorAsyncResult<CloseOrAbortLinkAsyncResult>.BeginCall beginCall1 = (CloseOrAbortLinkAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.channel.BeginRequest(thisPtr.closeMessage, SbmpProtocolDefaults.BufferTimeout(t, true), c, s);
			yield return closeOrAbortLinkAsyncResult2.CallAsync(beginCall1, (CloseOrAbortLinkAsyncResult thisPtr, IAsyncResult a) => thisPtr.channel.EndRequest(a), (IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy)exceptionPolicy);
		Label0:
			yield break;
		}

		public void Schedule()
		{
			ActionItem.Schedule((object s) => ((CloseOrAbortLinkAsyncResult)s).Start(), this);
		}
	}
}