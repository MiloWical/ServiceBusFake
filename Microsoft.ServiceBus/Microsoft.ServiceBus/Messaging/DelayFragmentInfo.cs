using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal sealed class DelayFragmentInfo : SleepInfo
	{
		public const short InvalidFragmentId = -1;

		[DataMember]
		public List<short> FragmentIds
		{
			get;
			set;
		}

		public DelayFragmentInfo()
		{
		}
	}
}