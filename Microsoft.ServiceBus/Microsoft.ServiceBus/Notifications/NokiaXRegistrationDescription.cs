using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[AmqpContract(Code=13L)]
	[DataContract(Name="NokiaXRegistrationDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal class NokiaXRegistrationDescription : RegistrationDescription
	{
		internal override string AppPlatForm
		{
			get
			{
				return "nokiax";
			}
		}

		[AmqpMember(Order=3, Mandatory=false)]
		[DataMember(Name="NokiaXRegistrationId", Order=2001, IsRequired=true)]
		public string NokiaXRegistrationId
		{
			get;
			set;
		}

		internal override string PlatformType
		{
			get
			{
				return "nokiax";
			}
		}

		internal override string RegistrationType
		{
			get
			{
				return "nokiax";
			}
		}

		public NokiaXRegistrationDescription(NokiaXRegistrationDescription sourceRegistration) : base(sourceRegistration)
		{
			this.NokiaXRegistrationId = sourceRegistration.NokiaXRegistrationId;
		}

		public NokiaXRegistrationDescription(string nokiaXRegistrationId, IEnumerable<string> tags) : this(string.Empty, nokiaXRegistrationId, tags)
		{
		}

		public NokiaXRegistrationDescription(string nokiaXRegistrationId) : this(string.Empty, nokiaXRegistrationId, null)
		{
		}

		internal NokiaXRegistrationDescription(string notificationHubPath, string nokiaXRegistrationId, IEnumerable<string> tags) : base(notificationHubPath)
		{
			if (string.IsNullOrWhiteSpace(nokiaXRegistrationId))
			{
				throw new ArgumentNullException("nokiaXRegistrationId");
			}
			this.NokiaXRegistrationId = nokiaXRegistrationId;
			if (tags != null)
			{
				base.Tags = new HashSet<string>(tags);
			}
		}

		internal override RegistrationDescription Clone()
		{
			return new NokiaXRegistrationDescription(this);
		}

		internal override string GetPnsHandle()
		{
			return this.NokiaXRegistrationId;
		}

		internal override void OnValidate(bool allowLocalMockPns, ApiVersion version)
		{
			if (string.IsNullOrWhiteSpace(this.NokiaXRegistrationId))
			{
				throw new InvalidDataContractException(SRClient.NokiaXRegistrationInvalidId);
			}
		}

		internal override void SetPnsHandle(string pnsHandle)
		{
			this.NokiaXRegistrationId = pnsHandle;
		}
	}
}