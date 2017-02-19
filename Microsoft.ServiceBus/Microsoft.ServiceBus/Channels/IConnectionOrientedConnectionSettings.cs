using System;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IConnectionOrientedConnectionSettings
	{
		int ConnectionBufferSize
		{
			get;
		}

		TimeSpan IdleTimeout
		{
			get;
		}

		TimeSpan MaxOutputDelay
		{
			get;
		}
	}
}