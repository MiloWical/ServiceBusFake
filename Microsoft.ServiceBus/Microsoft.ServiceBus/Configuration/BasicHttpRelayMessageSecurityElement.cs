using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ComponentModel;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace Microsoft.ServiceBus.Configuration
{
	public sealed class BasicHttpRelayMessageSecurityElement : ConfigurationElement
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

		[ConfigurationProperty("clientCredentialType", DefaultValue=BasicHttpMessageCredentialType.UserName)]
		public BasicHttpMessageCredentialType ClientCredentialType
		{
			get
			{
				return (BasicHttpMessageCredentialType)base["clientCredentialType"];
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
						new ConfigurationProperty("clientCredentialType", typeof(BasicHttpMessageCredentialType), (object)BasicHttpMessageCredentialType.UserName, null, null, ConfigurationPropertyOptions.None),
						new ConfigurationProperty("algorithmSuite", typeof(SecurityAlgorithmSuite), "Default", new SecurityAlgorithmSuiteConverter(), null, ConfigurationPropertyOptions.None)
					};
					this.properties = configurationPropertyCollections;
				}
				return this.properties;
			}
		}

		public BasicHttpRelayMessageSecurityElement()
		{
		}

		internal void ApplyConfiguration(BasicHttpRelayMessageSecurity security)
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

		internal void InitializeFrom(BasicHttpRelayMessageSecurity security)
		{
			if (security == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("security");
			}
			this.ClientCredentialType = security.ClientCredentialType;
			this.AlgorithmSuite = security.AlgorithmSuite;
		}
	}
}