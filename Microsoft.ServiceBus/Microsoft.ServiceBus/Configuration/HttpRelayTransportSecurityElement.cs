using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Configuration;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Configuration
{
	public sealed class HttpRelayTransportSecurityElement : ConfigurationElement
	{
		[ConfigurationProperty("proxyCredentialType", DefaultValue=HttpProxyCredentialType.None)]
		[ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.HttpProxyCredentialTypeHelper))]
		public HttpProxyCredentialType ProxyCredentialType
		{
			get
			{
				return (HttpProxyCredentialType)base["proxyCredentialType"];
			}
			set
			{
				base["proxyCredentialType"] = value;
			}
		}

		public HttpRelayTransportSecurityElement()
		{
		}

		internal void ApplyConfiguration(HttpRelayTransportSecurity security)
		{
			if (security == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			security.ProxyCredentialType = this.ProxyCredentialType;
		}

		internal void InitializeFrom(HttpRelayTransportSecurity security)
		{
			if (security == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			this.ProxyCredentialType = security.ProxyCredentialType;
		}
	}
}