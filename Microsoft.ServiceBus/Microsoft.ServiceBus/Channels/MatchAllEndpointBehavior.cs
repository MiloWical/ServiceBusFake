using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Microsoft.ServiceBus.Channels
{
	internal class MatchAllEndpointBehavior : IEndpointBehavior
	{
		public MatchAllEndpointBehavior()
		{
		}

		void System.ServiceModel.Description.IEndpointBehavior.AddBindingParameters(ServiceEndpoint serviceEndpoint, BindingParameterCollection bindingParameters)
		{
		}

		void System.ServiceModel.Description.IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint serviceEndpoint, ClientRuntime behavior)
		{
		}

		void System.ServiceModel.Description.IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint serviceEndpoint, EndpointDispatcher endpointDispatcher)
		{
			if (endpointDispatcher == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("endpointDispatcher");
			}
			endpointDispatcher.AddressFilter = new MatchAllMessageFilter();
		}

		void System.ServiceModel.Description.IEndpointBehavior.Validate(ServiceEndpoint serviceEndpoint)
		{
		}
	}
}