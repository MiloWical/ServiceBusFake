using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Channels;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace Microsoft.ServiceBus
{
	internal sealed class RelayedOnewayChannelListener : DelegatingTransportChannelListener<IInputChannel>
	{
		private readonly RelayedOnewayChannelListener.RelayedOnewayInputChannel channel;

		private readonly System.ServiceModel.Channels.MessageVersion messageVersion;

		private readonly Microsoft.ServiceBus.NameSettings nameSettings;

		private readonly string scheme;

		private System.Uri baseAddress;

		private IRelayedOnewayListener connection;

		private System.Uri listenUri;

		private string relativeAddress;

		public System.Uri BaseAddress
		{
			get
			{
				return this.baseAddress;
			}
		}

		private Microsoft.ServiceBus.Channels.InputChannelAcceptor ChannelAcceptor
		{
			get
			{
				return (Microsoft.ServiceBus.Channels.InputChannelAcceptor)base.Acceptor;
			}
		}

		public System.ServiceModel.Channels.MessageVersion MessageVersion
		{
			get
			{
				return this.messageVersion;
			}
		}

		public Microsoft.ServiceBus.NameSettings NameSettings
		{
			get
			{
				return this.nameSettings;
			}
		}

		public string RelativeAddress
		{
			get
			{
				return this.relativeAddress;
			}
		}

		public override System.Uri Uri
		{
			get
			{
				return this.listenUri;
			}
		}

		public RelayedOnewayChannelListener(BindingContext context, RelayedOnewayTransportBindingElement transportBindingElement) : base(context.Binding)
		{
			this.nameSettings = context.BindingParameters.Find<Microsoft.ServiceBus.NameSettings>();
			if (this.nameSettings == null)
			{
				this.nameSettings = new Microsoft.ServiceBus.NameSettings();
				this.nameSettings.ServiceSettings.ListenerType = ListenerType.Unicast;
			}
			this.scheme = context.Binding.Scheme;
			this.messageVersion = context.Binding.MessageVersion;
			switch (context.ListenUriMode)
			{
				case ListenUriMode.Explicit:
				{
					this.SetUri(context.ListenUriBaseAddress, context.ListenUriRelativeAddress);
					break;
				}
				case ListenUriMode.Unique:
				{
					this.SetUniqueUri(context.ListenUriBaseAddress, context.ListenUriRelativeAddress);
					break;
				}
			}
			this.channel = new RelayedOnewayChannelListener.RelayedOnewayInputChannel(this, new EndpointAddress(this.Uri, new AddressHeader[0]));
			base.Acceptor = new Microsoft.ServiceBus.Channels.InputChannelAcceptor(this, () => this.GetPendingException());
			this.ChannelAcceptor.EnqueueAndDispatch(this.channel, new Action(this.OnChannelDequeued));
			this.connection = RelayedOnewayManager.RegisterListener(context, transportBindingElement, this);
		}

		private void CleanUpConnection()
		{
			IRelayedOnewayListener relayedOnewayListener = null;
			lock (base.ThisLock)
			{
				relayedOnewayListener = this.connection;
				this.connection = null;
			}
			if (relayedOnewayListener != null)
			{
				relayedOnewayListener.Unregister(this);
				relayedOnewayListener.Abort();
			}
		}

		public void EnqueueAndDispatch(Message message, Action dequeuedCallback)
		{
			this.channel.EnqueueAndDispatch(message, dequeuedCallback);
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) == typeof(System.ServiceModel.Channels.MessageVersion))
			{
				return (T)this.messageVersion;
			}
			if (typeof(T) != typeof(IConnectionStatus))
			{
				return base.GetProperty<T>();
			}
			return (T)this.connection;
		}

		protected override void OnAbort()
		{
			this.CleanUpConnection();
			base.OnAbort();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IRelayedOnewayListener relayedOnewayListener = null;
			lock (base.ThisLock)
			{
				relayedOnewayListener = this.connection;
				this.connection = null;
			}
			if (relayedOnewayListener == null)
			{
				return new DelegatingAsyncResult(new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(this.OnBeginClose), new Action<IAsyncResult>(this.OnEndClose), timeout, callback, state);
			}
			relayedOnewayListener.Unregister(this);
			IRelayedOnewayListener relayedOnewayListener1 = relayedOnewayListener;
			IRelayedOnewayListener relayedOnewayListener2 = relayedOnewayListener;
			return new Microsoft.ServiceBus.Common.ChainedAsyncResult(timeout, callback, state, new Microsoft.ServiceBus.Common.ChainedBeginHandler(relayedOnewayListener1.BeginClose), new Microsoft.ServiceBus.Common.ChainedEndHandler(relayedOnewayListener2.EndClose), new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.OnBeginClose), new Microsoft.ServiceBus.Common.ChainedEndHandler(this.OnEndClose));
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IRelayedOnewayListener relayedOnewayListener = this.connection;
			if (relayedOnewayListener == null)
			{
				return new DelegatingAsyncResult(new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(this.OnBeginOpen), new Action<IAsyncResult>(this.OnEndOpen), timeout, callback, state);
			}
			Microsoft.ServiceBus.Common.ChainedBeginHandler chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.OnBeginOpen);
			Microsoft.ServiceBus.Common.ChainedEndHandler chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.OnEndOpen);
			IRelayedOnewayListener relayedOnewayListener1 = relayedOnewayListener;
			IRelayedOnewayListener relayedOnewayListener2 = relayedOnewayListener;
			return new Microsoft.ServiceBus.Common.ChainedAsyncResult(timeout, callback, state, chainedBeginHandler, chainedEndHandler, new Microsoft.ServiceBus.Common.ChainedBeginHandler(relayedOnewayListener1.BeginOpen), new Microsoft.ServiceBus.Common.ChainedEndHandler(relayedOnewayListener2.EndOpen));
		}

		private void OnChannelDequeued()
		{
		}

		protected override void OnClose(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			IRelayedOnewayListener relayedOnewayListener = null;
			lock (base.ThisLock)
			{
				relayedOnewayListener = this.connection;
				this.connection = null;
			}
			if (relayedOnewayListener != null)
			{
				relayedOnewayListener.Unregister(this);
				relayedOnewayListener.Close(timeoutHelper.RemainingTime());
			}
			base.OnClose(timeoutHelper.RemainingTime());
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			if (!(result is DelegatingAsyncResult))
			{
				Microsoft.ServiceBus.Common.ChainedAsyncResult.End(result);
				return;
			}
			AsyncResult<DelegatingAsyncResult>.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			if (!(result is DelegatingAsyncResult))
			{
				Microsoft.ServiceBus.Common.ChainedAsyncResult.End(result);
				return;
			}
			AsyncResult<DelegatingAsyncResult>.End(result);
		}

		protected override void OnFaulted()
		{
			this.CleanUpConnection();
			base.OnFaulted();
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			base.OnOpen(timeoutHelper.RemainingTime());
			IRelayedOnewayListener relayedOnewayListener = this.connection;
			if (relayedOnewayListener != null)
			{
				relayedOnewayListener.Open(timeoutHelper.RemainingTime());
			}
		}

		public void SetUniqueUri(System.Uri baseAddress, string relativeAddress)
		{
			if (baseAddress.Scheme != this.scheme)
			{
				throw new ArgumentException(SRClient.BaseAddressScheme(this.scheme), "baseAddress");
			}
			if (relativeAddress.Length <= 0 || relativeAddress.EndsWith("/", StringComparison.Ordinal))
			{
				Guid guid = Guid.NewGuid();
				this.SetUri(baseAddress, string.Concat(relativeAddress, guid.ToString(), "/"));
				return;
			}
			Guid guid1 = Guid.NewGuid();
			this.SetUri(baseAddress, string.Concat(relativeAddress, "/", guid1.ToString(), "/"));
		}

		public void SetUri(System.Uri baseAddress, string relativeAddress)
		{
			if (baseAddress.Scheme != this.scheme)
			{
				throw new ArgumentException(SRClient.BaseAddressScheme(this.scheme), "baseAddress");
			}
			if (!baseAddress.AbsoluteUri.EndsWith("/", StringComparison.Ordinal))
			{
				this.baseAddress = new System.Uri(string.Concat(baseAddress.AbsoluteUri, "/"));
			}
			else
			{
				this.baseAddress = baseAddress;
			}
			this.relativeAddress = relativeAddress;
			if (relativeAddress.Length <= 0)
			{
				this.listenUri = baseAddress;
				return;
			}
			this.listenUri = new System.Uri(baseAddress, relativeAddress);
		}

		private class RelayedOnewayInputChannel : Microsoft.ServiceBus.Channels.InputChannel
		{
			public RelayedOnewayInputChannel(RelayedOnewayChannelListener channelListener, EndpointAddress localAddress) : base(channelListener, localAddress)
			{
			}
		}
	}
}