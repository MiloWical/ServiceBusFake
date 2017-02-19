using Microsoft.ServiceBus;
using System;

namespace Microsoft.ServiceBus.Channels.Security
{
	internal interface IServiceBusSecuritySettings
	{
		Microsoft.ServiceBus.TokenProvider TokenProvider
		{
			get;
			set;
		}
	}
}