using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="ScheduledNotification", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class ScheduledNotification
	{
		[DataMember(Name="Payload", IsRequired=true, Order=1001, EmitDefaultValue=true)]
		public Notification Payload
		{
			get;
			internal set;
		}

		[DataMember(Name="ScheduledNotificationId", IsRequired=true, Order=1002, EmitDefaultValue=true)]
		public string ScheduledNotificationId
		{
			get;
			internal set;
		}

		[DataMember(Name="ScheduledTime", IsRequired=true, Order=1003, EmitDefaultValue=true)]
		public DateTimeOffset ScheduledTime
		{
			get;
			internal set;
		}

		[DataMember(Name="Tags", IsRequired=true, Order=1004, EmitDefaultValue=true)]
		public string Tags
		{
			get;
			internal set;
		}

		[DataMember(Name="TrackingId", IsRequired=true, Order=1005, EmitDefaultValue=true)]
		public string TrackingId
		{
			get;
			internal set;
		}

		public ScheduledNotification()
		{
		}
	}
}