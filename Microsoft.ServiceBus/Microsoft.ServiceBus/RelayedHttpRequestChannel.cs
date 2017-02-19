using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class RelayedHttpRequestChannel : Microsoft.ServiceBus.Channels.LayeredChannel<IRequestChannel>, IRequestChannel, IChannel, ICommunicationObject
	{
		private EndpointAddress remoteAddress;

		private Uri via;

		private TokenProvider tokenProvider;

		public EndpointAddress RemoteAddress
		{
			get
			{
				return this.remoteAddress;
			}
		}

		public Uri Via
		{
			get
			{
				return this.via;
			}
		}

		public RelayedHttpRequestChannel(ChannelManagerBase channelManager, TokenProvider tokenProvider, IRequestChannel innerChannel) : base(channelManager, innerChannel)
		{
			this.tokenProvider = tokenProvider;
			this.remoteAddress = innerChannel.RemoteAddress;
			this.via = innerChannel.Via;
		}

		private void AddTokenToMessage(Message message, TimeSpan timeout)
		{
			HttpRequestMessageProperty item;
			if (this.tokenProvider == null)
			{
				return;
			}
			this.ValidateTransportProtectionForToken();
			string str = "Send";
			if (message.Properties.ContainsKey(HttpRequestMessageProperty.Name))
			{
				item = message.Properties[HttpRequestMessageProperty.Name] as HttpRequestMessageProperty;
			}
			else
			{
				item = new HttpRequestMessageProperty();
				message.Properties.Add(HttpRequestMessageProperty.Name, item);
			}
			if (!this.tokenProvider.IsWebTokenSupported)
			{
				throw new NotSupportedException(SRClient.HTTPAuthTokenNotSupportedException);
			}
			string str1 = ServiceBusUriHelper.NormalizeUri(message.Headers.To ?? this.via, false);
			string webToken = this.tokenProvider.GetWebToken(str1, str, false, timeout);
			item.Headers["ServiceBusAuthorization"] = webToken;
		}

		public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			RelayedHttpRequestChannel.RequestAsyncResult requestAsyncResult = new RelayedHttpRequestChannel.RequestAsyncResult(this, callback, state);
			this.AddTokenToMessage(message, timeoutHelper.RemainingTime());
			requestAsyncResult.SendRequest(message, timeoutHelper.RemainingTime());
			return requestAsyncResult;
		}

		public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
		{
			return this.BeginRequest(message, base.DefaultSendTimeout, callback, state);
		}

		public Message EndRequest(IAsyncResult result)
		{
			return AsyncResult<RelayedHttpRequestChannel.RequestAsyncResult>.End(result).Reply;
		}

		public Message Request(Message message, TimeSpan timeout)
		{
			return this.EndRequest(this.BeginRequest(message, timeout, null, null));
		}

		public Message Request(Message message)
		{
			return this.EndRequest(this.BeginRequest(message, null, null));
		}

		private void ValidateTransportProtectionForToken()
		{
			if (this.via.Scheme != Uri.UriSchemeHttps)
			{
				throw new CommunicationException(SRClient.TransportSecurity);
			}
		}

		private class RequestAsyncResult : AsyncResult<RelayedHttpRequestChannel.RequestAsyncResult>
		{
			private readonly static AsyncCallback requestCallback;

			private readonly IRequestChannel innerChannel;

			public Message Reply
			{
				get;
				private set;
			}

			protected override TraceEventType TraceEventType
			{
				get
				{
					return TraceEventType.Warning;
				}
			}

			static RequestAsyncResult()
			{
				RelayedHttpRequestChannel.RequestAsyncResult.requestCallback = new AsyncCallback(RelayedHttpRequestChannel.RequestAsyncResult.RequestCallback);
			}

			public RequestAsyncResult(RelayedHttpRequestChannel channel, AsyncCallback callback, object state) : base(callback, state)
			{
				this.innerChannel = channel.InnerChannel;
			}

			private static void RequestCallback(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				((RelayedHttpRequestChannel.RequestAsyncResult)result.AsyncState).RequestComplete(result, false);
			}

			private void RequestComplete(IAsyncResult result, bool completedSynchronously)
			{
				Exception exception = null;
				try
				{
					this.Reply = this.innerChannel.EndRequest(result);
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
				base.Complete(completedSynchronously, exception);
			}

			public void SendRequest(Message request, TimeSpan timeout)
			{
				IAsyncResult asyncResult = this.innerChannel.BeginRequest(request, timeout, RelayedHttpRequestChannel.RequestAsyncResult.requestCallback, this);
				if (asyncResult.CompletedSynchronously)
				{
					this.RequestComplete(asyncResult, true);
				}
			}
		}
	}
}