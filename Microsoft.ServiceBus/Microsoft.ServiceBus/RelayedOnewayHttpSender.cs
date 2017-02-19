using Microsoft.ServiceBus.Common;
using System;
using System.IdentityModel.Tokens;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus
{
	internal class RelayedOnewayHttpSender : CommunicationObject, IRelayedOnewaySender, ICommunicationObject, IConnectionStatus
	{
		private IOutputChannel channel;

		private ChannelFactory<IOutputChannel> channelFactory;

		private Microsoft.ServiceBus.TokenProvider tokenProvider;

		private Uri via;

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
				return true;
			}
		}

		public Exception LastError
		{
			get
			{
				return null;
			}
		}

		public Microsoft.ServiceBus.TokenProvider TokenProvider
		{
			get
			{
				return this.tokenProvider;
			}
		}

		public RelayedOnewayHttpSender(BindingContext context, Uri uri, bool transportProtectionEnabled)
		{
			Binding wSHttpBinding;
			this.tokenProvider = TokenProviderUtility.CreateTokenProvider(context);
			if (this.tokenProvider != null)
			{
				transportProtectionEnabled = true;
			}
			if (!transportProtectionEnabled)
			{
				wSHttpBinding = new WSHttpBinding(SecurityMode.None);
				this.via = RelayedHttpUtility.ConvertToHttpUri(uri);
			}
			else
			{
				wSHttpBinding = new WSHttpBinding(SecurityMode.Transport);
				this.via = RelayedHttpUtility.ConvertToHttpsUri(uri);
			}
			this.channelFactory = new ChannelFactory<IOutputChannel>(wSHttpBinding);
			this.channelFactory.Open();
			this.channel = this.channelFactory.CreateChannel(new EndpointAddress(uri, new AddressHeader[0]), this.via);
			this.channel.Open();
			this.OnOnline(this, EventArgs.Empty);
		}

		private void AddToken(Message message, TimeSpan timeout)
		{
			if (this.TokenProvider != null)
			{
				SecurityToken token = this.TokenProvider.GetToken(this.via.AbsoluteUri, "Send", false, timeout);
				message.Headers.Add(new RelayTokenHeader(token));
			}
		}

		public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			this.AddToken(message, timeoutHelper.RemainingTime());
			return this.channel.BeginSend(message, timeoutHelper.RemainingTime(), callback, state);
		}

		public void EndSend(IAsyncResult result)
		{
			this.channel.EndSend(result);
		}

		protected override void OnAbort()
		{
			this.channelFactory.Abort();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.channelFactory.BeginClose(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.channelFactory.Close(timeout);
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			this.OnOffline(this, EventArgs.Empty);
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			this.channelFactory.EndClose(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
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
		}

		public void Send(Message message, TimeSpan timeout)
		{
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			this.AddToken(message, timeoutHelper.RemainingTime());
			this.channel.Send(message, timeoutHelper.RemainingTime());
		}

		public event EventHandler Connecting;

		public event EventHandler Offline;

		public event EventHandler Online;
	}
}