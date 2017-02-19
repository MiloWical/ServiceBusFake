using Microsoft.ServiceBus;
using System;
using System.Configuration;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class TransportClientEndpointBehaviorElement : BehaviorExtensionElement
	{
		private ConfigurationPropertyCollection properties;

		public override Type BehaviorType
		{
			get
			{
				return typeof(TransportClientEndpointBehavior);
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					properties.Add(new ConfigurationProperty("tokenProvider", typeof(TokenProviderElement), null, null, null, ConfigurationPropertyOptions.None));
					this.properties = properties;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("tokenProvider")]
		public TokenProviderElement TokenProvider
		{
			get
			{
				return (TokenProviderElement)base["tokenProvider"];
			}
		}

		public TransportClientEndpointBehaviorElement()
		{
		}

		public override void CopyFrom(ServiceModelExtensionElement from)
		{
			base.CopyFrom(from);
			TransportClientEndpointBehaviorElement transportClientEndpointBehaviorElement = (TransportClientEndpointBehaviorElement)from;
			this.TokenProvider.CopyFrom(transportClientEndpointBehaviorElement.TokenProvider);
		}

		protected override object CreateBehavior()
		{
			TransportClientEndpointBehavior transportClientEndpointBehavior = new TransportClientEndpointBehavior();
			if (!this.TokenProvider.IsValid)
			{
				transportClientEndpointBehavior.CredentialType = TransportClientCredentialType.Unauthenticated;
			}
			else
			{
				transportClientEndpointBehavior.TokenProvider = this.TokenProvider.CreateTokenProvider();
				transportClientEndpointBehavior.CredentialType = TransportClientCredentialType.TokenProvider;
			}
			return transportClientEndpointBehavior;
		}
	}
}