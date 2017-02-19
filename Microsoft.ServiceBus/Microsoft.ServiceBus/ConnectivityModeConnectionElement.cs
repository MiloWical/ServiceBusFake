using Microsoft.ServiceBus.Channels;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class ConnectivityModeConnectionElement : IConnectionElement, ISecureableConnectionElement
	{
		private readonly static Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> transportManagerTable;

		private readonly TokenProvider tokenProvider;

		private readonly ConnectivityModeCache cache;

		private readonly IConnectionElement connectionElement;

		public SocketSecurityRole SecurityMode
		{
			get
			{
				return JustDecompileGenerated_get_SecurityMode();
			}
			set
			{
				JustDecompileGenerated_set_SecurityMode(value);
			}
		}

		private SocketSecurityRole JustDecompileGenerated_SecurityMode_k__BackingField;

		public SocketSecurityRole JustDecompileGenerated_get_SecurityMode()
		{
			return this.JustDecompileGenerated_SecurityMode_k__BackingField;
		}

		private void JustDecompileGenerated_set_SecurityMode(SocketSecurityRole value)
		{
			this.JustDecompileGenerated_SecurityMode_k__BackingField = value;
		}

		public Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> TransportManagerTable
		{
			get
			{
				return ConnectivityModeConnectionElement.transportManagerTable;
			}
		}

		static ConnectivityModeConnectionElement()
		{
			ConnectivityModeConnectionElement.transportManagerTable = new Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration>(true);
		}

		public ConnectivityModeConnectionElement(TokenProvider tokenProvider, SocketSecurityRole socketSecurityMode, BindingContext context, NameSettings nameSettings, ConnectivitySettings connectivitySettings, HttpConnectivitySettings httpConnectivitySettings)
		{
			this.tokenProvider = tokenProvider;
			this.SecurityMode = socketSecurityMode;
			this.cache = new ConnectivityModeCache(connectivitySettings, httpConnectivitySettings);
			if (context == null || !(context.ListenUriBaseAddress != null))
			{
				return;
			}
			InternalConnectivityMode internalConnectivityMode = this.cache.GetInternalConnectivityMode(context.ListenUriBaseAddress);
			switch (internalConnectivityMode)
			{
				case InternalConnectivityMode.Tcp:
				{
					this.connectionElement = new RelayedSocketElement(context, nameSettings, this.tokenProvider, this.SecurityMode);
					return;
				}
				case InternalConnectivityMode.Http:
				{
					this.connectionElement = new WebStreamRelayedConnectionElement(this.tokenProvider, this.SecurityMode, context, nameSettings, false);
					return;
				}
				case InternalConnectivityMode.Https:
				{
					this.connectionElement = new WebStreamRelayedConnectionElement(this.tokenProvider, this.SecurityMode, context, nameSettings, true);
					return;
				}
				case InternalConnectivityMode.HttpsWebSocket:
				{
					this.connectionElement = new WebSocketRelayedConnectionElement(this.tokenProvider, this.SecurityMode, context, nameSettings);
					return;
				}
			}
			throw new InvalidOperationException(SRClient.UnsupportedConnectivityMode(internalConnectivityMode));
		}

		public Microsoft.ServiceBus.Channels.IConnectionInitiator CreateInitiator(int bufferSize)
		{
			return new ConnectivityModeConnectionInitiator(this.tokenProvider, this.SecurityMode, bufferSize, this.cache);
		}

		public Microsoft.ServiceBus.Channels.IConnectionListener CreateListener(int bufferSize, Uri uri)
		{
			if (this.connectionElement == null)
			{
				throw new ArgumentNullException("connectionElement");
			}
			return this.connectionElement.CreateListener(bufferSize, uri);
		}

		public T GetProperty<T>()
		where T : class
		{
			if (this.connectionElement != null)
			{
				return this.connectionElement.GetProperty<T>();
			}
			return default(T);
		}

		public bool IsCompatible(IConnectionElement element)
		{
			return element is ConnectivityModeConnectionElement;
		}
	}
}