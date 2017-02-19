using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="RegistrationCounts", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal sealed class RegistrationCounts
	{
		[DataMember(Name="AdmRegistrationsCount", IsRequired=false, Order=1006, EmitDefaultValue=false)]
		public long AdmRegistrationsCount
		{
			get;
			internal set;
		}

		[DataMember(Name="AllRegistrationsCount", IsRequired=true, Order=1001, EmitDefaultValue=true)]
		public long AllRegistrationsCount
		{
			get;
			internal set;
		}

		[DataMember(Name="AppleRegistrationsCount", IsRequired=false, Order=1003, EmitDefaultValue=false)]
		public long AppleRegistrationsCount
		{
			get;
			internal set;
		}

		[DataMember(Name="GcmRegistrationsCount", IsRequired=false, Order=1004, EmitDefaultValue=false)]
		public long GcmRegistrationsCount
		{
			get;
			internal set;
		}

		[DataMember(Name="MpnsRegistrationsCount", IsRequired=false, Order=1005, EmitDefaultValue=false)]
		public long MpnsRegistrationsCount
		{
			get;
			internal set;
		}

		[DataMember(Name="WindowsRegistrationsCount", IsRequired=false, Order=1002, EmitDefaultValue=false)]
		public long WindowsRegistrationsCount
		{
			get;
			internal set;
		}

		public RegistrationCounts()
		{
		}
	}
}