using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="NamespaceEntityStats", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class NamespaceEntityStats
	{
		public readonly static DataContractSerializer Serializer;

		[DataMember(Name="EventHubCount", IsRequired=false, Order=104, EmitDefaultValue=true)]
		public long EventHubCount
		{
			get;
			set;
		}

		[DataMember(Name="NotificationHubCount", IsRequired=false, Order=103, EmitDefaultValue=true)]
		public long NotificationHubCount
		{
			get;
			set;
		}

		[DataMember(Name="QueueCount", IsRequired=false, Order=101, EmitDefaultValue=true)]
		public long QueueCount
		{
			get;
			set;
		}

		[DataMember(Name="RelayCount", IsRequired=false, Order=102, EmitDefaultValue=true)]
		public long RelayCount
		{
			get;
			set;
		}

		[DataMember(Name="TopicCount", IsRequired=false, Order=100, EmitDefaultValue=true)]
		public long TopicCount
		{
			get;
			set;
		}

		static NamespaceEntityStats()
		{
			NamespaceEntityStats.Serializer = new DataContractSerializer(typeof(NamespaceEntityStats));
		}

		public NamespaceEntityStats()
		{
		}
	}
}