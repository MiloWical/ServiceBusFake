using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="NotificationHubPlan", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class NotificationHubPlan
	{
		private const string RevisionName = "Revision";

		[DataMember(Name="SKU", Order=1001, IsRequired=true)]
		public NotificationHubSKUType SKU;

		[DataMember(Name="Revision", IsRequired=false, Order=1005, EmitDefaultValue=false)]
		public long Revision
		{
			get;
			set;
		}

		[DataMember(Name="SelectedSKUMaxUnits", Order=1002, IsRequired=true, EmitDefaultValue=false)]
		public int SelectedSKUMaxUnits
		{
			get;
			set;
		}

		[DataMember(Name="SelectedSKUMinUnits", Order=1003, IsRequired=true, EmitDefaultValue=false)]
		public int SelectedSKUMinUnits
		{
			get;
			set;
		}

		[DataMember(Name="UpdatedAt", IsRequired=false, Order=1004, EmitDefaultValue=false)]
		public DateTime UpdatedAt
		{
			get;
			internal set;
		}

		public NotificationHubPlan()
		{
		}
	}
}