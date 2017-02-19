using System;
using System.Collections.ObjectModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Microsoft.ServiceBus
{
	internal class ConnectivitySettingsEndpointBehavior : IEndpointBehavior
	{
		private ConnectivitySettings connectivitySettings;

		private HttpConnectivitySettings httpConnectivitySettings;

		public ConnectivitySettingsEndpointBehavior(ConnectivitySettings connectivitySettings, HttpConnectivitySettings httpConnectivitySettings)
		{
			this.connectivitySettings = connectivitySettings;
			this.httpConnectivitySettings = httpConnectivitySettings;
		}

		public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{
			if (this.connectivitySettings != null)
			{
				bindingParameters.Add(this.connectivitySettings);
			}
			if (this.httpConnectivitySettings != null)
			{
				bindingParameters.Add(this.httpConnectivitySettings);
			}
		}

		public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
		{
		}

		public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
		{
		}

		public void Validate(ServiceEndpoint endpoint)
		{
		}
	}
}