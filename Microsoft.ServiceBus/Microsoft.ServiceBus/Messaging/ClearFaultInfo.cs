using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal sealed class ClearFaultInfo : FaultInjectionInfo
	{
		public ClearFaultInfo()
		{
		}
	}
}