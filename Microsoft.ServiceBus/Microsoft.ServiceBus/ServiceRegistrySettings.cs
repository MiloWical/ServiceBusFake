using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Microsoft.ServiceBus
{
	public class ServiceRegistrySettings : IEndpointBehavior
	{
		private string displayName;

		private DiscoveryType discoveryType;

		protected bool transportProtectionEnabled;

		protected bool allowUnauthenticatedAccess;

		public DiscoveryType DiscoveryMode
		{
			get
			{
				return this.discoveryType;
			}
			set
			{
				this.discoveryType = value;
			}
		}

		public string DisplayName
		{
			get
			{
				return this.displayName;
			}
			set
			{
				this.displayName = value;
			}
		}

		internal bool? PreserveRawHttp
		{
			get;
			set;
		}

		public ServiceRegistrySettings()
		{
			this.discoveryType = DiscoveryType.Private;
		}

		public ServiceRegistrySettings(DiscoveryType discoveryType)
		{
			this.discoveryType = discoveryType;
		}

		void System.ServiceModel.Description.IEndpointBehavior.AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{
			NameSettings nameSetting = new NameSettings()
			{
				DisplayName = this.displayName
			};
			nameSetting.ServiceSettings.Discovery = this.discoveryType;
			nameSetting.ServiceSettings.RelayClientAuthenticationType = (this.allowUnauthenticatedAccess ? RelayClientAuthenticationType.None : RelayClientAuthenticationType.RelayAccessToken);
			nameSetting.ServiceSettings.TransportProtection = (this.transportProtectionEnabled ? RelayTransportProtectionMode.EndToEnd : RelayTransportProtectionMode.None);
			if (this.PreserveRawHttp.HasValue)
			{
				nameSetting.ServiceSettings.PreserveRawHttp = this.PreserveRawHttp.Value;
			}
			bindingParameters.Add(nameSetting);
		}

		void System.ServiceModel.Description.IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
		}

		void System.ServiceModel.Description.IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
		}

		void System.ServiceModel.Description.IEndpointBehavior.Validate(ServiceEndpoint endpoint)
		{
		}
	}
}