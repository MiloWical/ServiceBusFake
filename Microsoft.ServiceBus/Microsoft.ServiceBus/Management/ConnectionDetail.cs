using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="ConnectionDetail", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class ConnectionDetail
	{
		[DataMember(Name="AuthorizationType", IsRequired=false, Order=1004, EmitDefaultValue=false)]
		public string AuthorizationType
		{
			get;
			set;
		}

		[DataMember(Name="ConnectionString", IsRequired=false, Order=1002, EmitDefaultValue=false)]
		public string ConnectionString
		{
			get;
			set;
		}

		[DataMember(Name="KeyName", IsRequired=true, Order=1001, EmitDefaultValue=false)]
		[Key]
		public string KeyName
		{
			get;
			set;
		}

		[DataMember(Name="Rights", IsRequired=false, Order=1005, EmitDefaultValue=false)]
		public IEnumerable<AccessRights> Rights
		{
			get;
			set;
		}

		[DataMember(Name="SecondaryConnectionString", IsRequired=false, Order=1003, EmitDefaultValue=false)]
		public string SecondaryConnectionString
		{
			get;
			set;
		}

		public ConnectionDetail()
		{
		}
	}
}