using Microsoft.ServiceBus.Notifications;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="NotificationHubPnsCredentials", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class NotificationHubPnsCredentials
	{
		[DataMember(Name="AdmCredential", IsRequired=false, EmitDefaultValue=false, Order=1007)]
		public Microsoft.ServiceBus.Notifications.AdmCredential AdmCredential
		{
			get;
			set;
		}

		[DataMember(Name="ApnsCredential", IsRequired=false, EmitDefaultValue=false, Order=1001)]
		public Microsoft.ServiceBus.Notifications.ApnsCredential ApnsCredential
		{
			get;
			set;
		}

		[DataMember(Name="BaiduCredential", IsRequired=false, EmitDefaultValue=false, Order=1009)]
		public Microsoft.ServiceBus.Notifications.BaiduCredential BaiduCredential
		{
			get;
			set;
		}

		[DataMember(Name="GcmCredential", IsRequired=false, EmitDefaultValue=false, Order=1003)]
		public Microsoft.ServiceBus.Notifications.GcmCredential GcmCredential
		{
			get;
			set;
		}

		[DataMember(Name="MpnsCredential", IsRequired=false, EmitDefaultValue=false, Order=1004)]
		public Microsoft.ServiceBus.Notifications.MpnsCredential MpnsCredential
		{
			get;
			set;
		}

		[DataMember(Name="NokiaXCredential", IsRequired=false, EmitDefaultValue=false, Order=1008)]
		internal Microsoft.ServiceBus.Notifications.NokiaXCredential NokiaXCredential
		{
			get;
			set;
		}

		[DataMember(Name="Revision", IsRequired=false, Order=1006, EmitDefaultValue=false)]
		public long Revision
		{
			get;
			set;
		}

		[DataMember(Name="UpdatedAt", IsRequired=false, Order=1005, EmitDefaultValue=false)]
		public DateTime UpdatedAt
		{
			get;
			internal set;
		}

		[DataMember(Name="WnsCredential", IsRequired=false, EmitDefaultValue=false, Order=1002)]
		public Microsoft.ServiceBus.Notifications.WnsCredential WnsCredential
		{
			get;
			set;
		}

		public NotificationHubPnsCredentials()
		{
		}
	}
}