using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	internal class ConfigurationlessServiceHost : ServiceHost
	{
		public ConfigurationlessServiceHost(object singletonInstance, params Uri[] addresses) : base(singletonInstance, addresses)
		{
		}

		protected override void ApplyConfiguration()
		{
		}
	}
}