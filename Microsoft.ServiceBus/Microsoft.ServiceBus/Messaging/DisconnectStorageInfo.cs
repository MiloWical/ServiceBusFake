using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal sealed class DisconnectStorageInfo : FaultInjectionInfo
	{
		[DataMember]
		public int firingFrequencyInMilliseconds
		{
			get;
			set;
		}

		public DisconnectStorageInfo()
		{
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] target = new object[] { base.Target, base.FireOnce, this.firingFrequencyInMilliseconds, base.ValidAfter, base.ValidBefore };
			return string.Format(invariantCulture, "FaultType: DisconnectStorageInfo, Target: {0}, FireOnce: {1} FiringFrequencyInMilliseconds: {2}, ValidAfter: {3}, ValidBefore: {4}", target);
		}
	}
}