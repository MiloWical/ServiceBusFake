using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract]
	internal sealed class NotificationDetails
	{
		[DataMember(Name="EndTime", Order=1006, EmitDefaultValue=false)]
		public DateTime? EndTime
		{
			get;
			set;
		}

		[DataMember(Name="EnqueueTime", Order=1004, EmitDefaultValue=false)]
		public DateTime? EnqueueTime
		{
			get;
			set;
		}

		[DataMember(Name="Location", Order=1002, EmitDefaultValue=false)]
		public Uri Location
		{
			get;
			set;
		}

		[DataMember(Name="NotificationId", Order=1000, EmitDefaultValue=false)]
		public string NotificationId
		{
			get;
			set;
		}

		[DataMember(Name="Outcomes", Order=1009, EmitDefaultValue=false)]
		public PushOutcomeCollection Outcomes
		{
			get;
			set;
		}

		[DataMember(Name="StartTime", Order=1005, EmitDefaultValue=false)]
		public DateTime? StartTime
		{
			get;
			set;
		}

		[DataMember(Name="State", Order=1003, EmitDefaultValue=false)]
		public NotificationOutcomeState State
		{
			get;
			set;
		}

		[DataMember(Name="Tags", Order=1007, EmitDefaultValue=false)]
		public string Tags
		{
			get;
			set;
		}

		[DataMember(Name="TargetPlatforms", Order=1008, EmitDefaultValue=false)]
		public string TargetPlatforms
		{
			get;
			set;
		}

		public NotificationDetails()
		{
		}
	}
}