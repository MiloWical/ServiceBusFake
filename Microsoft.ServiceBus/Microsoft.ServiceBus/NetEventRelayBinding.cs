using Microsoft.ServiceBus.Configuration;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Configuration;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus
{
	public class NetEventRelayBinding : NetOnewayRelayBinding, IBindingRuntimePreferences
	{
		public NetEventRelayBinding() : base(RelayedOnewayConnectionMode.Multicast, EndToEndSecurityMode.Transport, RelayClientAuthenticationType.RelayAccessToken)
		{
		}

		public NetEventRelayBinding(EndToEndSecurityMode securityMode, RelayEventSubscriberAuthenticationType relayClientAuthenticationType) : base(RelayedOnewayConnectionMode.Multicast, securityMode, (RelayClientAuthenticationType)relayClientAuthenticationType)
		{
		}

		public NetEventRelayBinding(string configurationName) : base(configurationName)
		{
			this.transport.ConnectionMode = RelayedOnewayConnectionMode.Multicast;
		}

		protected NetEventRelayBinding(RelayedOnewayTransportBindingElement transport, BinaryMessageEncodingBindingElement encoding, NetOnewayRelaySecurity security) : base(transport, encoding, security)
		{
			this.transport.ConnectionMode = RelayedOnewayConnectionMode.Multicast;
		}

		protected override void ApplyConfiguration(string configurationName)
		{
			NetEventRelayBindingElement item = NetEventRelayBindingCollectionElement.GetBindingCollectionElement().Bindings[configurationName];
			if (item == null)
			{
				ExceptionUtility exceptionUtility = DiagnosticUtility.ExceptionUtility;
				string configInvalidBindingConfigurationName = Resources.ConfigInvalidBindingConfigurationName;
				object[] objArray = new object[] { configurationName, "netEventRelayBinding" };
				throw exceptionUtility.ThrowHelperError(new ConfigurationErrorsException(Microsoft.ServiceBus.SR.GetString(configInvalidBindingConfigurationName, objArray)));
			}
			item.ApplyConfiguration(this);
		}

		protected bool IsBindingElementsMatch(RelayedOnewayTransportBindingElement transport, BinaryMessageEncodingBindingElement encoding, ReliableSessionBindingElement session)
		{
			return true;
		}
	}
}