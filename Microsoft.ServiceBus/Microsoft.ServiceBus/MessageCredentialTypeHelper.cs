using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	internal static class MessageCredentialTypeHelper
	{
		internal static bool IsDefined(MessageCredentialType value)
		{
			if (value == MessageCredentialType.None || value == MessageCredentialType.UserName || value == MessageCredentialType.Windows || value == MessageCredentialType.Certificate)
			{
				return true;
			}
			return value == MessageCredentialType.IssuedToken;
		}
	}
}