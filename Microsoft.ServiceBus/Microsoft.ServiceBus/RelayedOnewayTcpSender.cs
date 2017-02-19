using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.IdentityModel.Tokens;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal class RelayedOnewayTcpSender : CommunicationObject, IRelayedOnewaySender, ICommunicationObject, IConnectionStatus
	{
		private readonly RelayedOnewayTcpClient client;

		protected override TimeSpan DefaultCloseTimeout
		{
			get
			{
				return Microsoft.ServiceBus.ServiceDefaults.CloseTimeout;
			}
		}

		protected override TimeSpan DefaultOpenTimeout
		{
			get
			{
				return Microsoft.ServiceBus.ServiceDefaults.OpenTimeout;
			}
		}

		public bool IsOnline
		{
			get
			{
				return this.client.IsOnline;
			}
		}

		public Exception LastError
		{
			get
			{
				return this.client.LastError;
			}
		}

		public Microsoft.ServiceBus.TokenProvider TokenProvider
		{
			get
			{
				return this.client.TokenProvider;
			}
		}

		public RelayedOnewayTcpSender(BindingContext context, RelayedOnewayTransportBindingElement transportBindingElement, Uri uri, bool transportProtectionEnabled, EventTraceActivity activity)
		{
			this.client = new RelayedOnewayTcpSender.RelayedOnewayTcpSenderClient(context, transportBindingElement, uri, transportProtectionEnabled, activity);
			this.client.Connecting += new EventHandler(this.OnConnecting);
			this.client.Online += new EventHandler(this.OnOnline);
			this.client.Offline += new EventHandler(this.OnOffline);
		}

		public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.client.BeginSend(message, timeout, callback, state);
		}

		public void EndSend(IAsyncResult result)
		{
			this.client.EndSend(result);
		}

		protected override void OnAbort()
		{
			this.client.Abort();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.client.BeginClose(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.client.BeginOpen(timeout, callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.client.Close(timeout);
		}

		private void OnConnecting(object sender, EventArgs args)
		{
			EventHandler eventHandler = this.Connecting;
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			this.client.EndClose(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			this.client.EndOpen(result);
		}

		private void OnOffline(object sender, EventArgs args)
		{
			EventHandler eventHandler = this.Offline;
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		private void OnOnline(object sender, EventArgs args)
		{
			EventHandler eventHandler = this.Online;
			if (eventHandler != null)
			{
				eventHandler(this, EventArgs.Empty);
			}
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			this.client.Open();
		}

		public void Send(Message message, TimeSpan timeout)
		{
			this.client.Send(message, timeout);
		}

		public event EventHandler Connecting;

		public event EventHandler Offline;

		public event EventHandler Online;

		private class RelayedOnewayTcpSenderClient : RelayedOnewayTcpClient
		{
			public RelayedOnewayTcpSenderClient(BindingContext context, RelayedOnewayTransportBindingElement transportBindingElement, Uri uri, bool transportProtectionEnabled, EventTraceActivity activity) : base(context, transportBindingElement, uri, transportProtectionEnabled, activity)
			{
				base.IsListener = false;
			}

			protected override RelayedOnewayTcpClient.RelayedOnewayConnection Connect(TimeSpan timeout)
			{
				RelayedOnewayTcpClient.RelayedOnewayConnection relayedOnewayConnection;
				Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				Message uniqueId = Message.CreateMessage(base.MessageVersion, "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/Connect", new ConnectMessage(base.Uri));
				TrackingIdHeader.TryAddOrUpdate(uniqueId.Headers, base.Activity.ActivityId.ToString());
				uniqueId.Headers.MessageId = new UniqueId();
				uniqueId.Headers.ReplyTo = EndpointAddress2.AnonymousAddress;
				if (base.TokenProvider != null)
				{
					SecurityToken token = base.TokenProvider.GetToken(base.AppliesTo, "Send", false, timeoutHelper.RemainingTime());
					uniqueId.Headers.Add(new RelayTokenHeader(token));
				}
				using (RelayedOnewayTcpClient.ConnectRequestReplyContext connectRequestReplyContext = new RelayedOnewayTcpClient.ConnectRequestReplyContext(this))
				{
					connectRequestReplyContext.Send(uniqueId, timeoutHelper.RemainingTime(), out relayedOnewayConnection);
				}
				return relayedOnewayConnection;
			}
		}
	}
}