using System;

namespace Microsoft.ServiceBus
{
	internal static class EndToEndWebHttpSecurityModeHelper
	{
		internal static bool IsDefined(EndToEndWebHttpSecurityMode value)
		{
			if (value == EndToEndWebHttpSecurityMode.None)
			{
				return true;
			}
			return value == EndToEndWebHttpSecurityMode.Transport;
		}
	}
}