using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="PortalRedirect", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class PortalManagementAddress
	{
		[DataMember(Name="Address")]
		public Uri Address
		{
			get;
			set;
		}

		public PortalManagementAddress()
		{
		}
	}
}