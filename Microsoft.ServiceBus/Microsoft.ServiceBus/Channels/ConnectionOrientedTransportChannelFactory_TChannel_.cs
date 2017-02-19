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

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class ConnectionOrientedTransportChannelFactory<TChannel> : Microsoft.ServiceBus.Channels.TransportChannelFactory<TChannel>, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportChannelFactorySettings, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings, Microsoft.ServiceBus.Channels.ITransportFactorySettings, IDefaultCommunicationTimeouts, Microsoft.ServiceBus.Channels.IConnectionOrientedConnectionSettings
	{
		private int connectionBufferSize;

		private Microsoft.ServiceBus.Channels.IConnectionInitiator connectionInitiator;

		private Microsoft.ServiceBus.Channels.ConnectionPool connectionPool;

		private string connectionPoolGroupName;

		private bool exposeConnectionProperty;

		private TimeSpan idleTimeout;

		private int maxBufferSize;

		private int maxOutboundConnectionsPerEndpoint;

		private TimeSpan maxOutputDelay;

		private System.ServiceModel.TransferMode transferMode;

		private ISecurityCapabilities securityCapabilities;

		private StreamUpgradeProvider upgrade;

		public int ConnectionBufferSize
		{
			get
			{
				return this.connectionBufferSize;
			}
		}

		internal Microsoft.ServiceBus.Channels.IConnectionInitiator ConnectionInitiator
		{
			get
			{
				if (this.connectionInitiator == null)
				{
					lock (base.ThisLock)
					{
						if (this.connectionInitiator == null)
						{
							this.connectionInitiator = this.GetConnectionInitiator();
						}
					}
				}
				return this.connectionInitiator;
			}
		}

		public string ConnectionPoolGroupName
		{
			get
			{
				return this.connectionPoolGroupName;
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

		public int MaxOutboundConnectionsPerEndpoint
		{
			get
			{
				return this.maxOutboundConnectionsPerEndpoint;
			}
		}

		public TimeSpan MaxOutputDelay
		{
			get
			{
				return this.maxOutputDelay;
			}
		}

		ServiceSecurityAuditBehavior Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings.AuditBehavior
		{
			get
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(Microsoft.ServiceBus.SR.GetString(Resources.SecurityAuditNotSupportedOnChannelFactory, new object[0])));
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
				StreamUpgradeProvider streamUpgradeProvider = this.upgrade;
				base.ThrowIfDisposed();
				return streamUpgradeProvider;
			}
		}

		internal ConnectionOrientedTransportChannelFactory(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportBindingElement bindingElement, BindingContext context, string connectionPoolGroupName, TimeSpan idleTimeout, int maxOutboundConnectionsPerEndpoint) : base(bindingElement, context)
		{
			if (bindingElement.TransferMode == System.ServiceModel.TransferMode.Buffered && bindingElement.MaxReceivedMessageSize > (long)2147483647)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("bindingElement.MaxReceivedMessageSize", Microsoft.ServiceBus.SR.GetString(Resources.MaxReceivedMessageSizeMustBeInIntegerRange, new object[0])));
			}
			this.connectionBufferSize = bindingElement.ConnectionBufferSize;
			this.connectionPoolGroupName = connectionPoolGroupName;
			this.exposeConnectionProperty = bindingElement.ExposeConnectionProperty;
			this.idleTimeout = idleTimeout;
			this.maxBufferSize = bindingElement.MaxBufferSize;
			this.maxOutboundConnectionsPerEndpoint = maxOutboundConnectionsPerEndpoint;
			this.maxOutputDelay = bindingElement.MaxOutputDelay;
			this.transferMode = bindingElement.TransferMode;
			Collection<StreamUpgradeBindingElement> streamUpgradeBindingElements = context.BindingParameters.FindAll<StreamUpgradeBindingElement>();
			if (streamUpgradeBindingElements.Count > 1)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.MultipleStreamUpgradeProvidersInParameters, new object[0])));
			}
			if (streamUpgradeBindingElements.Count == 1)
			{
				this.upgrade = streamUpgradeBindingElements[0].BuildClientStreamUpgradeProvider(context);
				context.BindingParameters.Remove<StreamUpgradeBindingElement>();
				this.securityCapabilities = streamUpgradeBindingElements[0].GetProperty<ISecurityCapabilities>(context);
			}
		}

		internal abstract Microsoft.ServiceBus.Channels.IConnectionInitiator GetConnectionInitiator();

		internal abstract Microsoft.ServiceBus.Channels.ConnectionPool GetConnectionPool();

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) != typeof(ISecurityCapabilities))
			{
				return base.GetProperty<T>();
			}
			return (T)this.securityCapabilities;
		}

		private bool GetUpgradeAndConnectionPool(out StreamUpgradeProvider upgradeCopy, out Microsoft.ServiceBus.Channels.ConnectionPool poolCopy)
		{
			bool flag;
			if (this.upgrade != null || this.connectionPool != null)
			{
				lock (base.ThisLock)
				{
					if (this.upgrade != null || this.connectionPool != null)
					{
						upgradeCopy = this.upgrade;
						poolCopy = this.connectionPool;
						this.upgrade = null;
						this.connectionPool = null;
						flag = true;
					}
					else
					{
						upgradeCopy = null;
						poolCopy = null;
						return false;
					}
				}
				return flag;
			}
			upgradeCopy = null;
			poolCopy = null;
			return false;
		}

		protected override void OnAbort()
		{
			StreamUpgradeProvider streamUpgradeProvider;
			Microsoft.ServiceBus.Channels.ConnectionPool connectionPool;
			if (this.GetUpgradeAndConnectionPool(out streamUpgradeProvider, out connectionPool))
			{
				if (connectionPool != null)
				{
					this.ReleaseConnectionPool(connectionPool, TimeSpan.Zero);
				}
				if (streamUpgradeProvider != null)
				{
					streamUpgradeProvider.Abort();
				}
			}
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult(this, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.OpenAsyncResult(this.Upgrade, timeout, callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			StreamUpgradeProvider streamUpgradeProvider;
			Microsoft.ServiceBus.Channels.ConnectionPool connectionPool;
			if (this.GetUpgradeAndConnectionPool(out streamUpgradeProvider, out connectionPool))
			{
				TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
				if (connectionPool != null)
				{
					this.ReleaseConnectionPool(connectionPool, timeoutHelper.RemainingTime());
				}
				if (streamUpgradeProvider != null)
				{
					streamUpgradeProvider.Close(timeoutHelper.RemainingTime());
				}
			}
		}

		protected override TChannel OnCreateChannel(EndpointAddress address, Uri via)
		{
			base.ValidateScheme(via);
			if (this.TransferMode != System.ServiceModel.TransferMode.Buffered)
			{
				return (TChannel)(new Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel(this, this, address, via, this.ConnectionInitiator, this.connectionPool));
			}
			return (TChannel)(new Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel(this, this, address, via, this.ConnectionInitiator, this.connectionPool, this.exposeConnectionProperty));
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.OpenAsyncResult.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			StreamUpgradeProvider upgrade = this.Upgrade;
			if (upgrade != null)
			{
				upgrade.Open(timeout);
			}
		}

		protected override void OnOpening()
		{
			base.OnOpening();
			this.connectionPool = this.GetConnectionPool();
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(this.connectionPool != null, "ConnectionPool should always be found");
		}

		internal abstract void ReleaseConnectionPool(Microsoft.ServiceBus.Channels.ConnectionPool pool, TimeSpan timeout);

		private class CloseAsyncResult : AsyncResult
		{
			private Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel> parent;

			private Microsoft.ServiceBus.Channels.ConnectionPool connectionPool;

			private StreamUpgradeProvider upgradeProvider;

			private TimeoutHelper timeoutHelper;

			private static AsyncCallback onCloseComplete;

			private static Action<object> onReleaseConnectionPoolScheduled;

			static CloseAsyncResult()
			{
				Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult.onCloseComplete = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult.OnCloseComplete));
			}

			public CloseAsyncResult(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel> parent, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.parent = parent;
				this.timeoutHelper = new TimeoutHelper(timeout);
				this.parent.GetUpgradeAndConnectionPool(out this.upgradeProvider, out this.connectionPool);
				if (this.connectionPool != null)
				{
					if (Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult.onReleaseConnectionPoolScheduled == null)
					{
						Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult.onReleaseConnectionPoolScheduled = new Action<object>(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult.OnReleaseConnectionPoolScheduled);
					}
					IOThreadScheduler.ScheduleCallbackNoFlow(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult.onReleaseConnectionPoolScheduled, this);
				}
				else if (this.HandleReleaseConnectionPoolComplete())
				{
					base.Complete(true);
					return;
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult>(result);
			}

			private bool HandleReleaseConnectionPoolComplete()
			{
				if (this.upgradeProvider == null)
				{
					return true;
				}
				IAsyncResult asyncResult = this.upgradeProvider.BeginClose(this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult.onCloseComplete, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				this.upgradeProvider.EndClose(asyncResult);
				return true;
			}

			private static void OnCloseComplete(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					asyncState.upgradeProvider.EndClose(result);
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

			private bool OnReleaseConnectionPoolScheduled()
			{
				this.parent.ReleaseConnectionPool(this.connectionPool, this.timeoutHelper.RemainingTime());
				return this.HandleReleaseConnectionPoolComplete();
			}

			private static void OnReleaseConnectionPoolScheduled(object state)
			{
				bool flag;
				Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult closeAsyncResult = (Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.CloseAsyncResult)state;
				Exception exception = null;
				try
				{
					flag = closeAsyncResult.OnReleaseConnectionPoolScheduled();
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					flag = true;
					exception = exception1;
				}
				if (flag)
				{
					closeAsyncResult.Complete(false, exception);
				}
			}
		}

		private class OpenAsyncResult : AsyncResult
		{
			private ICommunicationObject communicationObject;

			private static AsyncCallback onOpenComplete;

			static OpenAsyncResult()
			{
				Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.OpenAsyncResult.onOpenComplete = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.OpenAsyncResult.OnOpenComplete));
			}

			public OpenAsyncResult(ICommunicationObject communicationObject, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.communicationObject = communicationObject;
				if (this.communicationObject == null)
				{
					base.Complete(true);
					return;
				}
				IAsyncResult asyncResult = this.communicationObject.BeginOpen(timeout, Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.OpenAsyncResult.onOpenComplete, this);
				if (asyncResult.CompletedSynchronously)
				{
					this.communicationObject.EndOpen(asyncResult);
					base.Complete(true);
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.OpenAsyncResult>(result);
			}

			private static void OnOpenComplete(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.OpenAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelFactory<TChannel>.OpenAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					asyncState.communicationObject.EndOpen(result);
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