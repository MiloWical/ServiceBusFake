using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Microsoft.ServiceBus
{
	public sealed class TransportClientEndpointBehavior : IEndpointBehavior
	{
		private Microsoft.ServiceBus.TokenProvider tokenProvider;

		internal TransportClientCredentialType CredentialType
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.TokenProvider TokenProvider
		{
			get
			{
				if (this.CredentialType != TransportClientCredentialType.TokenProvider)
				{
					return null;
				}
				return this.tokenProvider;
			}
			set
			{
				this.UpdateCredentials(value);
			}
		}

		public TransportClientEndpointBehavior()
		{
			this.CredentialType = TransportClientCredentialType.Unauthenticated;
		}

		public TransportClientEndpointBehavior(Microsoft.ServiceBus.TokenProvider tokenProvider) : this()
		{
			this.TokenProvider = tokenProvider;
		}

		void System.ServiceModel.Description.IEndpointBehavior.AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
		{
			bindingParameters.Add(this);
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

		private void UpdateCredentials(Microsoft.ServiceBus.TokenProvider newTokenProvider)
		{
			if (newTokenProvider != null)
			{
				this.CredentialType = TransportClientCredentialType.TokenProvider;
				this.tokenProvider = newTokenProvider;
				return;
			}
			this.CredentialType = TransportClientCredentialType.Unauthenticated;
			this.tokenProvider = null;
		}
	}
}