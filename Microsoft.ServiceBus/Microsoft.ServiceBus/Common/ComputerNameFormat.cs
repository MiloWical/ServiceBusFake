using System;

namespace Microsoft.ServiceBus.Common
{
	internal enum ComputerNameFormat
	{
		NetBIOS,
		DnsHostName,
		Dns,
		DnsFullyQualified,
		PhysicalNetBIOS,
		PhysicalDnsHostName,
		PhysicalDnsDomain,
		PhysicalDnsFullyQualified
	}
}