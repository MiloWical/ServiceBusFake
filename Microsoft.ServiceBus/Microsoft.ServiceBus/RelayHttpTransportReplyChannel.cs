using Microsoft.ServiceBus.Channels;
using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class RelayHttpTransportReplyChannel : Microsoft.ServiceBus.Channels.LayeredChannel<IReplyChannel>, IReplyChannel, IChannel, ICommunicationObject
	{
		private readonly EndpointAddress localAddress;

		private readonly MessageEncoder encoder;

		private readonly bool preserveRawHttp;

		public EndpointAddress LocalAddress
		{
			get
			{
				return this.localAddress;
			}
		}

		public RelayHttpTransportReplyChannel(ChannelManagerBase channelManager, IReplyChannel innerChannel, MessageEncoder encoder, bool preserveRawHttp) : base(channelManager, innerChannel)
		{
			this.localAddress = innerChannel.LocalAddress;
			this.encoder = encoder;
			this.preserveRawHttp = preserveRawHttp;
		}

		public IAsyncResult BeginReceiveRequest(AsyncCallback callback, object state)
		{
			return this.BeginReceiveRequest(base.DefaultReceiveTimeout, callback, state);
		}

		public IAsyncResult BeginReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return Microsoft.ServiceBus.Channels.ReplyChannel.HelpBeginReceiveRequest(this, timeout, callback, state);
		}

		public IAsyncResult BeginTryReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.InnerChannel.BeginTryReceiveRequest(timeout, callback, state);
		}

		public IAsyncResult BeginWaitForRequest(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.InnerChannel.BeginWaitForRequest(timeout, callback, state);
		}

		public RequestContext EndReceiveRequest(IAsyncResult result)
		{
			return Microsoft.ServiceBus.Channels.ReplyChannel.HelpEndReceiveRequest(result);
		}

		public bool EndTryReceiveRequest(IAsyncResult result, out RequestContext context)
		{
			RequestContext requestContext;
			if (!base.InnerChannel.EndTryReceiveRequest(result, out requestContext))
			{
				context = null;
				return false;
			}
			context = this.WrapInnerRequestContext(requestContext);
			return true;
		}

		public bool EndWaitForRequest(IAsyncResult result)
		{
			return base.InnerChannel.EndWaitForRequest(result);
		}

		public RequestContext ReceiveRequest()
		{
			return this.ReceiveRequest(base.DefaultReceiveTimeout);
		}

		public RequestContext ReceiveRequest(TimeSpan timeout)
		{
			return Microsoft.ServiceBus.Channels.ReplyChannel.HelpReceiveRequest(this, timeout);
		}

		public bool TryReceiveRequest(TimeSpan timeout, out RequestContext context)
		{
			RequestContext requestContext;
			if (!base.InnerChannel.TryReceiveRequest(timeout, out requestContext))
			{
				context = null;
				return false;
			}
			context = this.WrapInnerRequestContext(requestContext);
			return true;
		}

		public bool WaitForRequest(TimeSpan timeout)
		{
			return base.InnerChannel.WaitForRequest(timeout);
		}

		private RequestContext WrapInnerRequestContext(RequestContext request)
		{
			if (request == null)
			{
				return null;
			}
			return new RelayHttpTransportReplyChannel.RelayHttpTransportRequestContext(request, base.Manager, this.encoder, this.preserveRawHttp);
		}

		private class RelayHttpTransportRequestContext : RequestContext
		{
			private readonly bool preserveRawHttp;

			private readonly RequestContext innerContext;

			private readonly IDefaultCommunicationTimeouts defaultTimeouts;

			private readonly string requestAction;

			private readonly Message requestMessage;

			private readonly MessageEncoder encoder;

			private readonly MessageWrapper wrapper;

			private bool disposed;

			private bool isSoapRequest;

			public override Message RequestMessage
			{
				get
				{
					return this.requestMessage;
				}
			}

			private object ThisLock
			{
				get
				{
					return this.wrapper;
				}
			}

			public RelayHttpTransportRequestContext(RequestContext innerContext, IDefaultCommunicationTimeouts defaultTimeouts, MessageEncoder encoder, bool preserveRawHttp)
			{
				this.preserveRawHttp = preserveRawHttp;
				this.innerContext = innerContext;
				this.defaultTimeouts = defaultTimeouts;
				this.encoder = encoder;
				this.wrapper = new MessageWrapper(this.encoder);
				this.requestAction = this.innerContext.RequestMessage.Headers.Action;
				this.requestMessage = this.PrepareRequest(this.innerContext.RequestMessage);
			}

			public override void Abort()
			{
				this.Dispose(true);
			}

			public override IAsyncResult BeginReply(Message message, AsyncCallback callback, object state)
			{
				return this.BeginReply(message, this.defaultTimeouts.SendTimeout, callback, state);
			}

			public override IAsyncResult BeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state)
			{
				Message message1 = this.PrepareReply(message);
				return this.innerContext.BeginReply(message1, timeout, callback, state);
			}

			public override void Close()
			{
				this.Close(this.defaultTimeouts.CloseTimeout);
			}

			public override void Close(TimeSpan timeout)
			{
				this.Dispose(true);
			}

			private Message CreateAckMessage(HttpStatusCode statusCode, string statusDescription)
			{
				Message message = Message.CreateMessage(MessageVersion.None, "");
				HttpResponseMessageProperty httpResponseMessageProperty = new HttpResponseMessageProperty()
				{
					StatusCode = statusCode,
					SuppressEntityBody = true
				};
				if (statusDescription.Length > 0)
				{
					httpResponseMessageProperty.StatusDescription = statusDescription;
				}
				message.Properties.Add(HttpResponseMessageProperty.Name, httpResponseMessageProperty);
				return RelayedHttpUtility.ConvertWebResponseToSoapResponse(message, string.Concat(this.requestAction, "Response"));
			}

			protected override void Dispose(bool disposing)
			{
				bool flag = false;
				lock (this.ThisLock)
				{
					if (!this.disposed)
					{
						this.disposed = true;
						flag = true;
					}
				}
				if (flag && this.innerContext != null)
				{
					this.innerContext.Close();
				}
			}

			public override void EndReply(IAsyncResult result)
			{
				this.innerContext.EndReply(result);
			}

			private Message PrepareReply(Message message)
			{
				string str;
				if (message == null)
				{
					return this.CreateAckMessage(HttpStatusCode.Accepted, string.Empty);
				}
				if (!this.isSoapRequest)
				{
					if (message.Headers.Action == null)
					{
						str = (this.requestAction == null ? string.Empty : string.Concat(this.requestAction, "Response"));
					}
					else
					{
						str = string.Concat(message.Headers.Action, "Response");
					}
					return RelayedHttpUtility.ConvertWebResponseToSoapResponse(message, str);
				}
				if (this.encoder.MessageVersion.Addressing == AddressingVersion.None)
				{
					message.Headers.Action = null;
					message.Headers.To = null;
					message.Headers.ReplyTo = null;
					message.Headers.From = null;
					message.Headers.FaultTo = null;
					message.Headers.MessageId = null;
					message.Headers.RelatesTo = null;
				}
				return message.ConvertSoapResponseToWrappedSoapResponse(message.Headers.Action, this.wrapper);
			}

			private Message PrepareRequest(Message message)
			{
				if (message.Headers.FindHeader("BodyFormat", "http://schemas.microsoft.com/netservices/2009/05/servicebus/body") == -1)
				{
					this.isSoapRequest = true;
					return message;
				}
				return RelayedHttpUtility.ConvertSoapRequestToWebRequest(this.encoder, message, out this.isSoapRequest, this.wrapper, this.preserveRawHttp);
			}

			public override void Reply(Message message)
			{
				this.Reply(message, this.defaultTimeouts.SendTimeout);
			}

			public override void Reply(Message message, TimeSpan timeout)
			{
				Message message1 = this.PrepareReply(message);
				this.innerContext.Reply(message1, timeout);
			}
		}
	}
}