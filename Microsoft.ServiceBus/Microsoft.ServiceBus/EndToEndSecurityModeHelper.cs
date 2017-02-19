using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	internal static class EndToEndSecurityModeHelper
	{
		internal static bool IsDefined(EndToEndSecurityMode value)
		{
			if (value == EndToEndSecurityMode.None || value == EndToEndSecurityMode.Transport || value == EndToEndSecurityMode.TransportWithMessageCredential)
			{
				return true;
			}
			return value == EndToEndSecurityMode.Message;
		}

		internal static EndToEndSecurityMode ToRelaySecurityMode(Microsoft.ServiceBus.UnifiedSecurityMode value)
		{
			Microsoft.ServiceBus.UnifiedSecurityMode unifiedSecurityMode = value;
			if (unifiedSecurityMode > Microsoft.ServiceBus.UnifiedSecurityMode.Transport)
			{
				if (unifiedSecurityMode == Microsoft.ServiceBus.UnifiedSecurityMode.Message)
				{
					return EndToEndSecurityMode.Message;
				}
				if (unifiedSecurityMode == Microsoft.ServiceBus.UnifiedSecurityMode.TransportWithMessageCredential)
				{
					return EndToEndSecurityMode.TransportWithMessageCredential;
				}
			}
			else if (unifiedSecurityMode != Microsoft.ServiceBus.UnifiedSecurityMode.None)
			{
				if (unifiedSecurityMode == Microsoft.ServiceBus.UnifiedSecurityMode.Transport)
				{
					return EndToEndSecurityMode.Transport;
				}
			}
			return EndToEndSecurityMode.None;
		}

		internal static SecurityMode ToSecurityMode(EndToEndSecurityMode value)
		{
			switch (value)
			{
				case EndToEndSecurityMode.None:
				{
					return SecurityMode.None;
				}
				case EndToEndSecurityMode.Transport:
				{
					return SecurityMode.Transport;
				}
				case EndToEndSecurityMode.Message:
				{
					return SecurityMode.Message;
				}
				case EndToEndSecurityMode.TransportWithMessageCredential:
				{
					return SecurityMode.TransportWithMessageCredential;
				}
				default:
				{
					return SecurityMode.None;
				}
			}
		}
	}
}