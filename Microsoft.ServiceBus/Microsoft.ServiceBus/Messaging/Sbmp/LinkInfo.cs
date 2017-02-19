using Microsoft.ServiceBus.Messaging;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[DataContract(Name="LinkInfo", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal class LinkInfo : IExtensibleDataObject
	{
		private readonly static DataContractSerializer linkInfoSerializer;

		[DataMember(Name="LinkType", IsRequired=false, EmitDefaultValue=false)]
		private Microsoft.ServiceBus.Messaging.Sbmp.LinkType linkType;

		[DataMember(Name="LinkId", IsRequired=true)]
		private string linkId;

		[DataMember(Name="ConnectionId", IsRequired=true)]
		private string connectionId;

		[DataMember(Name="entityName", IsRequired=true)]
		private string entityName;

		[DataMember(Name="receiveMode", IsRequired=false, EmitDefaultValue=false)]
		private Microsoft.ServiceBus.Messaging.ReceiveMode receiveMode;

		[DataMember(Name="isSessionReceiver", IsRequired=false, EmitDefaultValue=false)]
		private bool isSessionReceiver;

		[DataMember(Name="entityType", IsRequired=false, EmitDefaultValue=false)]
		private MessagingEntityType? entityType;

		[DataMember(Name="transferDestinationEntityName", IsRequired=false, EmitDefaultValue=false)]
		private string transferDestinationEntityAddress;

		[DataMember(Name="transferDestinationResourceId", IsRequired=false, EmitDefaultValue=false)]
		private long transferDestinationResourceId;

		[DataMember(Name="transferDestinationInstanceHandle", IsRequired=false, EmitDefaultValue=false)]
		private string transferDestinationMessagingInstanceHandle;

		[DataMember(Name="transferDestinationAuthorizationHeader", IsRequired=false, EmitDefaultValue=false)]
		private string transferDestinationAuthorizationToken;

		[DataMember(Name="isBrowseMode", EmitDefaultValue=false, IsRequired=false)]
		private bool isBrowseMode;

		[DataMember(Name="sessionId", IsRequired=false, EmitDefaultValue=false)]
		private string sessionId;

		[DataMember(Name="api-version", IsRequired=false, EmitDefaultValue=false)]
		private int apiVersion;

		[DataMember(Name="isHttp", IsRequired=false, EmitDefaultValue=false)]
		private bool isHttp;

		[DataMember(Name="fromOffset", IsRequired=false, EmitDefaultValue=false, Order=65536)]
		private string fromOffset;

		[DataMember(Name="fromTimestamp", IsRequired=false, EmitDefaultValue=false, Order=65537)]
		private long? fromTimestamp;

		public int ApiVersion
		{
			get
			{
				return this.apiVersion;
			}
			set
			{
				this.apiVersion = value;
			}
		}

		public string ConnectionId
		{
			get
			{
				return this.connectionId;
			}
			set
			{
				this.connectionId = value;
			}
		}

		public string EntityName
		{
			get
			{
				return this.entityName;
			}
			set
			{
				this.entityName = value;
			}
		}

		public MessagingEntityType? EntityType
		{
			get
			{
				return this.entityType;
			}
			set
			{
				this.entityType = value;
			}
		}

		public ExtensionDataObject ExtensionData
		{
			get;
			set;
		}

		public string FromOffset
		{
			get
			{
				return this.fromOffset;
			}
			set
			{
				this.fromOffset = value;
			}
		}

		public long? FromTimestamp
		{
			get
			{
				return this.fromTimestamp;
			}
			set
			{
				this.fromTimestamp = value;
			}
		}

		public bool IsBrowseMode
		{
			get
			{
				return this.isBrowseMode;
			}
			set
			{
				this.isBrowseMode = value;
			}
		}

		public bool IsHttp
		{
			get
			{
				return this.isHttp;
			}
			set
			{
				this.isHttp = value;
			}
		}

		public bool IsSessionReceiver
		{
			get
			{
				return this.isSessionReceiver;
			}
			set
			{
				this.isSessionReceiver = value;
			}
		}

		public string LinkId
		{
			get
			{
				return this.linkId;
			}
			set
			{
				this.linkId = value;
			}
		}

		public Microsoft.ServiceBus.Messaging.Sbmp.LinkType LinkType
		{
			get
			{
				return this.linkType;
			}
			set
			{
				this.linkType = value;
			}
		}

		public Microsoft.ServiceBus.Messaging.ReceiveMode ReceiveMode
		{
			get
			{
				return this.receiveMode;
			}
			set
			{
				this.receiveMode = value;
			}
		}

		public string SessionId
		{
			get
			{
				return this.sessionId;
			}
			set
			{
				this.sessionId = value;
			}
		}

		public string TransferDestinationAuthorizationToken
		{
			get
			{
				return this.transferDestinationAuthorizationToken;
			}
			set
			{
				this.transferDestinationAuthorizationToken = value;
			}
		}

		public string TransferDestinationEntityAddress
		{
			get
			{
				return this.transferDestinationEntityAddress;
			}
			set
			{
				this.transferDestinationEntityAddress = value;
			}
		}

		public string TransferDestinationMessagingInstanceHandle
		{
			get
			{
				return this.transferDestinationMessagingInstanceHandle;
			}
			set
			{
				this.transferDestinationMessagingInstanceHandle = value;
			}
		}

		public long TransferDestinationResourceResourceId
		{
			get
			{
				return this.transferDestinationResourceId;
			}
			set
			{
				this.transferDestinationResourceId = value;
			}
		}

		static LinkInfo()
		{
			LinkInfo.linkInfoSerializer = new DataContractSerializer(typeof(LinkInfo));
		}

		public LinkInfo()
		{
		}

		public void AddTo(MessageHeaders messageHeaders)
		{
			messageHeaders.Add(MessageHeader.CreateHeader("LinkInfo", "http://schemas.microsoft.com/netservices/2011/06/servicebus", this, LinkInfo.linkInfoSerializer));
		}

		public static LinkInfo GetHeader(MessageHeaders messageHeaders)
		{
			return messageHeaders.GetHeader<LinkInfo>("LinkInfo", "http://schemas.microsoft.com/netservices/2011/06/servicebus", LinkInfo.linkInfoSerializer);
		}
	}
}