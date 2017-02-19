using Microsoft.ServiceBus.Channels;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class WebStreamRelayedConnectionElement : IConnectionElement, ISecureableConnectionElement
	{
		private static Dictionary<NameSettings, Binding> bindingTable;

		private static Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> transportManagerTable;

		private BindingContext context;

		private Binding innerBinding;

		private NameSettings nameSettings;

		private TokenProvider tokenProvider;

		private WebStreamRelayedConnectionListener connectionListener;

		private bool useHttpsMode;

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
				return WebStreamRelayedConnectionElement.transportManagerTable;
			}
		}

		static WebStreamRelayedConnectionElement()
		{
			WebStreamRelayedConnectionElement.bindingTable = new Dictionary<NameSettings, Binding>();
			WebStreamRelayedConnectionElement.transportManagerTable = new Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration>(true);
		}

		public WebStreamRelayedConnectionElement(TokenProvider tokenProvider, SocketSecurityRole socketSecurityMode, BindingContext context, NameSettings nameSettings, bool useHttpsMode)
		{
			this.tokenProvider = tokenProvider;
			this.SecurityMode = socketSecurityMode;
			this.context = context;
			this.nameSettings = nameSettings;
			this.useHttpsMode = useHttpsMode;
			this.connectionListener = new WebStreamRelayedConnectionListener(this.tokenProvider, this.SecurityMode, this.nameSettings, context, useHttpsMode);
		}

		public Microsoft.ServiceBus.Channels.IConnectionInitiator CreateInitiator(int bufferSize)
		{
			return new WebStreamConnectionInitiator(this.tokenProvider, this.SecurityMode, bufferSize, this.useHttpsMode);
		}

		private Binding CreateInnerBinding()
		{
			Binding customBinding;
			Binding binding;
			lock (WebStreamRelayedConnectionElement.bindingTable)
			{
				if (!WebStreamRelayedConnectionElement.bindingTable.TryGetValue(this.nameSettings, out customBinding))
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
					WebStreamRelayedConnectionElement.bindingTable.Add(this.nameSettings, customBinding);
				}
				binding = customBinding;
			}
			return binding;
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
			return element is WebStreamRelayedConnectionElement;
		}
	}
}