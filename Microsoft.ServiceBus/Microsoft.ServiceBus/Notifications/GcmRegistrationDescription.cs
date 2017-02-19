using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[AmqpContract(Code=1L)]
	[DataContract(Name="GcmRegistrationDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class GcmRegistrationDescription : RegistrationDescription
	{
		internal override string AppPlatForm
		{
			get
			{
				return "gcm";
			}
		}

		[AmqpMember(Order=3, Mandatory=false)]
		[DataMember(Name="GcmRegistrationId", Order=2001, IsRequired=true)]
		public string GcmRegistrationId
		{
			get;
			set;
		}

		internal override string PlatformType
		{
			get
			{
				return "gcm";
			}
		}

		internal override string RegistrationType
		{
			get
			{
				return "gcm";
			}
		}

		public GcmRegistrationDescription(GcmRegistrationDescription sourceRegistration) : base(sourceRegistration)
		{
			this.GcmRegistrationId = sourceRegistration.GcmRegistrationId;
		}

		public GcmRegistrationDescription(string gcmRegistrationId) : this(string.Empty, gcmRegistrationId, null)
		{
		}

		public GcmRegistrationDescription(string gcmRegistrationId, IEnumerable<string> tags) : this(string.Empty, gcmRegistrationId, tags)
		{
		}

		internal GcmRegistrationDescription(string notificationHubPath, string gcmRegistrationId, IEnumerable<string> tags) : base(notificationHubPath)
		{
			if (string.IsNullOrWhiteSpace(gcmRegistrationId))
			{
				throw new ArgumentNullException("gcmRegistrationId");
			}
			this.GcmRegistrationId = gcmRegistrationId;
			if (tags != null)
			{
				base.Tags = new HashSet<string>(tags);
			}
		}

		internal override RegistrationDescription Clone()
		{
			return new GcmRegistrationDescription(this);
		}

		internal override string GetPnsHandle()
		{
			return this.GcmRegistrationId;
		}

		internal override void OnValidate(bool allowLocalMockPns, ApiVersion version)
		{
			if (string.IsNullOrWhiteSpace(this.GcmRegistrationId))
			{
				throw new InvalidDataContractException(SRClient.GCMRegistrationInvalidId);
			}
		}

		internal override void SetPnsHandle(string pnsHandle)
		{
			this.GcmRegistrationId = pnsHandle;
		}
	}
}