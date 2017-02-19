using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="ScheduleMessageResponse", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class ScheduleMessageResponseCommand : IExtensibleDataObject
	{
		[DataMember(Name="SequenceNumbers", EmitDefaultValue=true, IsRequired=true, Order=65537)]
		private IEnumerable<long> sequenceNumbers;

		private ExtensionDataObject extensionData;

		public ExtensionDataObject ExtensionData
		{
			get
			{
				return this.extensionData;
			}
			set
			{
				this.extensionData = value;
			}
		}

		public IEnumerable<long> SequenceNumbers
		{
			get
			{
				return this.sequenceNumbers;
			}
			set
			{
				this.sequenceNumbers = value;
			}
		}

		public ScheduleMessageResponseCommand()
		{
		}
	}
}