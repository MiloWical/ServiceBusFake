using System;
using System.Net;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus
{
	internal static class IPAddressExtensionMethods
	{
		public static long GetRawIPv4Address(this IPAddress address)
		{
			return address.Address;
		}
	}
}