using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="NamespaceDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class NamespaceDescription
	{
		public readonly static DataContractSerializer Serializer;

		[DataMember(Name="AcsManagementEndpoint", IsRequired=false, Order=105, EmitDefaultValue=false)]
		public Uri AcsManagementEndpoint
		{
			get;
			set;
		}

		[DataMember(Name="ConnectionString", IsRequired=false, Order=107, EmitDefaultValue=false)]
		public string ConnectionString
		{
			get;
			set;
		}

		[DataMember(Name="CreateACSNamespace", IsRequired=false, Order=204, EmitDefaultValue=false)]
		public bool CreateACSNamespace
		{
			get;
			set;
		}

		[DataMember(Name="CreatedAt", IsRequired=false, Order=104, EmitDefaultValue=false)]
		public DateTime CreatedAt
		{
			get;
			set;
		}

		[DataMember(Name="Critical", IsRequired=false, Order=110, EmitDefaultValue=false)]
		public bool? Critical
		{
			get;
			set;
		}

		internal string CurrentState
		{
			get;
			set;
		}

		[DataMember(Name="DataCenter", IsRequired=false, Order=202, EmitDefaultValue=false)]
		internal string DataCenter
		{
			get;
			set;
		}

		[DataMember(Name="DefaultKey", IsRequired=false, Order=102, EmitDefaultValue=false)]
		public string DefaultKey
		{
			get;
			set;
		}

		[DataMember(Name="Enabled", IsRequired=false, Order=109, EmitDefaultValue=true)]
		public bool Enabled
		{
			get;
			set;
		}

		[DataMember(Name="EventHubEnabled", IsRequired=false, Order=205, EmitDefaultValue=false)]
		public bool EventHubEnabled
		{
			get;
			set;
		}

		internal bool InDeletedSubscription
		{
			get;
			set;
		}

		[DataMember(Name="Name", IsRequired=false, Order=100, EmitDefaultValue=false)]
		public string Name
		{
			get;
			set;
		}

		[DataMember(Name="NamespaceType", IsRequired=false, Order=206, EmitDefaultValue=false)]
		public Microsoft.ServiceBus.Management.NamespaceType? NamespaceType
		{
			get;
			set;
		}

		internal string ProjectKey
		{
			get;
			set;
		}

		[DataMember(Name="Region", IsRequired=false, Order=101, EmitDefaultValue=false)]
		public string Region
		{
			get;
			set;
		}

		[DataMember(Name="ScaleUnit", IsRequired=false, Order=201, EmitDefaultValue=false)]
		internal string ScaleUnit
		{
			get;
			set;
		}

		internal string ScaleUnitKey
		{
			get;
			set;
		}

		internal string ScopeKey
		{
			get;
			set;
		}

		[DataMember(Name="ServiceBusEndpoint", IsRequired=false, Order=106, EmitDefaultValue=false)]
		public Uri ServiceBusEndpoint
		{
			get;
			set;
		}

		internal NamespaceState State
		{
			get;
			set;
		}

		[DataMember(Name="Status", IsRequired=false, Order=103, EmitDefaultValue=false)]
		public NamespaceState Status
		{
			get;
			set;
		}

		[DataMember(Name="SubscriptionId", IsRequired=false, Order=108, EmitDefaultValue=false)]
		public string SubscriptionId
		{
			get;
			set;
		}

		internal string TargetState
		{
			get;
			set;
		}

		[DataMember(Name="UpdatedAt", IsRequired=false, Order=203, EmitDefaultValue=false)]
		internal DateTime? UpdatedAt
		{
			get;
			set;
		}

		static NamespaceDescription()
		{
			NamespaceDescription.Serializer = new DataContractSerializer(typeof(NamespaceDescription));
		}

		public NamespaceDescription()
		{
		}
	}
}