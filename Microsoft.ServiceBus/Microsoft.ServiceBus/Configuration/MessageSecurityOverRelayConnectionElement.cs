using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ComponentModel;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace Microsoft.ServiceBus.Configuration
{
	public sealed class MessageSecurityOverRelayConnectionElement : ConfigurationElement
	{
		private ConfigurationPropertyCollection properties;

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

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection configurationPropertyCollections = new ConfigurationPropertyCollection()
					{
						new ConfigurationProperty("clientCredentialType", typeof(MessageCredentialType), (object)MessageCredentialType.Windows, null, new ServiceModelEnumValidator(typeof(Microsoft.ServiceBus.MessageCredentialTypeHelper)), ConfigurationPropertyOptions.None),
						new ConfigurationProperty("algorithmSuite", typeof(SecurityAlgorithmSuite), "Default", new SecurityAlgorithmSuiteConverter(), null, ConfigurationPropertyOptions.None)
					};
					this.properties = configurationPropertyCollections;
				}
				return this.properties;
			}
		}

		public MessageSecurityOverRelayConnectionElement()
		{
		}

		internal void ApplyConfiguration(MessageSecurityOverRelayConnection security)
		{
			if (security == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			security.ClientCredentialType = this.ClientCredentialType;
			if (base.ElementInformation.Properties["algorithmSuite"].ValueOrigin != PropertyValueOrigin.Default)
			{
				security.AlgorithmSuite = this.AlgorithmSuite;
			}
		}

		internal void InitializeFrom(MessageSecurityOverRelayConnection security)
		{
			if (security == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			this.ClientCredentialType = security.ClientCredentialType;
			if (security.WasAlgorithmSuiteSet)
			{
				this.AlgorithmSuite = security.AlgorithmSuite;
			}
		}
	}
}