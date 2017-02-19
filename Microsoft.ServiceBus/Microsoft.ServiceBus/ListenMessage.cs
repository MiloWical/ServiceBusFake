using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="Listen", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	internal class ListenMessage : IExtensibleDataObject
	{
		[DataMember(Name="NameSettings", IsRequired=false, EmitDefaultValue=false, Order=0)]
		private Microsoft.ServiceBus.NameSettings nameSettings;

		[DataMember(Name="Uri", IsRequired=false, EmitDefaultValue=false, Order=1)]
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

		public Microsoft.ServiceBus.NameSettings NameSettings
		{
			get
			{
				return this.nameSettings;
			}
			set
			{
				this.nameSettings = value;
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

		public ListenMessage(System.Uri uri, Microsoft.ServiceBus.NameSettings nameSettings)
		{
			this.uri = uri;
			this.nameSettings = nameSettings;
		}
	}
}