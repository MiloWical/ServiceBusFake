using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ComponentModel;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace Microsoft.ServiceBus.Configuration
{
	public class MessageSecurityOverRelayHttpElement : ConfigurationElement
	{
		[ConfigurationProperty("algorithmSuite", DefaultValue="Default")]
		[TypeConverter(typeof(SecurityAlgorithmSuiteConverter))]
		public SecurityAlgorithmSuite AlgorithmSuite
		{
			get
			{
				return (SecurityAlgorithmSuite)base["algorithmSuite"];
			}
			set
			{
				base["algorithmSuite"] = value;
			}
		}

		[ConfigurationProperty("clientCredentialType", DefaultValue=MessageCredentialType.Windows)]
		[ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.MessageCredentialTypeHelper))]
		public MessageCredentialType ClientCredentialType
		{
			get
			{
				return (MessageCredentialType)base["clientCredentialType"];
			}
			set
			{
				base["clientCredentialType"] = value;
			}
		}

		[ConfigurationProperty("negotiateServiceCredential", DefaultValue=true)]
		public bool NegotiateServiceCredential
		{
			get
			{
				return (bool)base["negotiateServiceCredential"];
			}
			set
			{
				base["negotiateServiceCredential"] = value;
			}
		}

		internal MessageSecurityOverRelayHttpElement()
		{
		}

		internal void ApplyConfiguration(MessageSecurityOverRelayHttp security)
		{
			if (security == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			security.ClientCredentialType = this.ClientCredentialType;
			security.NegotiateServiceCredential = this.NegotiateServiceCredential;
			if (base.ElementInformation.Properties["algorithmSuite"].ValueOrigin != PropertyValueOrigin.Default)
			{
				security.AlgorithmSuite = this.AlgorithmSuite;
			}
		}

		internal void InitializeFrom(MessageSecurityOverRelayHttp security)
		{
			if (security == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			this.ClientCredentialType = security.ClientCredentialType;
			this.NegotiateServiceCredential = security.NegotiateServiceCredential;
			if (security.WasAlgorithmSuiteSet)
			{
				this.AlgorithmSuite = security.AlgorithmSuite;
			}
		}
	}
}