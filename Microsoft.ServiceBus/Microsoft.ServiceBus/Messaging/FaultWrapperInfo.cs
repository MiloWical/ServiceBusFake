using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal sealed class FaultWrapperInfo : Microsoft.ServiceBus.Messaging.FaultInjectionInfo
	{
		[DataMember]
		public Microsoft.ServiceBus.Messaging.FaultInjectionInfo FaultInjectionInfo
		{
			get;
			set;
		}

		public FaultWrapperInfo()
		{
		}
	}
}