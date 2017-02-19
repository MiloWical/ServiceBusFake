using Microsoft.ServiceBus;
using System;
using System.ServiceModel.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	public class ConnectionStatusElement : BehaviorExtensionElement
	{
		public override Type BehaviorType
		{
			get
			{
				return typeof(ConnectionStatusBehavior);
			}
		}

		public ConnectionStatusElement()
		{
		}

		protected override object CreateBehavior()
		{
			return new ConnectionStatusBehavior();
		}
	}
}