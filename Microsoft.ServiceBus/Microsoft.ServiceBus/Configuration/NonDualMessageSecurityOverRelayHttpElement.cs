using Microsoft.ServiceBus;
using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public sealed class NonDualMessageSecurityOverRelayHttpElement : MessageSecurityOverRelayHttpElement
	{
		private ConfigurationPropertyCollection properties;

		[ConfigurationProperty("establishSecurityContext", DefaultValue=true)]
		public bool EstablishSecurityContext
		{
			get
			{
				return (bool)base["establishSecurityContext"];
			}
			set
			{
				base["establishSecurityContext"] = value;
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					properties.Add(new ConfigurationProperty("establishSecurityContext", typeof(bool), true, null, null, ConfigurationPropertyOptions.None));
					this.properties = properties;
				}
				return this.properties;
			}
		}

		public NonDualMessageSecurityOverRelayHttpElement()
		{
		}

		internal void ApplyConfiguration(NonDualMessageSecurityOverRelayHttp security)
		{
			base.ApplyConfiguration(security);
			security.EstablishSecurityContext = this.EstablishSecurityContext;
		}

		internal void InitializeFrom(NonDualMessageSecurityOverRelayHttp security)
		{
			base.InitializeFrom(security);
			this.EstablishSecurityContext = security.EstablishSecurityContext;
		}
	}
}