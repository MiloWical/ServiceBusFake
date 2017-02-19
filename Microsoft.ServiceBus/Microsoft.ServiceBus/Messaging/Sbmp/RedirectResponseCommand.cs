using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="RedirectResponse", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class RedirectResponseCommand : IExtensibleDataObject
	{
		[DataMember(Name="redirectTo", EmitDefaultValue=true, IsRequired=false, Order=65537)]
		private string redirectTo;

		[DataMember(Name="containerNameResolutionMode", EmitDefaultValue=true, IsRequired=false, Order=65538)]
		private Microsoft.ServiceBus.Messaging.Sbmp.ContainerNameResolutionMode containerNameResolutionMode;

		private ExtensionDataObject extensionData;

		public Microsoft.ServiceBus.Messaging.Sbmp.ContainerNameResolutionMode ContainerNameResolutionMode
		{
			get
			{
				return this.containerNameResolutionMode;
			}
			set
			{
				this.containerNameResolutionMode = value;
			}
		}

		public ExtensionDataObject ExtensionData
		{
			get
			{
				return this.extensionData;
			}
			set
			{
				this.extensionData = value;
			}
		}

		public string RedirectTo
		{
			get
			{
				return this.redirectTo;
			}
			set
			{
				this.redirectTo = value;
			}
		}

		public RedirectResponseCommand()
		{
		}
	}
}