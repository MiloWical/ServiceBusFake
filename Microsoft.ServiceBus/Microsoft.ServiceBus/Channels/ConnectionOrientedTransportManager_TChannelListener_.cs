using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class ConnectionOrientedTransportManager<TChannelListener> : Microsoft.ServiceBus.Channels.TransportManager
	where TChannelListener : Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelListener
	{
		private Microsoft.ServiceBus.Channels.UriPrefixTable<TChannelListener> addressTable;

		private int connectionBufferSize;

		private TimeSpan channelInitializationTimeout;

		private int maxPendingConnections;

		private TimeSpan maxOutputDelay;

		private int maxPendingAccepts;

		private TimeSpan idleTimeout;

		private int maxPooledConnections;

		private MessageReceivedCallback messageReceivedCallback;

		private Microsoft.ServiceBus.Channels.UriPrefixTable<TChannelListener> AddressTable
		{
			get
			{
				return this.addressTable;
			}
		}

		protected TimeSpan ChannelInitializationTimeout
		{
			get
			{
				return this.channelInitializationTimeout;
			}
		}

		internal int ConnectionBufferSize
		{
			get
			{
				return this.connectionBufferSize;
			}
		}

		internal TimeSpan IdleTimeout
		{
			get
			{
				return this.idleTimeout;
			}
		}

		internal TimeSpan MaxOutputDelay
		{
			get
			{
				return this.maxOutputDelay;
			}
		}

		internal int MaxPendingAccepts
		{
			get
			{
				return this.maxPendingAccepts;
			}
		}

		internal int MaxPendingConnections
		{
			get
			{
				return this.maxPendingConnections;
			}
		}

		internal int MaxPooledConnections
		{
			get
			{
				return this.maxPooledConnections;
			}
		}

		protected ConnectionOrientedTransportManager()
		{
			this.addressTable = new Microsoft.ServiceBus.Channels.UriPrefixTable<TChannelListener>();
		}

		internal void ApplyListenerSettings(Microsoft.ServiceBus.Channels.IConnectionOrientedListenerSettings listenerSettings)
		{
			this.connectionBufferSize = listenerSettings.ConnectionBufferSize;
			this.channelInitializationTimeout = listenerSettings.ChannelInitializationTimeout;
			this.maxPendingConnections = listenerSettings.MaxPendingConnections;
			this.maxOutputDelay = listenerSettings.MaxOutputDelay;
			this.maxPendingAccepts = listenerSettings.MaxPendingAccepts;
			this.idleTimeout = listenerSettings.IdleTimeout;
			this.maxPooledConnections = listenerSettings.MaxPooledConnections;
		}

		private TChannelListener GetChannelListener(Uri via)
		{
			TChannelListener tChannelListener = default(TChannelListener);
			if (this.AddressTable.TryLookupUri(via, HostNameComparisonMode.StrongWildcard, out tChannelListener))
			{
				return tChannelListener;
			}
			if (this.AddressTable.TryLookupUri(via, HostNameComparisonMode.Exact, out tChannelListener))
			{
				return tChannelListener;
			}
			this.AddressTable.TryLookupUri(via, HostNameComparisonMode.WeakWildcard, out tChannelListener);
			return tChannelListener;
		}

		internal bool IsCompatible(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelListener channelListener)
		{
			if (channelListener.InheritBaseAddressSettings)
			{
				return true;
			}
			if (!(this.ChannelInitializationTimeout == channelListener.ChannelInitializationTimeout) || this.ConnectionBufferSize != channelListener.ConnectionBufferSize || this.MaxPendingConnections != channelListener.MaxPendingConnections || !(this.MaxOutputDelay == channelListener.MaxOutputDelay) || this.MaxPendingAccepts != channelListener.MaxPendingAccepts || !(this.idleTimeout == channelListener.IdleTimeout))
			{
				return false;
			}
			return this.maxPooledConnections == channelListener.MaxPooledConnections;
		}

		internal void OnDemuxerError(Exception exception)
		{
			lock (base.ThisLock)
			{
				base.Fault<TChannelListener>(this.AddressTable, exception);
			}
		}

		internal Microsoft.ServiceBus.Channels.ISingletonChannelListener OnGetSingletonMessageHandler(Microsoft.ServiceBus.Channels.ServerSingletonPreambleConnectionReader serverSingletonPreambleReader)
		{
			Uri via = serverSingletonPreambleReader.Via;
			TChannelListener channelListener = this.GetChannelListener(via);
			if (channelListener == null)
			{
				serverSingletonPreambleReader.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/EndpointNotFound");
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string endpointNotFound = Resources.EndpointNotFound;
				object[] objArray = new object[] { via };
				throw exceptionUtility.ThrowHelperError(new EndpointNotFoundException(Microsoft.ServiceBus.SR.GetString(endpointNotFound, objArray)));
			}
			if (!((object)channelListener is IChannelListener<IReplyChannel>))
			{
				serverSingletonPreambleReader.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/UnsupportedMode");
				ExceptionUtility exceptionUtility1 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string framingModeNotSupported = Resources.FramingModeNotSupported;
				object[] objArray1 = new object[] { Microsoft.ServiceBus.Channels.FramingMode.Singleton };
				throw exceptionUtility1.ThrowHelperError(new ProtocolException(Microsoft.ServiceBus.SR.GetString(framingModeNotSupported, objArray1)));
			}
			channelListener.RaiseMessageReceived();
			return (Microsoft.ServiceBus.Channels.ISingletonChannelListener)(object)channelListener;
		}

		internal Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings OnGetTransportFactorySettings(Uri via)
		{
			return (object)this.GetChannelListener(via);
		}

		internal void OnHandleServerSessionPreamble(Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader serverSessionPreambleReader, Microsoft.ServiceBus.Channels.ConnectionDemuxer connectionDemuxer)
		{
			Uri via = serverSessionPreambleReader.Via;
			TChannelListener channelListener = this.GetChannelListener(via);
			if (channelListener == null)
			{
				serverSessionPreambleReader.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/EndpointNotFound");
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string duplexSessionListenerNotFound = Resources.DuplexSessionListenerNotFound;
				object[] str = new object[] { via.ToString() };
				throw exceptionUtility.ThrowHelperError(new EndpointNotFoundException(Microsoft.ServiceBus.SR.GetString(duplexSessionListenerNotFound, str)));
			}
			Microsoft.ServiceBus.Channels.ISessionPreambleHandler sessionPreambleHandler = (object)channelListener as Microsoft.ServiceBus.Channels.ISessionPreambleHandler;
			if (sessionPreambleHandler == null || !((object)channelListener is IChannelListener<IDuplexSessionChannel>))
			{
				serverSessionPreambleReader.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/UnsupportedMode");
				ExceptionUtility exceptionUtility1 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string framingModeNotSupported = Resources.FramingModeNotSupported;
				object[] objArray = new object[] { Microsoft.ServiceBus.Channels.FramingMode.Duplex };
				throw exceptionUtility1.ThrowHelperError(new ProtocolException(Microsoft.ServiceBus.SR.GetString(framingModeNotSupported, objArray)));
			}
			sessionPreambleHandler.HandleServerSessionPreamble(serverSessionPreambleReader, connectionDemuxer);
		}

		private void OnMessageReceived()
		{
			if (this.messageReceivedCallback != null)
			{
				this.messageReceivedCallback();
			}
		}

		internal override void Register(TimeSpan timeout, Microsoft.ServiceBus.Channels.TransportChannelListener channelListener)
		{
			this.AddressTable.RegisterUri(channelListener.Uri, channelListener.HostNameComparisonModeInternal, (TChannelListener)channelListener);
			channelListener.SetMessageReceivedCallback(new MessageReceivedCallback(this.OnMessageReceived));
		}

		internal void SetMessageReceivedCallback(MessageReceivedCallback messageReceivedCallback)
		{
			this.messageReceivedCallback = messageReceivedCallback;
		}

		internal override void Unregister(TimeSpan timeout, Microsoft.ServiceBus.Channels.TransportChannelListener channelListener)
		{
			Microsoft.ServiceBus.Channels.TransportManager.EnsureRegistered<TChannelListener>(this.AddressTable, (TChannelListener)channelListener);
			this.AddressTable.UnregisterUri(channelListener.Uri, channelListener.HostNameComparisonModeInternal);
			channelListener.SetMessageReceivedCallback(null);
		}
	}
}