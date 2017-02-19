using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="Property", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class Property : ICloneable
	{
		private const string PropertyName = "Property";

		private const string NameName = "Name";

		private const string ValueName = "Value";

		private const string ModifiedName = "Modified";

		private const string RevisionName = "Revision";

		private const string CreatedName = "Created";

		public readonly static DataContractSerializer Serializer;

		[DataMember(Name="Created", IsRequired=false, Order=102, EmitDefaultValue=false)]
		public DateTime Created
		{
			get;
			internal set;
		}

		[DataMember(Name="Modified", IsRequired=false, Order=103, EmitDefaultValue=false)]
		public DateTime Modified
		{
			get;
			internal set;
		}

		[DataMember(Name="Name", IsRequired=true, Order=100)]
		public string Name
		{
			get;
			set;
		}

		[DataMember(Name="Revision", IsRequired=false, Order=104, EmitDefaultValue=false)]
		public long Revision
		{
			get;
			set;
		}

		[DataMember(Name="Value", IsRequired=true, Order=101)]
		public string Value
		{
			get;
			set;
		}

		static Property()
		{
			Property.Serializer = new DataContractSerializer(typeof(Property));
		}

		public Property()
		{
		}

		public object Clone()
		{
			return this.MemberwiseClone();
		}
	}
}