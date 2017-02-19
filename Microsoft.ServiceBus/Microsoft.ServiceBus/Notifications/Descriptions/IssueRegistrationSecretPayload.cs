using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications.Descriptions
{
	[DataContract(Name="IssueRegistrationSecretPayload")]
	internal class IssueRegistrationSecretPayload
	{
		[AmqpMember(Order=2, Mandatory=true)]
		[DataMember(Name="ApplicationPlatform", Order=1002, IsRequired=true)]
		public string ApplicationPlatform
		{
			get;
			internal set;
		}

		[AmqpMember(Order=1, Mandatory=true)]
		[DataMember(Name="Channel", Order=1001, IsRequired=true)]
		public string Channel
		{
			get;
			internal set;
		}

		[AmqpMember(Order=3, Mandatory=true)]
		[DataMember(Name="DeviceChallenge", Order=1003, IsRequired=true)]
		public string DeviceChallenge
		{
			get;
			set;
		}

		public IssueRegistrationSecretPayload()
		{
		}
	}
}