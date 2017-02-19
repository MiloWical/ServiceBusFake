using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="CloseClient", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal sealed class CloseClientCommand : IExtensibleDataObject
	{
		public ExtensionDataObject ExtensionData
		{
			get;
			set;
		}

		public CloseClientCommand()
		{
		}
	}
}