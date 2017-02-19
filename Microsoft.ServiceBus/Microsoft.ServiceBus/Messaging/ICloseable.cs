using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface ICloseable
	{
		bool IsClosedOrClosing
		{
			get;
		}
	}
}