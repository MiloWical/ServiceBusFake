using System;

namespace Microsoft.ServiceBus
{
	internal static class EndToEndBasicHttpSecurityModeHelper
	{
		internal static bool IsDefined(EndToEndBasicHttpSecurityMode mode)
		{
			return true;
		}

		internal static EndToEndBasicHttpSecurityMode ToEndToEndBasicHttpSecurityMode(UnifiedSecurityMode value)
		{
			UnifiedSecurityMode unifiedSecurityMode = value;
			if (unifiedSecurityMode > UnifiedSecurityMode.Transport)
			{
				if (unifiedSecurityMode == UnifiedSecurityMode.Message)
				{
					return EndToEndBasicHttpSecurityMode.Message;
				}
				if (unifiedSecurityMode == UnifiedSecurityMode.TransportWithMessageCredential)
				{
					return EndToEndBasicHttpSecurityMode.TransportWithMessageCredential;
				}
			}
			else if (unifiedSecurityMode != UnifiedSecurityMode.None)
			{
				if (unifiedSecurityMode == UnifiedSecurityMode.Transport)
				{
					return EndToEndBasicHttpSecurityMode.Transport;
				}
			}
			return EndToEndBasicHttpSecurityMode.None;
		}
	}
}