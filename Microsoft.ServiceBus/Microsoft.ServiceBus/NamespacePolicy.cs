using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[DataContract(Name="NamespacePolicy", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")]
	internal class NamespacePolicy : IExtensibleDataObject
	{
		[DataMember(Name="ContentType", IsRequired=false, EmitDefaultValue=false, Order=0)]
		private string contentType;

		[DataMember(Name="PolicyContent", IsRequired=false, EmitDefaultValue=false, Order=1)]
		private byte[] policyContent;

		private ExtensionDataObject extensionData;

		public string ContentType
		{
			get
			{
				return this.contentType;
			}
			internal set
			{
				this.contentType = value;
			}
		}

		public byte[] PolicyContent
		{
			get
			{
				return this.policyContent;
			}
			set
			{
				this.policyContent = value;
			}
		}

		ExtensionDataObject System.Runtime.Serialization.IExtensibleDataObject.ExtensionData
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

		public NamespacePolicy()
		{
		}
	}
}