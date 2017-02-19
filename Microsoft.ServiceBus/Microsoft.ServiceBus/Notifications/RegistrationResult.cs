using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="RegistrationResult", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class RegistrationResult
	{
		[DataMember(Name="ApplicationPlatform", IsRequired=true, Order=1001, EmitDefaultValue=true)]
		public string ApplicationPlatform
		{
			get;
			set;
		}

		[DataMember(Name="Outcome", Order=1004, EmitDefaultValue=true)]
		public string Outcome
		{
			get;
			set;
		}

		[DataMember(Name="PnsHandle", IsRequired=true, Order=1002, EmitDefaultValue=true)]
		public string PnsHandle
		{
			get;
			set;
		}

		[DataMember(Name="RegistrationId", IsRequired=true, Order=1003, EmitDefaultValue=true)]
		public string RegistrationId
		{
			get;
			set;
		}

		public RegistrationResult()
		{
		}
	}
}