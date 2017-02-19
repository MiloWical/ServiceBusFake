using Microsoft.ServiceBus.Channels;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class WebSocketRelayedConnectionElement : IConnectionElement, ISecureableConnectionElement
	{
		private readonly static Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> transportManagerTable;

		private readonly static Dictionary<NameSettings, Binding> bindingTable;

		private readonly BindingContext context;

		private readonly NameSettings nameSettings;

		private readonly TokenProvider tokenProvider;

		private readonly WebSocketRelayedConnectionListener connectionListener;

		private Binding innerBinding;

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

		public void JustDecompileGenerated_set_SecurityMode(SocketSecurityRole value)
		{
			this.JustDecompileGenerated_SecurityMode_k__BackingField = value;
		}

		public Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> TransportManagerTable
		{
			get
			{
				return WebSocketRelayedConnectionElement.transportManagerTable;
			}
		}

		static WebSocketRelayedConnectionElement()
		{
			WebSocketRelayedConnectionElement.transportManagerTable = new Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration>(true);
			WebSocketRelayedConnectionElement.bindingTable = new Dictionary<NameSettings, Binding>();
		}

		public WebSocketRelayedConnectionElement(TokenProvider tokenProvider, SocketSecurityRole socketSecurityMode, BindingContext context, NameSettings nameSettings)
		{
			this.tokenProvider = tokenProvider;
			this.SecurityMode = socketSecurityMode;
			this.context = context;
			this.nameSettings = nameSettings;
			this.connectionListener = new WebSocketRelayedConnectionListener(this.tokenProvider, this.SecurityMode, this.nameSettings, context);
		}

		public Microsoft.ServiceBus.Channels.IConnectionInitiator CreateInitiator(int bufferSize)
		{
			return new WebSocketConnectionInitiator(this.tokenProvider, bufferSize);
		}

		private Binding CreateInnerBinding()
		{
			Binding binding;
			Binding binding1;
			lock (WebSocketRelayedConnectionElement.bindingTable)
			{
				if (!WebSocketRelayedConnectionElement.bindingTable.TryGetValue(this.nameSettings, out binding))
				{
					BindingElement[] bindingElementArray = new BindingElement[] { ClientMessageUtility.CreateInnerEncodingBindingElement(this.context), new RelayedOnewayTransportBindingElement(this.nameSettings.ServiceSettings.RelayClientAuthenticationType, RelayedOnewayConnectionMode.Unicast) };
					CustomBinding customBinding = new CustomBinding(bindingElementArray)
					{
						CloseTimeout = this.context.Binding.CloseTimeout,
						Name = this.context.Binding.Name,
						Namespace = this.context.Binding.Namespace,
						OpenTimeout = this.context.Binding.OpenTimeout,
						ReceiveTimeout = this.context.Binding.ReceiveTimeout,
						SendTimeout = this.context.Binding.SendTimeout
					};
					binding = customBinding;
					WebSocketRelayedConnectionElement.bindingTable.Add(this.nameSettings, binding);
				}
				binding1 = binding;
			}
			return binding1;
		}

		public Microsoft.ServiceBus.Channels.IConnectionListener CreateListener(int bufferSize, Uri uri)
		{
			Binding binding = this.innerBinding ?? this.CreateInnerBinding();
			this.connectionListener.Initialize(uri, bufferSize, binding);
			return this.connectionListener;
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
			return element is WebSocketRelayedConnectionElement;
		}
	}
}