using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="UpdatePnsHandlePayload")]
	internal class UpdatePnsHandlePayload
	{
		[DataMember(Name="NewPnsHandle")]
		public string NewPnsHandle
		{
			get;
			set;
		}

		internal string NewPnsHandleHash
		{
			get
			{
				return RegistrationDescription.ComputeChannelHash(this.NewPnsHandle);
			}
		}

		[DataMember(Name="OriginalPnsHandle")]
		public string OriginalPnsHandle
		{
			get;
			set;
		}

		internal string OriginalPnsHandleHash
		{
			get
			{
				return RegistrationDescription.ComputeChannelHash(this.OriginalPnsHandle);
			}
		}

		public UpdatePnsHandlePayload()
		{
		}
	}
}