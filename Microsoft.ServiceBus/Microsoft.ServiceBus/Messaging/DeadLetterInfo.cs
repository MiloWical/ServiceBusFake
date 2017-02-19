using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract]
	internal class DeadLetterInfo
	{
		private string deadLetterReason;

		private string deadLetterErrorDescription;

		[DataMember(Name="deadLetterErrorDescription", EmitDefaultValue=true, IsRequired=false, Order=65538)]
		public string DeadLetterErrorDescription
		{
			get
			{
				return this.deadLetterErrorDescription;
			}
			set
			{
				this.deadLetterErrorDescription = value;
			}
		}

		[DataMember(Name="deadLetterReason", EmitDefaultValue=true, IsRequired=false, Order=65537)]
		public string DeadLetterReason
		{
			get
			{
				return this.deadLetterReason;
			}
			set
			{
				this.deadLetterReason = value;
			}
		}

		public ExtensionDataObject ExtensionData
		{
			get;
			set;
		}

		public DeadLetterInfo(string deadLetterReason, string deadLetterErrorDescription)
		{
			this.DeadLetterReason = deadLetterReason;
			this.DeadLetterErrorDescription = deadLetterErrorDescription;
		}
	}
}