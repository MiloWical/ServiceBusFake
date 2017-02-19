using Microsoft.ServiceBus.Channels;
using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class RelayedSocketElement : IConnectionElement, ISecureableConnectionElement
	{
		private static Dictionary<NameSettings, Binding> bindingTable;

		private static Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> transportManagerTable;

		private BindingContext context;

		private Binding innerBinding;

		private NameSettings nameSettings;

		private TokenProvider tokenProvider;

		private SocketSecurityRole socketSecurityMode;

		private RelayedSocketListener connectionListener;

		public Binding InnerBinding
		{
			get
			{
				return this.innerBinding;
			}
			set
			{
				this.innerBinding = value;
			}
		}

		public SocketSecurityRole SecurityMode
		{
			get
			{
				return this.socketSecurityMode;
			}
		}

		public Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> TransportManagerTable
		{
			get
			{
				return RelayedSocketElement.transportManagerTable;
			}
		}

		static RelayedSocketElement()
		{
			RelayedSocketElement.bindingTable = new Dictionary<NameSettings, Binding>();
			RelayedSocketElement.transportManagerTable = new Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration>(true);
		}

		public RelayedSocketElement(BindingContext context, NameSettings nameSettings, TokenProvider tokenProvider, SocketSecurityRole socketSecurityMode)
		{
			this.context = context;
			this.nameSettings = nameSettings;
			this.tokenProvider = tokenProvider;
			this.socketSecurityMode = socketSecurityMode;
			ConnectivitySettingsEndpointBehavior connectivitySettingsEndpointBehavior = null;
			ConnectivitySettings connectivitySetting = this.context.BindingParameters.Find<ConnectivitySettings>();
			HttpConnectivitySettings httpConnectivitySetting = this.context.BindingParameters.Find<HttpConnectivitySettings>();
			if (connectivitySetting != null || httpConnectivitySetting != null)
			{
				connectivitySettingsEndpointBehavior = new ConnectivitySettingsEndpointBehavior(connectivitySetting, httpConnectivitySetting);
			}
			this.connectionListener = new RelayedSocketListener(this.tokenProvider, this.nameSettings, this.socketSecurityMode, connectivitySettingsEndpointBehavior);
		}

		public Microsoft.ServiceBus.Channels.IConnectionInitiator CreateInitiator(int bufferSize)
		{
			return new RelayedSocketInitiator(bufferSize, this.tokenProvider, this.socketSecurityMode);
		}

		public Microsoft.ServiceBus.Channels.IConnectionListener CreateListener(int bufferSize, Uri uri)
		{
			Binding binding = this.innerBinding ?? this.GetBinding();
			this.connectionListener.Initialize(bufferSize, uri, binding);
			return this.connectionListener;
		}

		private Binding GetBinding()
		{
			Binding customBinding;
			Binding binding;
			lock (RelayedSocketElement.bindingTable)
			{
				if (!RelayedSocketElement.bindingTable.TryGetValue(this.nameSettings, out customBinding))
				{
					BindingElement[] bindingElementArray = new BindingElement[] { ClientMessageUtility.CreateInnerEncodingBindingElement(this.context), new RelayedOnewayTransportBindingElement(this.nameSettings.ServiceSettings.RelayClientAuthenticationType, RelayedOnewayConnectionMode.Unicast) };
					customBinding = new CustomBinding(bindingElementArray)
					{
						CloseTimeout = this.context.Binding.CloseTimeout,
						Name = this.context.Binding.Name,
						Namespace = this.context.Binding.Namespace,
						OpenTimeout = this.context.Binding.OpenTimeout,
						ReceiveTimeout = this.context.Binding.ReceiveTimeout,
						SendTimeout = this.context.Binding.SendTimeout
					};
					RelayedSocketElement.bindingTable.Add(this.nameSettings, customBinding);
				}
				binding = customBinding;
			}
			return binding;
		}

		public T GetProperty<T>()
		where T : class
		{
			if (typeof(T) != typeof(IConnectionStatus))
			{
				return default(T);
			}
			return (T)this.connectionListener.ConnectionStatus;
		}

		public bool IsCompatible(IConnectionElement element)
		{
			RelayedSocketElement relayedSocketElement = element as RelayedSocketElement;
			if (relayedSocketElement == null)
			{
				return false;
			}
			if (this.innerBinding == null && relayedSocketElement.InnerBinding == null)
			{
				return this.nameSettings.IsCompatible(relayedSocketElement.nameSettings);
			}
			return this.innerBinding == relayedSocketElement.InnerBinding;
		}
	}
}