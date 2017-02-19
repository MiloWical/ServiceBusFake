using Microsoft.ServiceBus.Channels;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class HybridConnectionElement : IConnectionElement
	{
		private NameSettings nameSettings;

		private TokenProvider tokenProvider;

		private BindingContext context;

		private TcpRelayTransportBindingElement transportBindingElement;

		private static Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> transportManagerTable;

		public Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> TransportManagerTable
		{
			get
			{
				return HybridConnectionElement.transportManagerTable;
			}
		}

		static HybridConnectionElement()
		{
			HybridConnectionElement.transportManagerTable = new Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration>(true);
		}

		public HybridConnectionElement(BindingContext context, TcpRelayTransportBindingElement transportBindingElement, NameSettings nameSettings, TokenProvider tokenProvider)
		{
			this.context = context;
			this.nameSettings = nameSettings;
			this.tokenProvider = tokenProvider;
			this.transportBindingElement = transportBindingElement;
		}

		internal static Binding CreateDirectControlBindingElement(BindingContext context, TcpRelayTransportBindingElement transportBindingElement, IConnectionElement directDemuxSocketElement)
		{
			SocketConnectionBindingElement socketConnectionBindingElement = new SocketConnectionBindingElement(directDemuxSocketElement)
			{
				ChannelInitializationTimeout = transportBindingElement.ChannelInitializationTimeout,
				ConnectionBufferSize = transportBindingElement.ConnectionBufferSize
			};
			socketConnectionBindingElement.ConnectionPoolSettings.GroupName = transportBindingElement.ConnectionPoolSettings.GroupName;
			socketConnectionBindingElement.ConnectionPoolSettings.IdleTimeout = transportBindingElement.ConnectionPoolSettings.IdleTimeout;
			socketConnectionBindingElement.ConnectionPoolSettings.LeaseTimeout = transportBindingElement.ConnectionPoolSettings.LeaseTimeout;
			socketConnectionBindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint = transportBindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint;
			socketConnectionBindingElement.ListenBacklog = transportBindingElement.ListenBacklog;
			socketConnectionBindingElement.MaxOutputDelay = transportBindingElement.MaxOutputDelay;
			socketConnectionBindingElement.MaxPendingAccepts = transportBindingElement.MaxPendingAccepts;
			socketConnectionBindingElement.MaxPendingConnections = transportBindingElement.MaxPendingConnections;
			socketConnectionBindingElement.ManualAddressing = false;
			socketConnectionBindingElement.MaxBufferPoolSize = (long)1048576;
			socketConnectionBindingElement.MaxBufferSize = 65536;
			socketConnectionBindingElement.MaxReceivedMessageSize = (long)socketConnectionBindingElement.MaxBufferSize;
			socketConnectionBindingElement.TransferMode = TransferMode.Buffered;
			BindingElement[] bindingElementArray = new BindingElement[] { ClientMessageUtility.CreateInnerEncodingBindingElement(context), socketConnectionBindingElement };
			Binding customBinding = new CustomBinding(bindingElementArray)
			{
				CloseTimeout = context.Binding.CloseTimeout,
				OpenTimeout = context.Binding.OpenTimeout,
				ReceiveTimeout = context.Binding.ReceiveTimeout,
				SendTimeout = context.Binding.SendTimeout
			};
			return customBinding;
		}

		public Microsoft.ServiceBus.Channels.IConnectionInitiator CreateInitiator(int bufferSize)
		{
			return new HybridConnectionInitiator(this.context, this.transportBindingElement, bufferSize, this.tokenProvider, this.nameSettings.ServiceSettings.TransportProtection);
		}

		public Microsoft.ServiceBus.Channels.IConnectionListener CreateListener(int bufferSize, Uri listenUri)
		{
			return new HybridConnectionListener(this.context, this.transportBindingElement, bufferSize, listenUri, this.nameSettings, this.tokenProvider);
		}

		public T GetProperty<T>()
		where T : class
		{
			return default(T);
		}

		public bool IsCompatible(IConnectionElement element)
		{
			if (!(element is HybridConnectionElement))
			{
				return false;
			}
			return this.nameSettings.IsCompatible(((HybridConnectionElement)element).nameSettings);
		}
	}
}