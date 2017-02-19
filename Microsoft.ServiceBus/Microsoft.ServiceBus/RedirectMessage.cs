using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="Redirect", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	internal class RedirectMessage : IExtensibleDataObject
	{
		[DataMember(Name="Uri", IsRequired=false, EmitDefaultValue=false, Order=0)]
		private System.Uri uri;

		private ExtensionDataObject extensionDataObject;

		public ExtensionDataObject ExtensionData
		{
			get
			{
				return this.extensionDataObject;
			}
			set
			{
				this.extensionDataObject = value;
			}
		}

		public System.Uri Uri
		{
			get
			{
				return this.uri;
			}
			set
			{
				this.uri = value;
			}
		}

		public RedirectMessage()
		{
		}

		public RedirectMessage(System.Uri uri)
		{
			this.uri = uri;
		}
	}
}