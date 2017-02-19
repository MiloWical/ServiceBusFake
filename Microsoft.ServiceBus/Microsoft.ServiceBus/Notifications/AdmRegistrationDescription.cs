using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[AmqpContract(Code=11L)]
	[DataContract(Name="AdmRegistrationDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class AdmRegistrationDescription : RegistrationDescription
	{
		[AmqpMember(Mandatory=false, Order=3)]
		[DataMember(Name="AdmRegistrationId", Order=2001, IsRequired=true)]
		public string AdmRegistrationId
		{
			get;
			set;
		}

		internal override string AppPlatForm
		{
			get
			{
				return "adm";
			}
		}

		internal override string PlatformType
		{
			get
			{
				return "adm";
			}
		}

		internal override string RegistrationType
		{
			get
			{
				return "adm";
			}
		}

		public AdmRegistrationDescription(string admRegistrationId) : this(string.Empty, admRegistrationId, null)
		{
		}

		public AdmRegistrationDescription(string admRegistrationId, IEnumerable<string> tags) : this(string.Empty, admRegistrationId, tags)
		{
		}

		public AdmRegistrationDescription(AdmRegistrationDescription sourceRegistration) : base(sourceRegistration)
		{
			this.AdmRegistrationId = sourceRegistration.AdmRegistrationId;
		}

		internal AdmRegistrationDescription(string notificationHubPath, string admRegistrationId, IEnumerable<string> tags) : base(notificationHubPath)
		{
			if (admRegistrationId == null)
			{
				throw new ArgumentNullException("admRegistrationId");
			}
			this.AdmRegistrationId = admRegistrationId;
			if (tags != null)
			{
				base.Tags = new HashSet<string>(tags);
			}
		}

		internal override RegistrationDescription Clone()
		{
			return new AdmRegistrationDescription(this);
		}

		internal override string GetPnsHandle()
		{
			return this.AdmRegistrationId;
		}

		internal override void OnValidate(bool allowLocalMockPns, ApiVersion version)
		{
			if (string.IsNullOrWhiteSpace(this.AdmRegistrationId))
			{
				throw new InvalidDataContractException(SRClient.AdmRegistrationIdInvalid);
			}
		}

		internal override void SetPnsHandle(string pnsHandle)
		{
			this.AdmRegistrationId = pnsHandle;
		}
	}
}