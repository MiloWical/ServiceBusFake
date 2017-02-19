using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class SleepInfo : FaultInjectionInfo
	{
		[DataMember]
		public int FireOnceInEveryXInvocation
		{
			get;
			set;
		}

		[DataMember]
		public System.TimeSpan TimeSpan
		{
			get;
			set;
		}

		public SleepInfo()
		{
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] target = new object[] { base.Target, this.TimeSpan, base.FireOnce, this.FireOnceInEveryXInvocation, base.ValidAfter, base.ValidBefore };
			return string.Format(invariantCulture, "FaultType: SleepInfo, Target: {0}, SleepDuration: {1} , FireOnce: {2} FireOnceInEveryXInvocation: {3}, ValidAfter: {4}, ValidBefore: {5}", target);
		}
	}
}