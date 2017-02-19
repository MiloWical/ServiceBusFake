using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="UserValidationResult", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class UserValidationResult
	{
		public string Reason
		{
			get;
			set;
		}

		[DataMember(Name="Result")]
		public bool Result
		{
			get;
			set;
		}

		public UserValidationResult()
		{
		}
	}
}