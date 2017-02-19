using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class ConnectionOrientedTransportChannelListener : Microsoft.ServiceBus.Channels.TransportChannelListener, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings, Microsoft.ServiceBus.Channels.ITransportFactorySettings, IDefaultCommunicationTimeouts, Microsoft.ServiceBus.Channels.IConnectionOrientedListenerSettings, Microsoft.ServiceBus.Channels.IConnectionOrientedConnectionSettings
	{
		private int connectionBufferSize;

		private bool exposeConnectionProperty;

		private TimeSpan channelInitializationTimeout;

		private int maxBufferSize;

		private int maxPendingConnections;

		private TimeSpan maxOutputDelay;

		private int maxPendingAccepts;

		private TimeSpan idleTimeout;

		private int maxPooledConnections;

		private System.ServiceModel.TransferMode transferMode;

		private ISecurityCapabilities securityCapabilities;

		private StreamUpgradeProvider upgrade;

		private bool ownUpgrade;

		private EndpointIdentity identity;

		public TimeSpan ChannelInitializationTimeout
		{
			get
			{
				return this.channelInitializationTimeout;
			}
		}

		public int ConnectionBufferSize
		{
			get
			{
				return this.connectionBufferSize;
			}
		}

		internal bool ExposeConnectionProperty
		{
			get
			{
				return this.exposeConnectionProperty;
			}
		}

		public System.ServiceModel.HostNameComparisonMode HostNameComparisonMode
		{
			get
			{
				return base.HostNameComparisonModeInternal;
			}
		}

		public TimeSpan IdleTimeout
		{
			get
			{
				return this.idleTimeout;
			}
		}

		public int MaxBufferSize
		{
			get
			{
				return this.maxBufferSize;
			}
		}

		public TimeSpan MaxOutputDelay
		{
			get
			{
				return this.maxOutputDelay;
			}
		}

		public int MaxPendingAccepts
		{
			get
			{
				return this.maxPendingAccepts;
			}
		}

		public int MaxPendingConnections
		{
			get
			{
				return this.maxPendingConnections;
			}
		}

		public int MaxPooledConnections
		{
			get
			{
				return this.maxPooledConnections;
			}
		}

		internal abstract TraceCode MessageReceivedTraceCode
		{
			get;
		}

		internal abstract TraceCode MessageReceiveFailedTraceCode
		{
			get;
		}

		ServiceSecurityAuditBehavior Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings.AuditBehavior
		{
			get
			{
				return base.AuditBehavior;
			}
		}

		int Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings.MaxBufferSize
		{
			get
			{
				return this.MaxBufferSize;
			}
		}

		System.ServiceModel.TransferMode Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings.TransferMode
		{
			get
			{
				return this.TransferMode;
			}
		}

		StreamUpgradeProvider Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings.Upgrade
		{
			get
			{
				return this.Upgrade;
			}
		}

		public System.ServiceModel.TransferMode TransferMode
		{
			get
			{
				return this.transferMode;
			}
		}

		public StreamUpgradeProvider Upgrade
		{
			get
			{
				return this.upgrade;
			}
		}

		protected ConnectionOrientedTransportChannelListener(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportBindingElement bindingElement, BindingContext context) : base(bindingElement, context, bindingElement.HostNameComparisonMode)
		{
			if (bindingElement.TransferMode == System.ServiceModel.TransferMode.Buffered)
			{
				if (bindingElement.MaxReceivedMessageSize > (long)2147483647)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("bindingElement.MaxReceivedMessageSize", Microsoft.ServiceBus.SR.GetString(Resources.MaxReceivedMessageSizeMustBeInIntegerRange, new object[0])));
				}
				if ((long)bindingElement.MaxBufferSize != bindingElement.MaxReceivedMessageSize)
				{
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("bindingElement", Microsoft.ServiceBus.SR.GetString(Resources.MaxBufferSizeMustMatchMaxReceivedMessageSize, new object[0]));
				}
			}
			else if ((long)bindingElement.MaxBufferSize > bindingElement.MaxReceivedMessageSize)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("bindingElement", Microsoft.ServiceBus.SR.GetString(Resources.MaxBufferSizeMustNotExceedMaxReceivedMessageSize, new object[0]));
			}
			this.connectionBufferSize = bindingElement.ConnectionBufferSize;
			this.exposeConnectionProperty = bindingElement.ExposeConnectionProperty;
			base.InheritBaseAddressSettings = bindingElement.InheritBaseAddressSettings;
			this.channelInitializationTimeout = bindingElement.ChannelInitializationTimeout;
			this.maxBufferSize = bindingElement.MaxBufferSize;
			this.maxPendingConnections = bindingElement.MaxPendingConnections;
			this.maxOutputDelay = bindingElement.MaxOutputDelay;
			this.maxPendingAccepts = bindingElement.MaxPendingAccepts;
			this.transferMode = bindingElement.TransferMode;
			Collection<StreamUpgradeBindingElement> streamUpgradeBindingElements = context.BindingParameters.FindAll<StreamUpgradeBindingElement>();
			if (streamUpgradeBindingElements.Count > 1)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.MultipleStreamUpgradeProvidersInParameters, new object[0])));
			}
			if (streamUpgradeBindingElements.Count == 1)
			{
				this.upgrade = streamUpgradeBindingElements[0].BuildServerStreamUpgradeProvider(context);
				this.ownUpgrade = true;
				context.BindingParameters.Remove<StreamUpgradeBindingElement>();
				this.securityCapabilities = streamUpgradeBindingElements[0].GetProperty<ISecurityCapabilities>(context);
			}
		}

		internal override int GetMaxBufferSize()
		{
			return this.MaxBufferSize;
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) == typeof(EndpointIdentity))
			{
				return (T)this.identity;
			}
			if (typeof(T) != typeof(ISecurityCapabilities))
			{
				return base.GetProperty<T>();
			}
			return (T)this.securityCapabilities;
		}

		private StreamUpgradeProvider GetUpgrade()
		{
			StreamUpgradeProvider streamUpgradeProvider = null;
			lock (base.ThisLock)
			{
				if (this.ownUpgrade)
				{
					streamUpgradeProvider = this.upgrade;
					this.ownUpgrade = false;
				}
			}
			return streamUpgradeProvider;
		}

		protected override void OnAbort()
		{
			StreamUpgradeProvider upgrade = this.GetUpgrade();
			if (upgrade != null)
			{
				upgrade.Abort();
			}
			base.OnAbort();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			StreamUpgradeProvider upgrade = this.GetUpgrade();
			if (upgrade == null)
			{
				return new Microsoft.ServiceBus.Channels.ChainedCloseAsyncResult(timeout, callback, state, new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.OnBeginClose), new Microsoft.ServiceBus.Common.ChainedEndHandler(this.OnEndClose), new ICommunicationObject[0]);
			}
			Microsoft.ServiceBus.Common.ChainedBeginHandler chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.OnBeginClose);
			Microsoft.ServiceBus.Common.ChainedEndHandler chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.OnEndClose);
			ICommunicationObject[] communicationObjectArray = new ICommunicationObject[] { upgrade };
			return new Microsoft.ServiceBus.Channels.ChainedCloseAsyncResult(timeout, callback, state, chainedBeginHandler, chainedEndHandler, communicationObjectArray);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			StreamUpgradeProvider upgrade = this.Upgrade;
			if (upgrade == null)
			{
				return base.OnBeginOpen(timeout, callback, state);
			}
			Microsoft.ServiceBus.Common.ChainedBeginHandler chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.OnBeginOpen);
			Microsoft.ServiceBus.Common.ChainedEndHandler chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.OnEndOpen);
			ICommunicationObject[] communicationObjectArray = new ICommunicationObject[] { upgrade };
			return new Microsoft.ServiceBus.Channels.ChainedOpenAsyncResult(timeout, callback, state, chainedBeginHandler, chainedEndHandler, communicationObjectArray);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			StreamUpgradeProvider upgrade = this.GetUpgrade();
			if (upgrade == null)
			{
				base.OnClose(timeout);
				return;
			}
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			upgrade.Close(timeoutHelper.RemainingTime());
			base.OnClose(timeoutHelper.RemainingTime());
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			Microsoft.ServiceBus.Common.ChainedAsyncResult.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			if (result is Microsoft.ServiceBus.Channels.ChainedOpenAsyncResult)
			{
				Microsoft.ServiceBus.Common.ChainedAsyncResult.End(result);
				return;
			}
			base.OnEndOpen(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			base.OnOpen(timeoutHelper.RemainingTime());
			StreamUpgradeProvider upgrade = this.Upgrade;
			if (upgrade != null)
			{
				upgrade.Open(timeoutHelper.RemainingTime());
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			StreamSecurityUpgradeProvider upgrade = this.Upgrade as StreamSecurityUpgradeProvider;
			if (upgrade != null)
			{
				this.identity = upgrade.Identity;
			}
		}

		internal void SetIdleTimeout(TimeSpan idleTimeout)
		{
			this.idleTimeout = idleTimeout;
		}

		internal void SetMaxPooledConnections(int maxPooledConnections)
		{
			this.maxPooledConnections = maxPooledConnections;
		}

		protected override void ValidateUri(System.Uri uri)
		{
			base.ValidateUri(uri);
			int num = 2048;
			int byteCount = Encoding.UTF8.GetByteCount(uri.AbsoluteUri);
			if (byteCount > num)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string uriLengthExceedsMaxSupportedSize = Resources.UriLengthExceedsMaxSupportedSize;
				object[] objArray = new object[] { uri, byteCount, num };
				throw exceptionUtility.ThrowHelperError(new QuotaExceededException(Microsoft.ServiceBus.SR.GetString(uriLengthExceedsMaxSupportedSize, objArray)));
			}
		}

		protected class ConnectionOrientedTransportReplyChannelAcceptor : Microsoft.ServiceBus.Channels.TransportReplyChannelAcceptor
		{
			private StreamUpgradeProvider upgrade;

			public ConnectionOrientedTransportReplyChannelAcceptor(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelListener listener) : base(listener)
			{
				this.upgrade = listener.GetUpgrade();
			}

			private IAsyncResult DummyBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new CompletedAsyncResult(callback, state);
			}

			private void DummyEndClose(IAsyncResult result)
			{
				CompletedAsyncResult.End(result);
			}

			protected override void OnAbort()
			{
				base.OnAbort();
				if (this.upgrade != null && !this.TransferUpgrade())
				{
					this.upgrade.Abort();
				}
			}

			protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				Microsoft.ServiceBus.Common.ChainedBeginHandler chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.DummyBeginClose);
				Microsoft.ServiceBus.Common.ChainedEndHandler chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.DummyEndClose);
				if (this.upgrade != null && !this.TransferUpgrade())
				{
					chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.upgrade.BeginClose);
					chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.upgrade.EndClose);
				}
				return new Microsoft.ServiceBus.Common.ChainedAsyncResult(timeout, callback, state, new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.OnBeginClose), new Microsoft.ServiceBus.Common.ChainedEndHandler(this.OnEndClose), chainedBeginHandler, chainedEndHandler);
			}

			protected override void OnClose(TimeSpan timeout)
			{
				TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
				base.OnClose(timeoutHelper.RemainingTime());
				if (this.upgrade != null && !this.TransferUpgrade())
				{
					this.upgrade.Close(timeoutHelper.RemainingTime());
				}
			}

			protected override Microsoft.ServiceBus.Channels.ReplyChannel OnCreateChannel()
			{
				return new Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelListener.ConnectionOrientedTransportReplyChannelAcceptor.ConnectionOrientedTransportReplyChannel(base.ChannelManager, null);
			}

			protected override void OnEndClose(IAsyncResult result)
			{
				Microsoft.ServiceBus.Common.ChainedAsyncResult.End(result);
			}

			private bool TransferUpgrade()
			{
				Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelListener.ConnectionOrientedTransportReplyChannelAcceptor.ConnectionOrientedTransportReplyChannel currentChannel = (Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelListener.ConnectionOrientedTransportReplyChannelAcceptor.ConnectionOrientedTransportReplyChannel)base.GetCurrentChannel();
				if (currentChannel == null)
				{
					return false;
				}
				return currentChannel.TransferUpgrade(this.upgrade);
			}

			private class ConnectionOrientedTransportReplyChannel : Microsoft.ServiceBus.Channels.TransportReplyChannelAcceptor.TransportReplyChannel
			{
				private StreamUpgradeProvider upgrade;

				public ConnectionOrientedTransportReplyChannel(ChannelManagerBase channelManager, EndpointAddress localAddress) : base(channelManager, localAddress)
				{
				}

				private IAsyncResult DummyBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
				{
					return new CompletedAsyncResult(callback, state);
				}

				private void DummyEndClose(IAsyncResult result)
				{
					CompletedAsyncResult.End(result);
				}

				protected override void OnAbort()
				{
					if (this.upgrade != null)
					{
						this.upgrade.Abort();
					}
					base.OnAbort();
				}

				protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
				{
					Microsoft.ServiceBus.Common.ChainedBeginHandler chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.DummyBeginClose);
					Microsoft.ServiceBus.Common.ChainedEndHandler chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.DummyEndClose);
					if (this.upgrade != null)
					{
						chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.upgrade.BeginClose);
						chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.upgrade.EndClose);
					}
					return new Microsoft.ServiceBus.Common.ChainedAsyncResult(timeout, callback, state, chainedBeginHandler, chainedEndHandler, new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.OnBeginClose), new Microsoft.ServiceBus.Common.ChainedEndHandler(this.OnEndClose));
				}

				protected override void OnClose(TimeSpan timeout)
				{
					TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
					if (this.upgrade != null)
					{
						this.upgrade.Close(timeoutHelper.RemainingTime());
					}
					base.OnClose(timeoutHelper.RemainingTime());
				}

				protected override void OnEndClose(IAsyncResult result)
				{
					Microsoft.ServiceBus.Common.ChainedAsyncResult.End(result);
				}

				public bool TransferUpgrade(StreamUpgradeProvider upgrade)
				{
					bool flag;
					lock (base.ThisLock)
					{
						if (base.State == CommunicationState.Opened)
						{
							this.upgrade = upgrade;
							flag = true;
						}
						else
						{
							flag = false;
						}
					}
					return flag;
				}
			}
		}
	}
}