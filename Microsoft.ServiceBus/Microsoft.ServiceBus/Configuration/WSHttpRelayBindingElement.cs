using Microsoft.ServiceBus;
using System;
using System.Configuration;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Configuration
{
	public abstract class WSHttpRelayBindingElement : WSHttpRelayBindingBaseElement
	{
		private ConfigurationPropertyCollection properties;

		[ConfigurationProperty("allowCookies", DefaultValue=false)]
		public bool AllowCookies
		{
			get
			{
				return (bool)base["allowCookies"];
			}
			set
			{
				base["allowCookies"] = value;
			}
		}

		protected override Type BindingElementType
		{
			get
			{
				return typeof(WSHttpRelayBinding);
			}
		}

		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				if (this.properties == null)
				{
					ConfigurationPropertyCollection properties = base.Properties;
					properties.Add(new ConfigurationProperty("allowCookies", typeof(bool), false, null, null, ConfigurationPropertyOptions.None));
					properties.Add(new ConfigurationProperty("security", typeof(WSHttpRelaySecurityElement), null, null, null, ConfigurationPropertyOptions.None));
					this.properties = properties;
				}
				return this.properties;
			}
		}

		[ConfigurationProperty("security")]
		public WSHttpRelaySecurityElement Security
		{
			get
			{
				return (WSHttpRelaySecurityElement)base["security"];
			}
		}

		internal WSHttpRelayBindingElement(string name) : base(name)
		{
		}

		protected override void InitializeFrom(Binding binding)
		{
			base.InitializeFrom(binding);
			WSHttpRelayBinding wSHttpRelayBinding = (WSHttpRelayBinding)binding;
			this.AllowCookies = wSHttpRelayBinding.AllowCookies;
			this.Security.InitializeFrom(wSHttpRelayBinding.Security);
		}

		protected override void OnApplyConfiguration(Binding binding)
		{
			base.OnApplyConfiguration(binding);
			WSHttpRelayBinding allowCookies = (WSHttpRelayBinding)binding;
			allowCookies.AllowCookies = this.AllowCookies;
			this.Security.ApplyConfiguration(allowCookies.Security);
		}
	}
}