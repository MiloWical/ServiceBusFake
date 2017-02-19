using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications.Descriptions
{
	[DataContract(Name="RegistrationSecretPayload")]
	internal class RegistrationSecretPayload
	{
		[AmqpMember(Order=3, Mandatory=true)]
		[DataMember(Name="WANHDeviceChallenge", Order=1003, IsRequired=true)]
		public string WANHDeviceChallenge
		{
			get;
			set;
		}

		[AmqpMember(Order=2, Mandatory=true)]
		[DataMember(Name="WANHExpirationTime", Order=1002, IsRequired=true)]
		public string WANHExpirationTime
		{
			get;
			internal set;
		}

		[AmqpMember(Order=1, Mandatory=true)]
		[DataMember(Name="WANHRegistrationSecret", Order=1001, IsRequired=true)]
		public string WANHRegistrationSecret
		{
			get;
			internal set;
		}

		public RegistrationSecretPayload()
		{
		}
	}
}