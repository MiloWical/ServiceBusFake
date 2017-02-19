using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal abstract class SocketConnectionChannelListener : Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelListener
	{
		private bool teredoEnabled;

		private int listenBacklog;

		private IConnectionElement connectionElement;

		private ISecurityCapabilities securityCapabilities;

		public IConnectionElement ConnectionElement
		{
			get
			{
				return this.connectionElement;
			}
		}

		public int ListenBacklog
		{
			get
			{
				return this.listenBacklog;
			}
		}

		internal override TraceCode MessageReceivedTraceCode
		{
			get
			{
				return TraceCode.TcpChannelMessageReceived;
			}
		}

		internal override TraceCode MessageReceiveFailedTraceCode
		{
			get
			{
				return TraceCode.TcpChannelMessageReceiveFailed;
			}
		}

		public override string Scheme
		{
			get
			{
				return "sb";
			}
		}

		public bool TeredoEnabled
		{
			get
			{
				return this.teredoEnabled;
			}
		}

		internal override Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> TransportManagerTable
		{
			get
			{
				return this.connectionElement.TransportManagerTable;
			}
		}

		protected SocketConnectionChannelListener(SocketConnectionBindingElement bindingElement, BindingContext context) : base(bindingElement, context)
		{
			this.listenBacklog = bindingElement.ListenBacklog;
			this.teredoEnabled = bindingElement.TeredoEnabled;
			base.SetIdleTimeout(bindingElement.ConnectionPoolSettings.IdleTimeout);
			base.SetMaxPooledConnections(bindingElement.ConnectionPoolSettings.MaxOutboundConnectionsPerEndpoint);
			this.connectionElement = bindingElement.ConnectionElement;
			this.securityCapabilities = bindingElement.GetProperty<ISecurityCapabilities>(context);
		}

		internal override Microsoft.ServiceBus.Channels.ITransportManagerRegistration CreateTransportManagerRegistration()
		{
			return this.CreateTransportManagerRegistration(base.BaseUri);
		}

		internal override Microsoft.ServiceBus.Channels.ITransportManagerRegistration CreateTransportManagerRegistration(System.Uri listenUri)
		{
			return new SocketConnectionTransportManagerRegistration(listenUri, this);
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) == typeof(ISecurityCapabilities))
			{
				return (T)this.securityCapabilities;
			}
			if (typeof(T) != typeof(IConnectionStatus))
			{
				return base.GetProperty<T>();
			}
			return this.connectionElement.GetProperty<T>();
		}
	}
}