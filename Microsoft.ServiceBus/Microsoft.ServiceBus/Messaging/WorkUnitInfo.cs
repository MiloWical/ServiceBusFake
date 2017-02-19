using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="WorkUnitInfo", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class WorkUnitInfo : IExtensibleDataObject
	{
		[DataMember(Name="Identifier", EmitDefaultValue=false)]
		private string identifier;

		[DataMember(Name="SequenceNumber", IsRequired=true)]
		private int sequenceNumber;

		public ExtensionDataObject ExtensionData
		{
			get;
			set;
		}

		public string Identifier
		{
			get
			{
				return this.identifier;
			}
			set
			{
				this.identifier = value;
			}
		}

		public bool More
		{
			get;
			set;
		}

		public int SequenceNumber
		{
			get
			{
				return this.sequenceNumber;
			}
			set
			{
				this.sequenceNumber = value;
			}
		}

		public WorkUnitInfo()
		{
		}

		public static void AddTo(MessageHeaders messageHeaders, string workUnitName, string workUnitId, int sequenceNumber)
		{
			WorkUnitInfo workUnitInfo = new WorkUnitInfo()
			{
				Identifier = workUnitId,
				SequenceNumber = sequenceNumber
			};
			messageHeaders.Add(MessageHeader.CreateHeader(workUnitName, "http://schemas.microsoft.com/netservices/2011/06/servicebus", workUnitInfo));
		}

		public static WorkUnitInfo GetFrom(MessageHeaders messageHeaders, string workUnitName)
		{
			int num = messageHeaders.FindHeader(workUnitName, "http://schemas.microsoft.com/netservices/2011/06/servicebus");
			if (num < 0)
			{
				return null;
			}
			return messageHeaders.GetHeader<WorkUnitInfo>(num);
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { this.identifier, this.sequenceNumber };
			return string.Format(invariantCulture, "{0}:{1}", objArray);
		}
	}
}