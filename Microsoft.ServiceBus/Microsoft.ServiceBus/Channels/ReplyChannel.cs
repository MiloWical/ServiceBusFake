using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Messaging.Channels;
using Microsoft.ServiceBus.Properties;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal class ReplyChannel : Microsoft.ServiceBus.Channels.InputQueueChannel<RequestContext>, IReplyChannel, IChannel, ICommunicationObject
	{
		private EndpointAddress localAddress;

		public EndpointAddress LocalAddress
		{
			get
			{
				return this.localAddress;
			}
		}

		public ReplyChannel(ChannelManagerBase channelManager, EndpointAddress localAddress) : base(channelManager)
		{
			this.localAddress = localAddress;
		}

		public IAsyncResult BeginReceiveRequest(AsyncCallback callback, object state)
		{
			return this.BeginReceiveRequest(base.DefaultReceiveTimeout, callback, state);
		}

		public IAsyncResult BeginReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			this.ThrowPending();
			return Microsoft.ServiceBus.Channels.ReplyChannel.HelpBeginReceiveRequest(this, timeout, callback, state);
		}

		public IAsyncResult BeginTryReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			this.ThrowPending();
			return base.BeginDequeue(timeout, callback, state);
		}

		public IAsyncResult BeginWaitForRequest(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			this.ThrowPending();
			return base.BeginWaitForItem(timeout, callback, state);
		}

		private static Exception CreateReceiveRequestTimedOutException(IReplyChannel channel, TimeSpan timeout)
		{
			if (channel.LocalAddress == null)
			{
				string receiveRequestTimedOutNoLocalAddress = Resources.ReceiveRequestTimedOutNoLocalAddress;
				object[] objArray = new object[] { timeout };
				return new TimeoutException(Microsoft.ServiceBus.SR.GetString(receiveRequestTimedOutNoLocalAddress, objArray));
			}
			string receiveRequestTimedOut = Resources.ReceiveRequestTimedOut;
			object[] absoluteUri = new object[] { channel.LocalAddress.Uri.AbsoluteUri, timeout };
			return new TimeoutException(Microsoft.ServiceBus.SR.GetString(receiveRequestTimedOut, absoluteUri));
		}

		public RequestContext EndReceiveRequest(IAsyncResult result)
		{
			return Microsoft.ServiceBus.Channels.ReplyChannel.HelpEndReceiveRequest(result);
		}

		public bool EndTryReceiveRequest(IAsyncResult result, out RequestContext context)
		{
			return base.EndDequeue(result, out context);
		}

		public bool EndWaitForRequest(IAsyncResult result)
		{
			return base.EndWaitForItem(result);
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) == typeof(IReplyChannel))
			{
				return (T)this;
			}
			T property = base.GetProperty<T>();
			if (property != null)
			{
				return property;
			}
			return default(T);
		}

		internal static IAsyncResult HelpBeginReceiveRequest(IReplyChannel channel, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.ReplyChannel.HelpReceiveRequestAsyncResult(channel, timeout, callback, state);
		}

		internal static RequestContext HelpEndReceiveRequest(IAsyncResult result)
		{
			return Microsoft.ServiceBus.Channels.ReplyChannel.HelpReceiveRequestAsyncResult.End(result);
		}

		internal static RequestContext HelpReceiveRequest(IReplyChannel channel, TimeSpan timeout)
		{
			RequestContext requestContext;
			if (!channel.TryReceiveRequest(timeout, out requestContext))
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(Microsoft.ServiceBus.Channels.ReplyChannel.CreateReceiveRequestTimedOutException(channel, timeout));
			}
			return requestContext;
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
		}

		public RequestContext ReceiveRequest()
		{
			return this.ReceiveRequest(base.DefaultReceiveTimeout);
		}

		public RequestContext ReceiveRequest(TimeSpan timeout)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			this.ThrowPending();
			return Microsoft.ServiceBus.Channels.ReplyChannel.HelpReceiveRequest(this, timeout);
		}

		public bool TryReceiveRequest(TimeSpan timeout, out RequestContext context)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			this.ThrowPending();
			return base.Dequeue(timeout, out context);
		}

		public bool WaitForRequest(TimeSpan timeout)
		{
			if (timeout < TimeSpan.Zero)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("timeout", (object)timeout, Microsoft.ServiceBus.SR.GetString(Resources.SFxTimeoutOutOfRange0, new object[0])));
			}
			this.ThrowPending();
			return base.WaitForItem(timeout);
		}

		private class HelpReceiveRequestAsyncResult : AsyncResult
		{
			private IReplyChannel channel;

			private TimeSpan timeout;

			private static AsyncCallback onReceiveRequest;

			private RequestContext requestContext;

			static HelpReceiveRequestAsyncResult()
			{
				Microsoft.ServiceBus.Channels.ReplyChannel.HelpReceiveRequestAsyncResult.onReceiveRequest = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ReplyChannel.HelpReceiveRequestAsyncResult.OnReceiveRequest));
			}

			public HelpReceiveRequestAsyncResult(IReplyChannel channel, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.channel = channel;
				this.timeout = timeout;
				IAsyncResult asyncResult = channel.BeginTryReceiveRequest(timeout, Microsoft.ServiceBus.Channels.ReplyChannel.HelpReceiveRequestAsyncResult.onReceiveRequest, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return;
				}
				this.HandleReceiveRequestComplete(asyncResult);
				base.Complete(true);
			}

			public static new RequestContext End(IAsyncResult result)
			{
				return AsyncResult.End<Microsoft.ServiceBus.Channels.ReplyChannel.HelpReceiveRequestAsyncResult>(result).requestContext;
			}

			private void HandleReceiveRequestComplete(IAsyncResult result)
			{
				if (!this.channel.EndTryReceiveRequest(result, out this.requestContext))
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(Microsoft.ServiceBus.Channels.ReplyChannel.CreateReceiveRequestTimedOutException(this.channel, this.timeout));
				}
			}

			private static void OnReceiveRequest(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.ReplyChannel.HelpReceiveRequestAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ReplyChannel.HelpReceiveRequestAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					asyncState.HandleReceiveRequestComplete(result);
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					exception = exception1;
				}
				asyncState.Complete(false, exception);
			}
		}
	}
}