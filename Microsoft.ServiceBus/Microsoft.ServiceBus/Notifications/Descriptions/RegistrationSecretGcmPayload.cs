using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications.Descriptions
{
	[DataContract(Name="RegistrationSecretGcmPayload")]
	internal class RegistrationSecretGcmPayload
	{
		[AmqpMember(Order=1, Mandatory=true)]
		[DataMember(Name="data", Order=1001, IsRequired=true)]
		public RegistrationSecretPayload Data
		{
			get;
			internal set;
		}

		public RegistrationSecretGcmPayload()
		{
		}
	}
}