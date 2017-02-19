using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="ServerErrorFault", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relay")]
	internal class ServerErrorFault : IExtensibleDataObject
	{
		ExtensionDataObject System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
		{
			get;
			set;
		}

		public ServerErrorFault()
		{
		}
	}
}